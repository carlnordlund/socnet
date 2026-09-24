using SocnetLegacy;
using SocnetLegacy.DataLibrary;
using SocnetLegacy.DataLibrary.Blocks;

namespace Socnet.Tests
{
    /// <summary>
    /// Evaluates blockmodels with the unchanged ideal block code of Socnet.se 1.4 (in the Legacy folder),
    /// replicating its 'binaryHamming' and 'nordlund2020' goodness-of-fit functions. Used as test oracle.
    /// </summary>
    internal static class LegacyOracle
    {
        /// <summary>
        /// Evaluates a partition with the 1.4 code. cellBlocks holds the block strings (e.g. "den(0.3)") of each
        /// block position (row-major); for nordlund only the first block of each position is used.
        /// </summary>
        public static (double gof, double[] ideal) Evaluate(double[] x, int n, int[] clusterOf, int k, string[][] cellBlocks, bool hamming)
        {
            Actorset actorset = new("legacy");
            for (int i = 0; i < n; i++)
                actorset.actors.Add(new Actor("a" + i, i));
            actorset.recreateLabelAndIndexToActor();
            Matrix matrix = new(actorset, "m", "N0");
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    matrix.data[i, j] = x[i * n + j];
            Cluster[] clusters = new Cluster[k];
            for (int c = 0; c < k; c++)
                clusters[c] = new Cluster("P" + c);
            for (int i = 0; i < n; i++)
                clusters[clusterOf[i]].addActor(actorset.actors[i]);

            Matrix idealMatrix = new(actorset, "ideal", "N0");
            double gof;
            if (hamming)
            {
                double penalty = 0;
                for (int r = 0; r < k; r++)
                    for (int c = 0; c < k; c++)
                    {
                        _Block[] blocks = [.. cellBlocks[r * k + c].Select(CreateBlock)];
                        double best = int.MaxValue;
                        int bestIndex = 0;
                        for (int i = 0; i < blocks.Length; i++)
                        {
                            double p = blocks[i].getPenaltyHamming(matrix, clusters[r], clusters[c]);
                            if (p < best)
                            {
                                best = p;
                                bestIndex = i;
                            }
                        }
                        penalty += best;
                        blocks[bestIndex].getPenaltyHamming(matrix, clusters[r], clusters[c], idealMatrix);
                    }
                gof = penalty;
            }
            else
            {
                List<Triple> triples = [];
                for (int r = 0; r < k; r++)
                    for (int c = 0; c < k; c++)
                        triples.AddRange(CreateBlock(cellBlocks[r * k + c][0]).getTripletList(matrix, clusters[r], clusters[c], idealMatrix));
                gof = Functions.correlateTriplets(triples);
            }
            double[] ideal = new double[n * n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    ideal[i * n + j] = idealMatrix.data[i, j];
            return (gof, ideal);
        }

        private static _Block CreateBlock(string blockString)
        {
            string name = blockString.Split('(')[0];
            Type type = Type.GetType("SocnetLegacy.DataLibrary.Blocks." + name + "Block")!;
            _Block block = (_Block)Activator.CreateInstance(type)!;
            if (blockString.Contains('('))
                block.initArgValue(double.Parse(blockString.Split('(')[1].TrimEnd(')'), System.Globalization.CultureInfo.InvariantCulture));
            return block;
        }
    }
}
