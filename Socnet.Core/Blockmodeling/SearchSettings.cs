using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// The available search algorithms.
    /// </summary>
    public enum SearchType
    {
        /// <summary>Checks all partitions.</summary>
        Exhaustive,
        /// <summary>Steepest-descent local optimization with (optional) pairwise switching.</summary>
        LocalOpt,
        /// <summary>Stochastic local optimization, moving on after a minimum number of better partitions has been found.</summary>
        Ljubljana
    }

    /// <summary>
    /// Settings for a direct blockmodeling search.
    /// </summary>
    public sealed class SearchSettings
    {
        /// <summary>The network to analyze.</summary>
        public required Matrix Network { get; init; }

        /// <summary>The blockimage (possibly multi-blocked).</summary>
        public required BlockImage BlockImage { get; init; }

        /// <summary>The search algorithm.</summary>
        public SearchType SearchType { get; init; } = SearchType.LocalOpt;

        /// <summary>The goodness-of-fit method.</summary>
        public GofMethod Method { get; init; } = GofMethod.Hamming;

        /// <summary>Minimum number of actors in each cluster.</summary>
        public int MinClusterSize { get; init; } = 1;

        /// <summary>Number of independent restarts (local searches).</summary>
        public int NbrRestarts { get; init; } = 50;

        /// <summary>Maximum number of iterations (steps) per restart (local searches).</summary>
        public int MaxIterations { get; init; } = 100;

        /// <summary>Number of random partitions from which the best is used as starting point for each restart.</summary>
        public int NbrRandomStart { get; init; } = 50;

        /// <summary>Number of better neighbors to find before moving on (ljubljana).</summary>
        public int MinNbrBetter { get; init; } = 5;

        /// <summary>Whether pairwise switching of actors is used (localopt).</summary>
        public bool DoSwitching { get; init; }

        /// <summary>Time limit in milliseconds (null for no limit).</summary>
        public long? MaxMilliseconds { get; init; } = 300000;

        /// <summary>Seed for the random number generators.</summary>
        public int Seed { get; init; }

        /// <summary>Maximum number of distinct optimal solutions kept.</summary>
        public int MaxSolutions { get; init; } = 100;

        /// <summary>Maximum degree of parallelism (null: number of processors).</summary>
        public int? MaxDegreeOfParallelism { get; init; }
    }

    /// <summary>
    /// A solution found by a search: a blockimage (a single-blocked variety for 'nordlund', otherwise the
    /// searched blockimage) and a partition.
    /// </summary>
    public sealed record SearchSolution(BlockImage BlockImage, int[] Partition, double Fitness);

    /// <summary>
    /// The result of a direct blockmodeling search.
    /// </summary>
    public sealed class SearchResult
    {
        /// <summary>The distinct optimal solutions found (empty if the search timed out).</summary>
        public List<SearchSolution> Solutions { get; init; } = [];

        /// <summary>Number of partitions that were evaluated.</summary>
        public long NbrTested { get; init; }

        /// <summary>True if more equally good solutions were found than the maximum number kept.</summary>
        public bool SolutionLimitReached { get; init; }

        /// <summary>True if the search was aborted because of the time limit.</summary>
        public bool TimedOut { get; init; }

        /// <summary>Execution time in milliseconds.</summary>
        public long ElapsedMilliseconds { get; init; }
    }
}
