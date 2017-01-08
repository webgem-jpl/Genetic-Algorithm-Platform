﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Open;
using Open.Collections;

namespace GeneticAlgorithmPlatform
{
	public class Program
	{
		static double AB(params double[] p)
		{
			var a = p[0];
			var b = p[1];
			return a * b;
		}

		static double SqrtA2B2(params double[] p)
		{
			var a = p[0];
			var b = p[1];
			return Math.Sqrt(a * a + b * b);
		}

		static double SqrtA2B2AB(params double[] p)
		{
			var a = p[0];
			var b = p[1];
			return Math.Sqrt(a * a + b * b + a) + b;
		}
		static readonly double[] OneOne = new double[] { 1, 1 };
		public static void Main(string[] args)
		{
			var sw = Stopwatch.StartNew();
			var env = new AlgebraBlackBox.Environment(SqrtA2B2, 10);
			var prob = ((AlgebraBlackBox.Problem)(env.Problem));

			bool converged = false;
			env.TopGenome.LinkTo(new ActionBlock<AlgebraBlackBox.Genome>(genome =>
			{
				var fitness = prob.GetOrCreateFitnessFor(genome).Fitness;
				if(!converged)
					converged = fitness.HasConverged(10);
				if (converged)
				{
					Console.WriteLine("\nConverged: ");
					env.TopGenome.Complete();
				}
				var asReduced = genome.AsReduced();
				if(asReduced==genome)
					Console.WriteLine("{0}:\t{1}",1, genome.ToAlphaParameters());
				else
					Console.WriteLine("{0}:\t{1} => {2}",1, genome.ToAlphaParameters(), asReduced.ToAlphaParameters());

				Console.WriteLine("  \t(1,1) = {0}", genome.Calculate(OneOne));
				Console.WriteLine("  \t[{0}] ({1} samples)", fitness.Scores.JoinToString(","), fitness.SampleCount);
				Console.WriteLine();
			}));

			Task.Run(async () =>
			{
				while (!converged)
				{
					var tc = prob.TestCount;
					if (tc != 0)
					{
						Console.WriteLine("{0} tests, {1} total time, {2} ticks average", tc, sw.Elapsed.ToStringVerbose(), sw.ElapsedTicks / tc);
						Console.WriteLine();
					}

					await Task.Delay(1000);
				}
			});

			env.TopGenome.Completion.Wait();

		}

		static void PerfTest()
		{
			var sw = new Stopwatch();
			sw.Start();

			var n = 0;
			for (var j = 0; j < 10; j++)
			{
				for (var i = 0; i < 1000000000; i++)
				{
					n += i;
				}
			}

			sw.Stop();
			Console.WriteLine("Result: " + n);

			Console.WriteLine("Elapsed Time: " + sw.ElapsedMilliseconds);
		}
	}
}
