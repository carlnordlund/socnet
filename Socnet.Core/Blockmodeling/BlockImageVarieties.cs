using Socnet.Core.Blocks;
using Socnet.Core.Model;
using Socnet.Core.Numerics;
using System.Text;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// Generates the single-blocked 'varieties' of a multi-blocked blockimage: all combinations of its ideal blocks
    /// that are non-trivial (no two positions are structurally equivalent) and mutually non-isomorphic.
    /// The varieties are generated and named (blockimagename_index) in the same order as in Socnet.se 1.4.
    /// </summary>
    public static class BlockImageVarieties
    {
        /// <summary>
        /// Returns the non-isomorphic, non-trivial single-blocked varieties of a (multi-blocked) blockimage.
        /// </summary>
        public static List<BlockImage> Generate(BlockImage template)
        {
            int k = template.NbrPositions;
            int[,] maxIndices = new int[k, k], indices = new int[k, k];
            double[,] isoMatrix = new double[k, k];
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                {
                    maxIndices[r, c] = template.Blocks(r, c).Count;
                    isoMatrix[r, c] = template.GetBlock(r, c, 0).IsoIndex;
                }

            // Candidates grouped by the key of their (rounded, real) eigenvalues, in order of first appearance
            Dictionary<string, List<int[,]>> groups = [];
            List<string> groupOrder = [];

            while (true)
            {
                if (!HasStructurallyEquivalentPositions(isoMatrix, k))
                {
                    string key = EigenvalueKey(isoMatrix);
                    int[,] current = new int[k, k];
                    for (int r = 0; r < k; r++)
                        for (int c = 0; c < k; c++)
                            current[r, c] = indices[r, c];
                    if (!groups.TryGetValue(key, out var group))
                    {
                        groups[key] = [current];
                        groupOrder.Add(key);
                    }
                    else if (!group.Any(existing => AreIsomorphic(template, existing, current, k)))
                        group.Add(current);
                }

                // Increment to the next combination of ideal blocks
                bool increased = false;
                for (int r = 0; r < k && !increased; r++)
                    for (int c = 0; c < k && !increased; c++)
                    {
                        if (maxIndices[r, c] <= 1)
                            continue;
                        indices[r, c]++;
                        if (indices[r, c] < maxIndices[r, c])
                            increased = true;
                        else
                            indices[r, c] = 0;
                        isoMatrix[r, c] = template.GetBlock(r, c, indices[r, c]).IsoIndex;
                    }
                if (!increased)
                    break;
            }

            List<BlockImage> varieties = [];
            int index = 0;
            foreach (string key in groupOrder)
                foreach (int[,] combination in groups[key])
                {
                    int[] flat = new int[k * k];
                    for (int r = 0; r < k; r++)
                        for (int c = 0; c < k; c++)
                            flat[r * k + c] = combination[r, c];
                    varieties.Add(template.CreateSingleBlocked(template.Name + "_" + index, flat));
                    index++;
                }
            return varieties;
        }

        /// <summary>
        /// True if two positions have identical rows and columns in the matrix of ideal block indices.
        /// </summary>
        private static bool HasStructurallyEquivalentPositions(double[,] m, int k)
        {
            for (int a1 = 0; a1 < k; a1++)
                for (int a2 = a1 + 1; a2 < k; a2++)
                {
                    bool equivalent = true;
                    for (int i = 0; i < k && equivalent; i++)
                        if (m[a1, i] != m[a2, i] || m[i, a1] != m[i, a2])
                            equivalent = false;
                    if (equivalent)
                        return true;
                }
            return false;
        }

        /// <summary>
        /// Key from the real parts of the eigenvalues, sorted descending and rounded to 4 decimals.
        /// </summary>
        private static string EigenvalueKey(double[,] m)
        {
            double[] eigenvalues = (double[])new EigenvalueDecomposition(m).RealEigenvalues.Clone();
            Array.Sort(eigenvalues);
            Array.Reverse(eigenvalues);
            StringBuilder sb = new();
            foreach (double ev in eigenvalues)
            {
                double v = Math.Round(ev, 4);
                if (v == 0)
                    v = 0;
                sb.Append(Utilities.Fmt.D(v)).Append(';');
            }
            return sb.ToString();
        }

        /// <summary>
        /// True if the two block combinations are identical under some permutation of the positions.
        /// </summary>
        private static bool AreIsomorphic(BlockImage template, int[,] existing, int[,] current, int k)
        {
            int[] perm = new int[k];
            for (int i = 0; i < k; i++)
                perm[i] = i;
            return Permute(perm, 0, template, existing, current, k);
        }

        private static bool Permute(int[] perm, int pos, BlockImage template, int[,] existing, int[,] current, int k)
        {
            if (pos == k - 1 || k == 1)
            {
                for (int r = 0; r < k; r++)
                    for (int c = 0; c < k; c++)
                        if (template.GetBlock(r, c, existing[r, c]).IsoIndex != template.GetBlock(perm[r], perm[c], current[perm[r], perm[c]]).IsoIndex)
                            return false;
                return true;
            }
            for (int i = pos; i < k; i++)
            {
                (perm[pos], perm[i]) = (perm[i], perm[pos]);
                bool found = Permute(perm, pos + 1, template, existing, current, k);
                (perm[pos], perm[i]) = (perm[i], perm[pos]);
                if (found)
                    return true;
            }
            return false;
        }
    }
}
