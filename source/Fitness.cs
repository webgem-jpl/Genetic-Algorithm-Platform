
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace GeneticAlgorithmPlatform
{

    public class SingleFitness : ThreadSafeTrackedList<double>
    {
        private Lazy<double> _average;

        public SingleFitness(IEnumerable<double> scores = null) : base()
        {
            if (scores != null)
                Add(scores);
            else
                OnModified();
        }

        protected override ModificationSynchronizer InitNewSync()
        {
            return new ModificationSynchronizer(OnModified);
        }


        protected void OnModified()
        {
            if (_average == null || _average.IsValueCreated)
                _average = new Lazy<double>(GetAverage, LazyThreadSafetyMode.ExecutionAndPublication);
        }


        private double GetAverage()
        {
            return this.ToArray().Average();
        }

        public int CompareTo(object obj)
        {
            var other = (SingleFitness)obj;
            var a = this.Average;
            var b = other.Average;
            if (a < b || double.IsNaN(a) && !double.IsNaN(b)) return -1;
            if (a > b || !double.IsNaN(a) && double.IsNaN(b)) return +1;
            return 0;
        }

        public double Average
        {
            get
            {
                if (Count == 0) return double.NaN;
                return _average.Value;
            }
        }
    }

    public class Fitness : ThreadSafeTrackedList<SingleFitness>, IComparable<Fitness>
    {

        public int SampleCount
        {
            get
            {
                if (Count == 0) return 0;
                return this.Min(s => s.Count);
            }
        }

        public bool HasConverged(uint minSamples = 100, float convergence = 1, float tolerance = 0)
        {
            if (minSamples > SampleCount) return false;

            foreach (var s in this.ToArray())
            {
                var score = s.Average;
                if (score > convergence)
                    throw new Exception("Score has exceeded convergence value.");
                if (score < convergence - tolerance)
                    return false;
            }
            return true;
        }


        public IEnumerable<double> Scores
        {
            get
            {
                return this.Select(s => s.Average);
            }
        }


        public void AddTheseScores(IEnumerable<double> scores)
        {
            Sync.Modifying(() =>
            {
                var i = 0;
                var count = Count;
                foreach (var n in scores)
                {
                    SingleFitness f;
                    if (i < count)
                    {
                        f = this[i];
                    }
                    else
                    {
                        this.Add(f = new SingleFitness());
                    }

                    f.Add(n);
                    i++;
                }
            });

        }

        public void AddScores(params double[] scores)
        {
            this.AddTheseScores(scores);
        }



        public int CompareTo(Fitness other)
        {
            var len = Count;
            Debug.Assert(len == other.Count);
            for (var i = 0; i < len; i++)
            {
                if(this.Count<other.Count) return -1;
                if(this.Count>other.Count) return +1;

                var a = this[i];
                var b = other[i];
                var aA = a.Average;
                var bA = b.Average;

                if (aA < bA || double.IsNaN(aA) && !double.IsNaN(bA)) return -1;
                if (aA > bA || !double.IsNaN(aA) && double.IsNaN(bA)) return +1;

                if (a.Count < b.Count) return -1;
                if (a.Count > b.Count) return +1;
            }
            return 0;
        }


    }
}