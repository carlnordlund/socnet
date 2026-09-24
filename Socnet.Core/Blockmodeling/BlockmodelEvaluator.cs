using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// The full evaluation of a blockmodel: goodness-of-fit, the ideal block chosen in each position, and the ideal matrix.
    /// </summary>
    /// <param name="Gof">Goodness-of-fit (penalty for 'hamming', correlation for 'nordlund').</param>
    /// <param name="BlockIndices">Index of the chosen ideal block in each block position (row-major k x k).</param>
    /// <param name="Ideal">Ideal values, row-major n x n in the actor order of the network.</param>
    public sealed record BlockmodelEvaluation(double Gof, int[] BlockIndices, double[] Ideal);

    /// <summary>
    /// Evaluates a given partition of a network against a blockimage. Used for hypothesis testing ('bmtest')
    /// and for creating the final blockmodels of a search. The goodness-of-fit is computed exactly as in
    /// Socnet.se 1.4.
    /// </summary>
    public static class BlockmodelEvaluator
    {
        /// <summary>
        /// Evaluates the partition (cluster index per actor, 0..k-1) of the network against the blockimage.
        /// With 'hamming', the best-fitting ideal block is chosen in each position of a multi-blocked blockimage;
        /// with 'nordlund', the first ideal block in each position is used.
        /// </summary>
        public static BlockmodelEvaluation Evaluate(Matrix network, BlockImage blockimage, int[] clusterOf, GofMethod method)
        {
            int n = network.N, k = blockimage.NbrPositions;
            int[][] members = new int[k][];
            List<int>[] lists = new List<int>[k];
            for (int c = 0; c < k; c++)
                lists[c] = [];
            for (int i = 0; i < n; i++)
                lists[clusterOf[i]].Add(i);
            for (int c = 0; c < k; c++)
                members[c] = [.. lists[c]];

            double[] ideal = new double[n * n];
            double[] scratch = new double[Math.Max(1, n * n)];
            int[] blockIndices = new int[k * k];

            if (method == GofMethod.Hamming)
            {
                double penalty = 0;
                for (int r = 0; r < k; r++)
                    for (int c = 0; c < k; c++)
                    {
                        BlockRegion region = new(members[r], members[c], r == c, network.Data, n, scratch);
                        List<IdealBlock> candidates = blockimage.Blocks(r, c);
                        double best = double.MaxValue;
                        for (int i = 0; i < candidates.Count; i++)
                        {
                            double p = candidates[i].Hamming(region);
                            if (p < best)
                            {
                                best = p;
                                blockIndices[r * k + c] = i;
                            }
                        }
                        penalty += best;
                        candidates[blockIndices[r * k + c]].WriteIdealHamming(region, ideal);
                    }
                return new BlockmodelEvaluation(penalty, blockIndices, ideal);
            }

            TripleList triples = new();
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                {
                    BlockRegion region = new(members[r], members[c], r == c, network.Data, n, scratch);
                    IdealBlock block = blockimage.GetBlock(r, c);
                    block.Nordlund(region, triples);
                    block.WriteIdealNordlund(region, ideal);
                }
            return new BlockmodelEvaluation(triples.Correlation(), blockIndices, ideal);
        }

        /// <summary>
        /// Creates a BlockModel for the given partition, with a single-blocked copy of the blockimage.
        /// </summary>
        /// <param name="name">Name of the blockmodel.</param>
        /// <param name="network">The network.</param>
        /// <param name="blockimage">The (possibly multi-blocked, if 'hamming') blockimage.</param>
        /// <param name="partition">The partition (its number of clusters must equal the number of positions).</param>
        /// <param name="method">The goodness-of-fit method.</param>
        public static BlockModel CreateBlockModel(string name, Matrix network, BlockImage blockimage, Partition partition, GofMethod method)
        {
            BlockmodelEvaluation eval = Evaluate(network, blockimage, partition.ClusterOf, method);
            BlockImage single = blockimage.CreateSingleBlocked(blockimage.Name, eval.BlockIndices);
            return new BlockModel(name, network, single, partition, eval.Gof, method, eval.Ideal);
        }

        /// <summary>
        /// Checks whether two blockmodels are equivalent: their partitions are identical up to a relabelling of
        /// the clusters, and under that relabelling their blockimages have the same types of ideal blocks.
        /// </summary>
        public static bool AreIdentical(BlockModel bm1, BlockModel bm2)
        {
            Partition p1 = bm1.Partition, p2 = bm2.Partition;
            if (p1.Actorset != p2.Actorset || p1.NbrClusters != p2.NbrClusters)
                return false;
            int k = p1.NbrClusters;
            int[] map12 = new int[k], map21 = new int[k];
            Array.Fill(map12, -1);
            Array.Fill(map21, -1);
            for (int i = 0; i < p1.ClusterOf.Length; i++)
            {
                int c1 = p1.ClusterOf[i], c2 = p2.ClusterOf[i];
                if (map12[c1] < 0 && map21[c2] < 0)
                {
                    map12[c1] = c2;
                    map21[c2] = c1;
                }
                else if (map12[c1] != c2 || map21[c2] != c1)
                    return false;
            }
            BlockImage bi1 = bm1.BlockImage, bi2 = bm2.BlockImage;
            if (bi1.NbrPositions != bi2.NbrPositions)
                return false;
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                {
                    if (map12[r] < 0 || map12[c] < 0)
                        return false;
                    if (bi1.GetBlock(r, c).IsoIndex != bi2.GetBlock(map12[r], map12[c]).IsoIndex)
                        return false;
                }
            return true;
        }
    }
}
