using Socnet.Core.Blocks;
using Socnet.Core.Model;
using System.Diagnostics;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// Runs direct blockmodeling searches. The work is split into independent tasks (restarts of the local
    /// searches, or subtrees of the exhaustive search, for each blockimage variety) that run in parallel.
    /// Each task has its own random number generator derived from the seed, and results are merged in task
    /// order, so that the result for a given seed does not depend on the number of processors.
    /// </summary>
    public static class BlockmodelSearch
    {
        /// <summary>
        /// Returns the blockimages to search: the single-blocked varieties of a multi-blocked blockimage for
        /// 'nordlund' (where each position must hold a single ideal block), otherwise the blockimage itself.
        /// </summary>
        public static List<BlockImage> GetSearchBlockImages(BlockImage blockimage, GofMethod method)
            => method == GofMethod.Nordlund && blockimage.MultiBlocked
                ? BlockImageVarieties.Generate(blockimage)
                : [blockimage];

        /// <summary>
        /// Runs a search over the given blockimages (see <see cref="GetSearchBlockImages"/>).
        /// </summary>
        public static SearchResult Run(SearchSettings settings, IReadOnlyList<BlockImage> blockimages)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            List<SearchProblem> problems = [.. blockimages.Select(bi => new SearchProblem(settings.Network, bi, settings.Method, settings.MinClusterSize))];
            int parallelism = settings.MaxDegreeOfParallelism ?? Environment.ProcessorCount;

            // Split the work into independent tasks
            List<(int problem, int[]? prefix, int restart)> tasks = [];
            for (int p = 0; p < problems.Count; p++)
            {
                if (settings.SearchType == SearchType.Exhaustive)
                {
                    foreach (int[] prefix in ExhaustiveSearch.GetPrefixes(problems[p], Math.Max(1, 8 * parallelism / problems.Count)))
                        tasks.Add((p, prefix, 0));
                }
                else
                    for (int r = 0; r < Math.Max(1, settings.NbrRestarts); r++)
                        tasks.Add((p, null, r));
            }

            SolutionCollector[] collectors = new SolutionCollector[tasks.Count];
            using CancellationTokenSource cts = settings.MaxMilliseconds is long ms ? new(TimeSpan.FromMilliseconds(ms)) : new();
            bool timedOut = false;
            try
            {
                Parallel.For(0, tasks.Count, new ParallelOptions { MaxDegreeOfParallelism = parallelism, CancellationToken = cts.Token }, i =>
                {
                    var (p, prefix, restart) = tasks[i];
                    SolutionCollector collector = new(settings.MaxSolutions);
                    collectors[i] = collector;
                    if (prefix != null)
                        ExhaustiveSearch.Run(problems[p], prefix, collector, cts.Token);
                    else
                        LocalSearch.RunRestart(problems[p], settings, new Random(TaskSeed(settings.Seed, p, restart)), collector, cts.Token);
                });
            }
            catch (OperationCanceledException)
            {
                timedOut = true;
            }
            catch (AggregateException ae) when (ae.InnerExceptions.All(e => e is OperationCanceledException))
            {
                timedOut = true;
            }

            long nbrTested = collectors.Where(c => c != null).Sum(c => c.NbrTested);
            if (timedOut)
                return new SearchResult { TimedOut = true, NbrTested = nbrTested, ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };

            // Merge: the distinct partitions (per blockimage) having the best fitness, in task order
            double best = double.NegativeInfinity;
            foreach (SolutionCollector c in collectors)
                if (SolutionCollector.IsBetter(c.BestFitness, best))
                    best = c.BestFitness;
            List<SearchSolution> solutions = [];
            HashSet<(int, PartitionKey)> seen = [];
            bool limitReached = false;
            for (int i = 0; i < tasks.Count; i++)
            {
                if (double.IsNegativeInfinity(collectors[i].BestFitness) || !SolutionCollector.IsEqual(collectors[i].BestFitness, best))
                    continue;
                limitReached |= collectors[i].LimitReached;
                int p = tasks[i].problem;
                foreach (int[] labels in collectors[i].Solutions)
                    if (seen.Add((p, new PartitionKey(labels))))
                    {
                        if (solutions.Count < settings.MaxSolutions)
                            solutions.Add(new SearchSolution(problems[p].BlockImage, labels, collectors[i].BestFitness));
                        else
                            limitReached = true;
                    }
            }
            return new SearchResult { Solutions = solutions, NbrTested = nbrTested, SolutionLimitReached = limitReached, ElapsedMilliseconds = stopwatch.ElapsedMilliseconds };
        }

        /// <summary>
        /// Creates the BlockModel objects for the solutions of a search, skipping solutions equivalent to earlier ones.
        /// Blockmodels are named [outname]_i or bm_[network]_[blockimage]_i, and their partitions part_[network]_[blockimage]_i.
        /// </summary>
        public static List<BlockModel> CreateBlockModels(SearchSettings settings, SearchResult result, string outname = "")
        {
            List<BlockModel> blockmodels = [];
            Matrix network = settings.Network;
            int index = 0;
            foreach (SearchSolution solution in result.Solutions)
            {
                BlockImage bi = solution.BlockImage;
                string basename = outname.Length > 0 ? outname : $"bm_{network.Name}_{bi.Name}";
                Partition partition = new(network.Actorset, $"part_{network.Name}_{bi.Name}_{index}", bi.PositionNames, solution.Partition);
                BlockModel blockmodel = BlockmodelEvaluator.CreateBlockModel(basename + "_" + index, network, bi, partition, settings.Method);
                if (blockmodels.Any(bm => BlockmodelEvaluator.AreIdentical(blockmodel, bm)))
                    continue;
                blockmodels.Add(blockmodel);
                index++;
            }
            return blockmodels;
        }

        /// <summary>
        /// Derives a seed for a task (SplitMix64 mixing of the base seed and the task identity).
        /// </summary>
        private static int TaskSeed(int seed, int problem, int restart)
        {
            ulong z = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + (ulong)problem * 0xBF58476D1CE4E5B9UL + (ulong)restart * 0x94D049BB133111EBUL + 1;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            z ^= z >> 31;
            return (int)(z & 0x7FFFFFFF);
        }
    }
}
