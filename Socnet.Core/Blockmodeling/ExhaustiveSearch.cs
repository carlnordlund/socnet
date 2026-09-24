namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// Exhaustive search: evaluates every partition of the actors into k clusters satisfying the minimum
    /// cluster size. Partitions are enumerated depth-first, assigning one actor at a time, so that each
    /// partition is evaluated incrementally. If the blockimage is invariant under all permutations of its
    /// positions, each partition is only evaluated once (instead of once per relabelling of its clusters).
    /// The enumeration is split into independent subtrees (prefixes) that can be searched in parallel.
    /// </summary>
    internal static class ExhaustiveSearch
    {
        /// <summary>
        /// Splits the search into subtrees: returns assignments of the first actors, enough to keep the
        /// available processors busy.
        /// </summary>
        public static List<int[]> GetPrefixes(SearchProblem p, int minNbrPrefixes)
        {
            List<int[]> prefixes = [[]];
            int depth = 0;
            while (prefixes.Count < minNbrPrefixes && depth < p.N - 1)
            {
                List<int[]> next = [];
                foreach (int[] prefix in prefixes)
                {
                    int maxUsed = prefix.Length == 0 ? -1 : prefix.Max();
                    foreach (int label in Options(p, maxUsed))
                    {
                        int[] extended = [.. prefix, label];
                        if (IsFeasible(p, extended))
                            next.Add(extended);
                    }
                }
                prefixes = next;
                depth++;
            }
            return prefixes;
        }

        private static IEnumerable<int> Options(SearchProblem p, int maxUsed)
        {
            int maxLabel = p.IsFullySymmetric ? Math.Min(maxUsed + 1, p.K - 1) : p.K - 1;
            for (int label = 0; label <= maxLabel; label++)
                yield return label;
        }

        private static bool IsFeasible(SearchProblem p, int[] prefix)
        {
            int[] size = new int[p.K];
            foreach (int label in prefix)
                size[label]++;
            int needed = 0;
            foreach (int s in size)
                needed += Math.Max(0, p.MinClusterSize - s);
            return needed <= p.N - prefix.Length;
        }

        /// <summary>
        /// Searches all partitions starting with the given prefix.
        /// </summary>
        public static void Run(SearchProblem p, int[] prefix, SolutionCollector collector, CancellationToken token)
        {
            SearchState state = new(p);
            int[] labels = new int[p.N];
            Array.Fill(labels, -1);
            int maxUsed = -1;
            for (int i = 0; i < prefix.Length; i++)
            {
                labels[i] = prefix[i];
                maxUsed = Math.Max(maxUsed, prefix[i]);
            }
            state.SetPartition(labels);
            int[] size = new int[p.K];
            for (int c = 0; c < p.K; c++)
                size[c] = state.Size(c);
            int needed = 0;
            foreach (int s in size)
                needed += Math.Max(0, p.MinClusterSize - s);
            Search(p, state, prefix.Length, maxUsed, needed, collector, token);
        }

        private static void Search(SearchProblem p, SearchState state, int actor, int maxUsed, int needed, SolutionCollector collector, CancellationToken token)
        {
            if (actor == p.N)
            {
                if (needed > 0)
                    return;
                double fitness = state.Fitness();
                collector.Offer(fitness, state.ClusterOf);
                if ((++collector.NbrTested & 0xFFF) == 0)
                    token.ThrowIfCancellationRequested();
                return;
            }
            int remaining = p.N - actor;
            if (needed > remaining)
                return;
            int maxLabel = p.IsFullySymmetric ? Math.Min(maxUsed + 1, p.K - 1) : p.K - 1;
            for (int label = 0; label <= maxLabel; label++)
            {
                // If all remaining actors are needed to fill clusters, only put this actor in a cluster that needs it
                bool fillsGap = state.Size(label) < p.MinClusterSize;
                if (needed == remaining && !fillsGap)
                    continue;
                state.Insert(actor, label);
                Search(p, state, actor + 1, Math.Max(maxUsed, label), needed - (fillsGap ? 1 : 0), collector, token);
                state.Remove(actor);
            }
        }
    }
}
