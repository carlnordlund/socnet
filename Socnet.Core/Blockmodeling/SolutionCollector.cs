namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// Collects the best partitions found by one search worker: the best fitness and the distinct partitions
    /// (up to a maximum number) having that fitness.
    /// </summary>
    internal sealed class SolutionCollector(int maxSolutions)
    {
        /// <summary>
        /// Fitness values within this (relative) tolerance are considered equal.
        /// </summary>
        public const double Tolerance = 1e-9;

        private readonly HashSet<PartitionKey> _seen = [];

        public double BestFitness { get; private set; } = double.NegativeInfinity;

        public List<int[]> Solutions { get; } = [];

        public long NbrTested { get; set; }

        /// <summary>
        /// True if more equally good partitions were found than could be kept.
        /// </summary>
        public bool LimitReached { get; private set; }

        /// <summary>
        /// Returns true if fitness a is better than fitness b (beyond the tolerance).
        /// </summary>
        public static bool IsBetter(double a, double b)
        {
            if (double.IsNegativeInfinity(b))
                return !double.IsNegativeInfinity(a);
            if (double.IsNegativeInfinity(a))
                return false;
            return a > b + Tolerance * Math.Max(1, Math.Abs(b));
        }

        /// <summary>
        /// Returns true if fitness a and b are equal within the tolerance.
        /// </summary>
        public static bool IsEqual(double a, double b) => !IsBetter(a, b) && !IsBetter(b, a);

        /// <summary>
        /// Offers a partition with its fitness. The labels are copied if kept.
        /// </summary>
        public void Offer(double fitness, int[] labels)
        {
            if (double.IsNegativeInfinity(fitness))
                return;
            if (IsBetter(fitness, BestFitness))
            {
                BestFitness = fitness;
                Solutions.Clear();
                _seen.Clear();
                LimitReached = false;
            }
            else if (!IsEqual(fitness, BestFitness))
                return;
            PartitionKey key = new(labels);
            if (_seen.Contains(key))
                return;
            if (Solutions.Count >= maxSolutions)
            {
                LimitReached = true;
                return;
            }
            _seen.Add(key);
            Solutions.Add(key.Labels);
        }
    }

    /// <summary>
    /// A partition (labels copied) usable as a hash key.
    /// </summary>
    internal readonly struct PartitionKey : IEquatable<PartitionKey>
    {
        private readonly int _hash;

        public PartitionKey(int[] labels)
        {
            Labels = (int[])labels.Clone();
            HashCode hc = new();
            foreach (int l in Labels)
                hc.Add(l);
            _hash = hc.ToHashCode();
        }

        public int[] Labels { get; }

        public bool Equals(PartitionKey other) => _hash == other._hash && Labels.AsSpan().SequenceEqual(other.Labels);

        public override bool Equals(object? obj) => obj is PartitionKey other && Equals(other);

        public override int GetHashCode() => _hash;
    }
}
