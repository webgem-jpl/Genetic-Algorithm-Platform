﻿using System;
using System.Diagnostics;

namespace GeneticAlgorithmPlatform
{
	public class Program
	{
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
		public static void Main(string[] args)
		{
			var e = new AlgebraBlackBox.Environment(SqrtA2B2);
			var converged = e.WaitForConverged();
			e.SpawnNew();
			converged.Wait();

			Console.Write("Converged: ");
			int i = 0;
			foreach (var problem in converged.Result)
			{
				Console.WriteLine((++i) + ":");
				foreach (var genome in problem.Convergent)
				{
					Console.WriteLine("\t");
					Console.WriteLine(genome.ToAlphaParameters());
				}
			}
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