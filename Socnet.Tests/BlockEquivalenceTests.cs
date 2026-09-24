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

        /// <summary>
        /// The ideal blocks that can be evaluated from block statistics under 'nordlund' (some only for networks
        /// with non-negative values and a zero diagonal).
        /// </summary>
        private static readonly string[] FastNordlundBlocks = ["dnc", "nul", "com", "denuci(0.3)", "reg", "rre", "cre", "rfn", "cfn", "pcdd", "cpdd"];

        [Theory]
        [MemberData(nameof(Seeds))]
        public void IncrementalState_MatchesFullEvaluation(int seed)
        {
            Random rng = new(2000 + seed);
            foreach (GofMethod method in new[] { GofMethod.Hamming, GofMethod.Nordlund })
            {
                string[] blocks = method == GofMethod.Hamming ? TestData.HammingBlocks : TestData.NordlundBlocks;
                RunIncrementalStateTest(rng, method, blocks, method == GofMethod.Hamming ? 3 : 1, zeroDiagonal: seed % 2 == 0, expectAllFast: null);
            }
        }

        [Theory]
        [MemberData(nameof(Seeds))]
        public void IncrementalState_FastNordlundBlocks_MatchFullEvaluation(int seed)
        {
            // Only blocks with constant-time evaluation: exercises the evaluation of trial moves without applying them
            Random rng = new(3000 + seed);
            bool zeroDiagonal = seed % 4 != 0;
            RunIncrementalStateTest(rng, GofMethod.Nordlund, FastNordlundBlocks, 1, zeroDiagonal, expectAllFast: zeroDiagonal);
            RunIncrementalStateTest(rng, GofMethod.Hamming, TestData.HammingBlocks, 3, zeroDiagonal, expectAllFast: true);
        }

        private static void RunIncrementalStateTest(Random rng, GofMethod method, string[] blocks, int maxPerCell, bool zeroDiagonal, bool? expectAllFast)
        {
            {
                int n = rng.Next(3, 16), k = rng.Next(2, Math.Min(n, 5) + 1);
                Matrix m = TestData.RandomNetwork(rng, n, rng.Next(3), zeroDiagonal);
                string[][] cells = TestData.RandomCells(rng, k, blocks, maxPerCell);
                if (expectAllFast == false)
                {
                    // Make sure that some block needs simple values (so that the fast path is not available)
                    cells[0] = ["rfn"];
                }
                BlockImage bi = TestData.CreateBlockImage(cells, k);
                SearchProblem problem = new(m, bi, method, 1);
                if (expectAllFast != null)
                    Assert.Equal(expectAllFast.Value, problem.AllCellsFast);
                SearchState state = new(problem);
                int[] labels = TestData.RandomPartition(rng, n, k);
                state.SetPartition(labels);
                AssertFitness(m, bi, method, state);

                // Random moves, swaps and trial moves: the state must always agree with a full evaluation
                for (int step = 0; step < 60; step++)
                {
                    int v = rng.Next(n), b = rng.Next(k);
                    switch (rng.Next(6))
                    {
                        case 2:
                            // Evaluating a whole partition must agree with a full evaluation
                            int[] other = TestData.RandomPartition(rng, n, k);
                            AssertFitness(m, bi, method, other, state.EvaluatePartition(other));
                            break;
                        case 3:
                            // Moving to a partition differing in a few actors (or many)
                            int[] target = state.GetLabels();
                            if (target.Contains(-1))
                                break;
                            int changes = rng.Next(2) == 0 ? 1 : n;
                            for (int c = 0; c < changes; c++)
                                target[rng.Next(n)] = rng.Next(k);
                            state.MoveTo(target);
                            Assert.Equal(target, state.GetLabels());
                            break;
                        case 0:
                            // The fitness of a trial move must equal that of the partition after the move
                            double tried = state.TryMove(v, b);
                            int[] moved = state.GetLabels();
                            moved[v] = b;
                            AssertFitness(m, bi, method, moved, tried);
                            break;
                        case 1:
                            int u = rng.Next(n);
                            double swapped = state.TrySwap(v, u);
                            int[] afterSwap = state.GetLabels();
                            (afterSwap[v], afterSwap[u]) = (afterSwap[u], afterSwap[v]);
                            AssertFitness(m, bi, method, afterSwap, swapped);
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
            => AssertFitness(m, bi, method, state.GetLabels(), state.Fitness());

        private static void AssertFitness(Matrix m, BlockImage bi, GofMethod method, int[] labels, double fitness)
        {
            // Skip partitions with empty clusters (not used in searches)
            for (int c = 0; c < bi.NbrPositions; c++)
                if (!labels.Contains(c))
                    return;
            double expected = BlockmodelEvaluator.Evaluate(m, bi, labels, method).Gof;
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
