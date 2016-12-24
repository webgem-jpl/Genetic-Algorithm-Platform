/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Genetic-Algorithm-Platform/blob/master/LICENSE.md
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Open.Math;
using Fitness = GeneticAlgorithmPlatform.Fitness;

namespace AlgebraBlackBox
{

    public delegate double Formula(params double[] p);

    public class Problem : GeneticAlgorithmPlatform.IProblem<Genome>
    {


        private ConcurrentDictionary<string, Lazy<Fitness>> _fitness;
        private Formula _actualFormula;

        protected ConcurrentDictionary<string, Genome> _convergent;
        public ICollection<Genome> Convergent
        {
            get
            {
                return this._convergent.Values;
            }
        }

        public Problem(Formula actualFormula)
        {
            this._fitness = new ConcurrentDictionary<string, Lazy<Fitness>>();
            this._actualFormula = actualFormula;
            this._convergent = new ConcurrentDictionary<string, Genome>();
        }

        // protected getScoreFor(genome:string):number;
        // protected getScoreFor(genome:AlgebraGenome):number;
        // protected getScoreFor(genome:any):number
        // {
        // 	if(!genome) return 0;
        // 	if(!Type.isString(genome)) genome = genome.hashReduced;
        // 	var s = this._fitness[genome];
        // 	return s && s.score || 0;
        // }

        public Fitness GetFitnessFor(Genome genome, bool createIfMissing = true)
        {
            var key = genome.CachedToStringReduced;
            if (createIfMissing) return _fitness.GetOrAdd(key, k => Lazy.New(() => new Fitness())).Value;

            Lazy<Fitness> value;
            return _fitness.TryGetValue(key, out value) ? value.Value : null;
        }

        public IEnumerable<Genome> Rank(IEnumerable<Genome> population)
        {
            return population
                .AsParallel()
                .Where(g =>
                {
                    var fitness = GetFitnessFor(g);
                    lock (fitness.SyncLock)
                    {
                        return fitness.Scores.All(s => !double.IsNaN(s));
                    }
                })
                .OrderByDescending(g => GetFitnessFor(g))
                .ThenBy(g => g.Hash.Length);
        }

        // rankAndReduce(
        // 	population:IEnumerableOrArray<AlgebraGenome>,
        // 	targetMaxPopulation:number):ILinqEnumerable<AlgebraGenome>
        // {
        // 	var lastFitness:Fitness;
        // 	return this.rank(population)
        // 		.takeWhile((g, i) =>
        // 		{
        // 			var lf = lastFitness, f = this.getFitnessFor(g);
        // 			lastFitness = f;
        // 			return i<targetMaxPopulation || lf.compareTo(f)===0;
        // 		});
        // }

        public List<Genome> Pareto(IEnumerable<Genome> population)
        {
            // TODO: Needs work/optimization.
            var d = population
                .Select(g => g.AsReduced())
                .Distinct()
                .ToDictionary(g => g.CachedToStringReduced, g => g);

            bool found;
            List<Genome> p;
            do
            {
                found = false;
                p = d.Values.ToList();
                foreach (var g in p)
                {
                    var gs = this.GetFitnessFor(g).Scores.ToArray();
                    var len = gs.Length;
                    if (d.Values.Any(o =>
                         {
                             var os = this.GetFitnessFor(o).Scores.ToArray();
                             for (var i = 0; i < len; i++)
                             {
                                 var osv = os[i];
                                 if (double.IsNaN(osv)) return true;
                                 if (gs[i] <= os[i]) return false;
                             }
                             return true;
                         }))
                    {
                        found = true;
                        d.Remove(g.Hash);
                    }
                }
            } while (found);

            return p;
        }

        public Task<double> Correlation(
            double[] aSample, double[] bSample,
            Genome gA, Genome gB)
        {
            return Task.Run(() =>
            {
                var len = aSample.Length * bSample.Length;

                var gA_result = new double[len];
                var gB_result = new double[len];
                var i = 0;

                foreach (var a in aSample)
                {
                    foreach (var b in bSample)
                    {
                        var p = new double[] { a, b }; // Could be using Tuples?
                        var r1 = gA.Calculate(p);
                        var r2 = gB.Calculate(p);
                        Task.WaitAll(r1, r2);
                        gA_result[i] = r1.Result;
                        gB_result[i] = r2.Result;
                        i++;
                    }
                }

                return gA_result.Correlation(gB_result);
            });
        }


        // // compare(a:AlgebraGenome, b:AlgebraGenome):boolean
        // // {
        // // 	return this.correlation(this.sample(), this.sample(), a, b)>0.9999999;
        // // }


        //noinspection JSMethodCanBeStatic
        public double[] Sample(int count = 5, double range = 100)
        {
            var result = new HashSet<double>();

            while (result.Count < count)
            {
                result.Add(RandomUtil.Random.NextDouble() * range);
            }
            return result.OrderBy(v => v).ToArray();
        }

        async Task ProcessTest(GeneticAlgorithmPlatform.Population<Genome> p)
        {
            var f = this._actualFormula;

            var aSample = Sample();
            var bSample = Sample();
            var samples = new List<double[]>();
            var correct = new List<double>();

            foreach (var a in aSample)
            {
                foreach (var b in bSample)
                {
                    samples.Add(new double[] { a, b });
                    correct.Add(f(a, b));
                }
            }

            var len = correct.Count;
            foreach (var g in p.Values)
            {
                var divergence = new double[correct.Count];
                var calc = new double[correct.Count];

                for (var i = 0; i < len; i++)
                {
                    var result = await g.Calculate(samples[i]);
                    calc[i] = result;
                    divergence[i] = -Math.Abs(result - correct[i]);
                }

                var c = correct.Correlation(calc);
                var d = divergence.Average() + 1;

                GetFitnessFor(g).AddScores(
                    (double.IsNaN(c) || double.IsInfinity(c)) ? -2 : c,
                    (double.IsNaN(d) || double.IsInfinity(d)) ? double.NegativeInfinity : d
                );
            }


            // return Task.WhenAll(
            //     p.Values.Select(
            //         async g =>
            //         {

            //         })
            // );

        }
        IEnumerable<Task> ProcessTest(GeneticAlgorithmPlatform.Population<Genome> p, int count)
        {
            for (var i = 0; i < count; i++)
            {
                yield return ProcessTest(p);
            }
        }

        public async Task Test(GeneticAlgorithmPlatform.Population<Genome> p, int count = 1)
        {
            // TODO: Need to find a way to dynamically implement more than 2 params... (discover significant params)
            await Task.WhenAll(ProcessTest(p, count));

            foreach (var g in p.Values)
            {
                var key = g.CachedToStringReduced;
                if (GetFitnessFor(g).HasConverged())
                {
                    this._convergent[key] = g;
                }
                else
                {
                    Genome v;
                    this._convergent.TryRemove(key, out v);
                }
            }

        }


    }



}