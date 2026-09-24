using Socnet.Core.Blockmodeling;
using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.Tests
{
    /// <summary>
    /// Verifies that the ideal blocks of Socnet.se 2.0 give exactly the same goodness-of-fit values and ideal
    /// matrices as the (unchanged) Socnet.se 1.4 code, and that the incremental evaluation used in searches
    /// agrees with the full evaluation.
    /// </summary>
    public class BlockEquivalenceTests
    {
        public static TheoryData<int> Seeds => [.. Enumerable.Range(0, 40)];

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Hamming_MatchesLegacy(int seed)
        {
            Random rng = new(seed);
            for (int trial = 0; trial < 25; trial++)
            {
                int n = rng.Next(2, 13), k = rng.Next(1, Math.Min(n, 4) + 1);
                Matrix m = TestData.RandomNetwork(rng, n, trial % 3);
                int[] labels = TestData.RandomPartition(rng, n, k);
                string[][] cells = TestData.RandomCells(rng, k, TestData.HammingBlocks, 3);

                var (legacyGof, legacyIdeal) = LegacyOracle.Evaluate(m.Data, n, labels, k, cells, hamming: true);
                BlockmodelEvaluation eval = BlockmodelEvaluator.Evaluate(m, TestData.CreateBlockImage(cells, k), labels, GofMethod.Hamming);

                Assert.Equal(legacyGof, eval.Gof);
                AssertIdealEqual(legacyIdeal, eval.Ideal);
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Nordlund_MatchesLegacy(int seed)
        {
            Random rng = new(1000 + seed);
            for (int trial = 0; trial < 25; trial++)
            {
                int n = rng.Next(2, 13), k = rng.Next(1, Math.Min(n, 4) + 1);
                Matrix m = TestData.RandomNetwork(rng, n, trial % 3);
                int[] labels = TestData.RandomPartition(rng, n, k);
                string[][] cells = TestData.RandomCells(rng, k, TestData.NordlundBlocks, 1);

                var (legacyGof, legacyIdeal) = LegacyOracle.Evaluate(m.Data, n, labels, k, cells, hamming: false);
                BlockmodelEvaluation eval = BlockmodelEvaluator.Evaluate(m, TestData.CreateBlockImage(cells, k), labels, GofMethod.Nordlund);

                if (double.IsNaN(legacyGof))
                    Assert.True(double.IsNaN(eval.Gof));
                else
                    Assert.Equal(legacyGof, eval.Gof, 12);
                AssertIdealEqual(legacyIdeal, eval.Ideal);
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void IncrementalState_MatchesFullEvaluation(int seed)
        {
            Random rng = new(2000 + seed);
            foreach (GofMethod method in new[] { GofMethod.Hamming, GofMethod.Nordlund })
            {
                int n = rng.Next(3, 16), k = rng.Next(2, Math.Min(n, 5) + 1);
                Matrix m = TestData.RandomNetwork(rng, n, rng.Next(3));
                string[][] cells = method == GofMethod.Hamming
                    ? TestData.RandomCells(rng, k, TestData.HammingBlocks, 3)
                    : TestData.RandomCells(rng, k, TestData.NordlundBlocks, 1);
                BlockImage bi = TestData.CreateBlockImage(cells, k);
                SearchState state = new(new SearchProblem(m, bi, method, 1));
                int[] labels = TestData.RandomPartition(rng, n, k);
                state.SetPartition(labels);
                AssertFitness(m, bi, method, state);

                // Random moves, swaps and trial moves: the state must always agree with a full evaluation
                for (int step = 0; step < 60; step++)
                {
                    int v = rng.Next(n), b = rng.Next(k);
                    switch (rng.Next(4))
                    {
                        case 0:
                            state.TryMove(v, b);
                            break;
                        case 1:
                            state.TrySwap(v, rng.Next(n));
                            break;
                        default:
                            state.Move(v, b);
                            break;
                    }
                    AssertFitness(m, bi, method, state);
                }
            }
        }

        private static void AssertFitness(Matrix m, BlockImage bi, GofMethod method, SearchState state)
        {
            int[] labels = state.GetLabels();
            // Skip partitions with empty clusters (not used in searches)
            for (int c = 0; c < bi.NbrPositions; c++)
                if (!labels.Contains(c))
                {
                    state.Fitness();
                    return;
                }
            double expected = BlockmodelEvaluator.Evaluate(m, bi, labels, method).Gof;
            double fitness = state.Fitness();
            if (method == GofMethod.Hamming)
                Assert.Equal(-expected, fitness);
            else if (double.IsNaN(expected))
                Assert.True(double.IsNegativeInfinity(fitness));
            else
                Assert.Equal(expected, fitness, 9);
        }

        private static void AssertIdealEqual(double[] expected, double[] actual)
        {
            Assert.Equal(expected.Length, actual.Length);
            for (int i = 0; i < expected.Length; i++)
                Assert.True(expected[i].Equals(actual[i]), $"Ideal matrix differs at cell {i}: expected {expected[i]}, got {actual[i]}");
        }
    }
}
