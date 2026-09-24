using Socnet.Core.Model;
using System.Text;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// Generates the single-blocked 'varieties' of a multi-blocked blockimage: all combinations of its ideal blocks
    /// that are non-trivial (no two positions are structurally equivalent) and mutually non-isomorphic (not identical
    /// under any permutation of the positions). Varieties are named [blockimagename]_[index], numbered in the order
    /// they are generated.
    ///
    /// Isomorphism is detected exactly by computing a canonical form of each combination: positions are first
    /// ordered by an isomorphism-invariant signature (their own ideal block and the multisets of ideal blocks in
    /// their row and column), and the canonical form is the lexicographically smallest matrix of ideal block types
    /// over all orderings consistent with the signatures. Two combinations are isomorphic if and only if their
    /// canonical forms are equal.
    /// </summary>
    public static class BlockImageVarieties
    {
        /// <summary>
        /// Returns the non-isomorphic, non-trivial single-blocked varieties of a (multi-blocked) blockimage.
        /// </summary>
        public static List<BlockImage> Generate(BlockImage template)
        {
            int k = template.NbrPositions;
            int[] maxIndices = new int[k * k], indices = new int[k * k], iso = new int[k * k];
            for (int i = 0; i < k * k; i++)
            {
                maxIndices[i] = template.Blocks(i / k, i % k).Count;
                iso[i] = template.GetBlock(i / k, i % k, 0).IsoIndex;
            }

            List<BlockImage> varieties = [];
            HashSet<string> seen = [];
            while (true)
            {
                if (!HasStructurallyEquivalentPositions(iso, k) && seen.Add(CanonicalForm(iso, k)))
                    varieties.Add(template.CreateSingleBlocked(template.Name + "_" + varieties.Count, (int[])indices.Clone()));

                // Next combination of ideal blocks (odometer over the block positions with several ideal blocks)
                int pos = 0;
                for (; pos < indices.Length; pos++)
                {
                    if (maxIndices[pos] <= 1)
                        continue;
                    indices[pos] = (indices[pos] + 1) % maxIndices[pos];
                    iso[pos] = template.GetBlock(pos / k, pos % k, indices[pos]).IsoIndex;
                    if (indices[pos] != 0)
                        break;
                }
                if (pos == indices.Length)
                    break;
            }
            return varieties;
        }

        /// <summary>
        /// True if two positions have identical rows and columns in the matrix of ideal block types,
        /// i.e. the blockimage could be reduced to fewer positions.
        /// </summary>
        private static bool HasStructurallyEquivalentPositions(int[] m, int k)
        {
            for (int a1 = 0; a1 < k; a1++)
                for (int a2 = a1 + 1; a2 < k; a2++)
                {
                    bool equivalent = true;
                    for (int i = 0; i < k && equivalent; i++)
                        if (m[a1 * k + i] != m[a2 * k + i] || m[i * k + a1] != m[i * k + a2])
                            equivalent = false;
                    if (equivalent)
                        return true;
                }
            return false;
        }

        /// <summary>
        /// Returns a canonical form of a k x k matrix of ideal block types: equal for two matrices if and only if
        /// one is a permutation (of rows and columns simultaneously) of the other.
        /// </summary>
        internal static string CanonicalForm(int[] m, int k)
        {
            // Order positions by an isomorphism-invariant signature; only permutations within groups of equal
            // signatures then have to be tried
            string[] signatures = new string[k];
            for (int p = 0; p < k; p++)
            {
                int[] row = new int[k], col = new int[k];
                for (int i = 0; i < k; i++)
                {
                    row[i] = m[p * k + i];
                    col[i] = m[i * k + p];
                }
                Array.Sort(row);
                Array.Sort(col);
                signatures[p] = $"{m[p * k + p]}|{string.Join(",", row)}|{string.Join(",", col)}";
            }
            int[] order = [.. Enumerable.Range(0, k).OrderBy(p => signatures[p], StringComparer.Ordinal)];
            List<(int start, int end)> groups = [];
            for (int start = 0; start < k;)
            {
                int end = start + 1;
                while (end < k && signatures[order[end]] == signatures[order[start]])
                    end++;
                groups.Add((start, end));
                start = end;
            }

            int[]? best = null;
            int[] candidate = new int[k * k];
            void Visit(int group)
            {
                if (group == groups.Count)
                {
                    for (int r = 0; r < k; r++)
                        for (int c = 0; c < k; c++)
                            candidate[r * k + c] = m[order[r] * k + order[c]];
                    if (best == null || candidate.AsSpan().SequenceCompareTo(best) < 0)
                        best = (int[])candidate.Clone();
                    return;
                }
                PermuteGroup(order, groups[group].start, groups[group].start, groups[group].end, () => Visit(group + 1));
            }
            Visit(0);

            StringBuilder sb = new();
            foreach (int v in best!)
                sb.Append(v).Append(',');
            return sb.ToString();
        }

        /// <summary>
        /// Calls 'action' for every permutation of order[start..end).
        /// </summary>
        private static void PermuteGroup(int[] order, int pos, int start, int end, Action action)
        {
            if (pos >= end - 1)
            {
                action();
                return;
            }
            for (int i = pos; i < end; i++)
            {
                (order[pos], order[i]) = (order[i], order[pos]);
                PermuteGroup(order, pos + 1, start, end, action);
                (order[pos], order[i]) = (order[i], order[pos]);
            }
        }
    }
}
