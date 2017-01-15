/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Genetic-Algorithm-Platform/blob/master/LICENSE.md
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Open.Collections;

namespace GeneticAlgorithmPlatform
{
	/// <summary>
	/// Problems define what parameters need to be tested to resolve fitness.
	/// </summary>
	public interface IProblem<TGenome>
		 where TGenome : IGenome
	{
		int ID { get; }

		// 0 = acquire sampleId from sample count.
		Task<IFitness> ProcessTest(TGenome g, long sampleId = 0);

		// Alternative for delegate usage.
		GenomeTestDelegate<TGenome> TestProcessor { get; }

		void AddToGlobalFitness<T>(IEnumerable<T> results) where T : IGenomeFitness<TGenome>;
		IFitness AddToGlobalFitness(IGenomeFitness<TGenome> result);
		IFitness AddToGlobalFitness(TGenome genome, IFitness result);

		GenomeFitness<TGenome, Fitness>? GetFitnessFor(TGenome genome, bool ensureSourceGenome = false);
		IEnumerable<IGenomeFitness<TGenome, Fitness>> GetFitnessFor(IEnumerable<TGenome> genome, bool ensureSourceGenomes = false);

		int GetSampleCountFor(TGenome genome);
	}

	public static class ProblemExtensions
	{

		public static Task<KeyValuePair<IProblem<TGenome>, IFitness>[]> ProcessOnce<TGenome>(
			this IEnumerable<IProblem<TGenome>> problems,
			TGenome genome,
			long sampleId = 0)
			where TGenome : IGenome
		{
			if (genome == null)
				throw new ArgumentNullException("genome");
			if (!problems.HasAny())
				return Task.FromResult(new KeyValuePair<IProblem<TGenome>, IFitness>[0]);

			return Task.WhenAll(
				problems.Select(p =>
					p.ProcessTest(genome, sampleId)
						.ContinueWith(t => KeyValuePair.New(p, t.Result))
				)
			);
		}

		public static Task<KeyValuePair<IProblem<TGenome>, GenomeFitness<TGenome>[]>[]> ProcessOnce<TGenome>(
			this IEnumerable<IProblem<TGenome>> problems,
			IEnumerable<TGenome> genomes,
			long sampleId = 0)
			where TGenome : IGenome
		{
			if (genomes == null)
				throw new ArgumentNullException("genomes");
			if (!problems.HasAny())
				return Task.FromResult(new KeyValuePair<IProblem<TGenome>, GenomeFitness<TGenome>[]>[0]);

			return Task.WhenAll(problems
				.Select(p =>
					Task.WhenAll(
						genomes.Select(g => p.ProcessTest(g, sampleId)
							.ContinueWith(t => new GenomeFitness<TGenome>(g, t.Result))))
						.ContinueWith(t => KeyValuePair.New(p, t.Result.Sort()))));
		}


		public static Task<KeyValuePair<IProblem<TGenome>, IFitness>[]> Process<TGenome>(
			this IEnumerable<IProblem<TGenome>> problems,
			TGenome genome,
			IEnumerable<long> sampleIds)
			where TGenome : IGenome
		{
			if (genome == null)
				throw new ArgumentNullException("genome");
			if (!problems.HasAny())
				return Task.FromResult(new KeyValuePair<IProblem<TGenome>, IFitness>[0]);

			return Task.WhenAll(
				problems.Select(
					p => Task.WhenAll(sampleIds.Select(id => p.ProcessTest(genome, id)))
						.ContinueWith(t => KeyValuePair.New(p, (IFitness)t.Result.Merge()))));
		}


		public static Task<KeyValuePair<IProblem<TGenome>, GenomeFitness<TGenome>[]>[]> Process<TGenome>(
			this IEnumerable<IProblem<TGenome>> problems,
			IEnumerable<TGenome> genomes,
			IEnumerable<long> sampleIds)
			where TGenome : IGenome
		{
			if (genomes == null)
				throw new ArgumentNullException("genomes");
			if (!problems.HasAny())
				return Task.FromResult(new KeyValuePair<IProblem<TGenome>, GenomeFitness<TGenome>[]>[0]);

			return Task.WhenAll(problems.Select(
				p => Task.WhenAll(genomes.Select(
					g => Task.WhenAll(sampleIds.Select(id => p.ProcessTest(g, id)))
						.ContinueWith(t => new GenomeFitness<TGenome>(g, t.Result.Merge()))))
						.ContinueWith(t => KeyValuePair.New(p, t.Result.Sort()))));
		}


		public static Task<KeyValuePair<IProblem<TGenome>, GenomeFitness<TGenome>[]>[]> Process<TGenome>(
			this IEnumerable<IProblem<TGenome>> problems,
			IEnumerable<TGenome> genomes,
			int count = 1)
			where TGenome : IGenome
		{
			return Process(problems, genomes, Enumerable.Range(0, count).Select(i => SampleID.Next()));
		}

		public static Task<KeyValuePair<IProblem<TGenome>, IFitness>[]> Process<TGenome>(
			this IEnumerable<IProblem<TGenome>> problems,
			TGenome genome,
			int count = 1)
			where TGenome : IGenome
		{
			return Process(problems, genome, Enumerable.Range(0, count).Select(i => SampleID.Next()));
		}
// need to add to global fitness
		public static Task<KeyValuePair<IProblem<TGenome>, GenomeFitness<TGenome>[]>> Process<TGenome>(
			this IProblem<TGenome> problem,
			IEnumerable<TGenome> genomes,
			int count = 1)
			where TGenome : IGenome
		{
			if (problem == null)
				throw new ArgumentNullException("problem");
			if (genomes == null)
				throw new ArgumentNullException("genomes");
			return Process(new IProblem<TGenome>[] { problem }, genomes, Enumerable.Range(0, 1).Select(i => SampleID.Next()))
				.ContinueWith(t => t.Result.Single());
		}

	}

}