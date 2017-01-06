using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Nito.AsyncEx;
using Open.Collections;
using Open.DataFlow;

namespace GeneticAlgorithmPlatform
{
	public class PoolProcessor<TGenome> : IPropagatorBlock<TGenome, GenomeFitness<TGenome>[]>
		where TGenome : IGenome
	{
		readonly IPropagatorBlock<TGenome, GenomeFitness<TGenome>[]> transform;

		public PoolProcessor(int poolSize,
			GenomeTestDelegate<TGenome> test)
		{
			transform = GenerateTransform(poolSize, test);
		}


		static long BatchId = 0;
		static ConcurrentBag<Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>>>
			BatchPool = new ConcurrentBag<Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>>>();

		public Task Completion
		{
			get
			{
				return transform.Completion;
			}
		}

		static Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>> GetBatchTracker()
		{
			Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>> batch;
			return BatchPool.TryTake(out batch) ? batch : new Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>>(new HashSet<string>(), new SortedDictionary<IFitness, TGenome>());
		}

		static void ReturnBatchTracker(Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>> tracker)
		{
			tracker.Item1.Clear();
			tracker.Item2.Clear();
			BatchPool.Add(tracker);
		}

		public static IPropagatorBlock<TGenome, GenomeFitness<TGenome>[]> SingleBatch(GenomeTestDelegate<TGenome> test, int limit = 0)
		{
			long batchId = Interlocked.Increment(ref BatchId);
			var asyncLock = new AsyncLock();
			var ordered = new SortedDictionary<IFitness, TGenome>();
			var output = new WriteOnceBlock<GenomeFitness<TGenome>[]>(null);

			ITargetBlock<TGenome> reception = new ActionBlock<TGenome>(
				async genome =>
				{
					var fitness = await test(genome, batchId);
					using (await asyncLock.LockAsync())
						ordered.Add(fitness, genome);
				},
				new ExecutionDataflowBlockOptions
				{
					MaxDegreeOfParallelism = 32,
					MaxMessagesPerTask = 3
				});

			if(limit>0)
			{
				reception = reception.AutoCompleteAfter(limit);
			}

			// Eat any repeats.
			reception = reception.Distinct(DataflowMessageStatus.Accepted);

			reception.Completion.ContinueWith(complete =>
			{
				output.Post(ordered.Select(kvp => new GenomeFitness<TGenome>(kvp.Value, kvp.Key)).ToArray());
				output.Complete();
				ordered.Clear();
			});

			return DataflowBlock.Encapsulate(reception, output);
		}


		public static IPropagatorBlock<TGenome, GenomeFitness<TGenome>[]> GenerateTransform(
			int poolSize,
			GenomeTestDelegate<TGenome> test)
		{
			// We have to create our own internal buffer and batching to allow for a progressive stream.
			long batchId = Interlocked.Increment(ref BatchId);
			var registry = new Dictionary<long, Tuple<HashSet<string>, SortedDictionary<IFitness, TGenome>>>();

			// Step 2: Process. (attach to a batch ID)
			var testing = new TransformBlock<Tuple<long, TGenome>, Tuple<long, GenomeFitness<TGenome>>>(
				async entry => new Tuple<long, GenomeFitness<TGenome>>(
					entry.Item1, new GenomeFitness<TGenome>(
						entry.Item2, await test(entry.Item2, entry.Item1))),
				new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 32 });

			// Step 1: Receieve and filter.
			var reception = new ActionBlock<TGenome>(genome =>
			{
				if (genome != null)
				{
					lock (registry) // Need to synchronize here because the size of the batch matters as well as the batch ID.
					{
						var e = registry.GetOrAdd(batchId, key => GetBatchTracker());
						if (e.Item1.Add(genome.Hash))
						{
							testing.Post(new Tuple<long, TGenome>(batchId, genome));
							if (e.Item1.Count == poolSize)
								batchId = Interlocked.Increment(ref BatchId);
						}
					}
				}
				Debug.WriteLineIf(genome == null, "Cannot process a null Genome.");
			}, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 2 });

			var output = new BufferBlock<GenomeFitness<TGenome>[]>();

			// Step 3: Buffer (sort) and emit.
			testing.LinkTo(new ActionBlock<Tuple<long, GenomeFitness<TGenome>>>(e =>
			{
				var bId = e.Item1;
				var gf = e.Item2;
				var entry = registry[bId];
				var results = entry.Item2;
				var complete = false;
				lock (results)
				{
					results.Add(gf.Fitness, gf.Genome); // Sorting occurs on adding.
					if (results.Count == poolSize)
					{
						complete = true;
						output.Post(results.Select(kvp => new GenomeFitness<TGenome>(kvp.Value, kvp.Key)).ToArray());
					}
				}
				if (complete)
				{
					lock (registry)
					{
						registry.Remove(bId);
					}
					ReturnBatchTracker(entry);
				}
			}));

			return DataflowBlock.Encapsulate(reception, output);
		}

		// public static Task<GenomeFitness<TGenome>[]> Test(IEnumerable<TGenome> genomes, GenomeTestDelegate<TGenome> test)
		// {
		// 	long batchId = Interlocked.Increment(ref BatchId);
		// 	foreach(var)
		// }

		public DataflowMessageStatus OfferMessage(DataflowMessageHeader messageHeader, TGenome messageValue, ISourceBlock<TGenome> source, bool consumeToAccept)
		{
			return transform.OfferMessage(messageHeader, messageValue, source, consumeToAccept);
		}

		public IDisposable LinkTo(ITargetBlock<GenomeFitness<TGenome>[]> target, DataflowLinkOptions linkOptions)
		{
			return transform.LinkTo(target, linkOptions);
		}

		public GenomeFitness<TGenome>[] ConsumeMessage(DataflowMessageHeader messageHeader, ITargetBlock<GenomeFitness<TGenome>[]> target, out bool messageConsumed)
		{
			return transform.ConsumeMessage(messageHeader, target, out messageConsumed);
		}

		public bool ReserveMessage(DataflowMessageHeader messageHeader, ITargetBlock<GenomeFitness<TGenome>[]> target)
		{
			return transform.ReserveMessage(messageHeader, target);
		}

		public void ReleaseReservation(DataflowMessageHeader messageHeader, ITargetBlock<GenomeFitness<TGenome>[]> target)
		{
			transform.ReleaseReservation(messageHeader, target);
		}

		public void Complete()
		{
			transform.Complete();
		}

		public void Fault(Exception exception)
		{
			transform.Fault(exception);
		}
	}

}