using Socnet.Core.Blockmodeling;
using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.Tests
{
    /// <summary>
    /// Tests of the search algorithms and blockimage varieties.
    /// </summary>
    public class SearchTests
    {
        /// <summary>
        /// Brute force over all k^n labellings with the full evaluator.
        /// </summary>
        private static double BruteForceBest(Matrix m, BlockImage bi, GofMethod method, int minClusterSize)
        {
            int n = m.N, k = bi.NbrPositions;
            int[] labels = new int[n];
            double best = double.NegativeInfinity;
            long total = (long)Math.Pow(k, n);
            for (long code = 0; code < total; code++)
            {
                long rest = code;
                int[] sizes = new int[k];
                for (int i = 0; i < n; i++)
                {
                    labels[i] = (int)(rest % k);
                    rest /= k;
                    sizes[labels[i]]++;
                }
                if (sizes.Any(s => s < minClusterSize))
                    continue;
                double gof = BlockmodelEvaluator.Evaluate(m, bi, labels, method).Gof;
                double fitness = method == GofMethod.Hamming ? -gof : gof;
                if (!double.IsNaN(fitness) && fitness > best)
                    best = fitness;
            }
            return best;
        }

        public static TheoryData<int> Seeds => [.. Enumerable.Range(0, 12)];

        [Theory]
        [MemberData(nameof(Seeds))]
        public void Exhaustive_FindsTheOptimum(int seed)
        {
            Random rng = new(seed);
            GofMethod method = seed % 2 == 0 ? GofMethod.Hamming : GofMethod.Nordlund;
            int minClusterSize = 1 + seed % 2;
            int k = rng.Next(2, 4), n = Math.Max(rng.Next(4, 9), k * minClusterSize);
            Matrix m = TestData.RandomNetwork(rng, n, rng.Next(3));
            // Alternate between symmetric blockimages (enumerated as set partitions) and asymmetric ones
            string[][] cells = seed % 3 == 0
                ? [.. Enumerable.Range(0, k * k).Select(_ => method == GofMethod.Hamming ? new[] { "nul", "com" } : ["reg"])]
                : TestData.RandomCells(rng, k, method == GofMethod.Hamming ? TestData.HammingBlocks : TestData.NordlundBlocks, method == GofMethod.Hamming ? 2 : 1);
            BlockImage bi = TestData.CreateBlockImage(cells, k);

            SearchSettings settings = new() { Network = m, BlockImage = bi, SearchType = SearchType.Exhaustive, Method = method, MinClusterSize = minClusterSize, MaxMilliseconds = null };
            SearchResult result = BlockmodelSearch.Run(settings, [bi]);

            double expected = BruteForceBest(m, bi, method, minClusterSize);
            Assert.NotEmpty(result.Solutions);
            foreach (SearchSolution solution in result.Solutions)
            {
                double gof = BlockmodelEvaluator.Evaluate(m, bi, solution.Partition, method).Gof;
                Assert.Equal(expected, method == GofMethod.Hamming ? -gof : gof, 9);
            }
        }

        [Fact]
        public void Exhaustive_SymmetricBlockimage_EvaluatesEachSetPartitionOnce()
        {
            Matrix m = TestData.LoadExample("little_league_ti.txt");
            BlockImage bi = new("bi3se", 3);
            bi.SetBlocksByPattern("nul;com");
            SearchSettings settings = new() { Network = m, BlockImage = bi, SearchType = SearchType.Exhaustive, Method = GofMethod.Hamming };
            SearchResult result = BlockmodelSearch.Run(settings, [bi]);
            // Stirling number of the second kind S(13,3)
            Assert.Equal(261625, result.NbrTested);
            List<BlockModel> blockmodels = BlockmodelSearch.CreateBlockModels(settings, result);
            Assert.Equal(2, blockmodels.Count);
            Assert.All(blockmodels, bm => Assert.Equal(23, bm.Gof));
        }

        [Theory]
        [InlineData(SearchType.LocalOpt, false)]
        [InlineData(SearchType.LocalOpt, true)]
        [InlineData(SearchType.Ljubljana, false)]
        public void LocalSearches_FindKnownOptimum_StructuralEquivalence(SearchType searchType, bool doSwitching)
        {
            // Test 1 of TESTING.md: 4-positional structural equivalence of Little League (TI), optimum 20
            Matrix m = TestData.LoadExample("little_league_ti.txt");
            BlockImage bi = new("bi4se", 4);
            bi.SetBlocksByPattern("nul;com");
            SearchSettings settings = new() { Network = m, BlockImage = bi, SearchType = searchType, Method = GofMethod.Hamming, DoSwitching = doSwitching, Seed = 42 };
            SearchResult result = BlockmodelSearch.Run(settings, [bi]);
            List<BlockModel> blockmodels = BlockmodelSearch.CreateBlockModels(settings, result);
            BlockModel bm = Assert.Single(blockmodels);
            Assert.Equal(20, bm.Gof);
            // Clusters: {Ron, Frank, Boyd, Tim}, {John, Jerry, Darrin, Ben, Arnie}, {Tom}, {Jeff, Jay, Sandy}
            string[][] expected = [["Ron__1", "Frank_3", "Boyd_4", "Tim__5"], ["John_6", "Jerry_10", "Darrin_11", "Ben_12", "Arnie_13"], ["Tom__2"], ["Jeff_7", "Jay__8", "Sandy_9"]];
            HashSet<string> found = [.. bm.Partition.AllMembers().Select(c => string.Join(",", c.Select(a => m.Actorset.Label(a)).Order()))];
            Assert.True(found.SetEquals(expected.Select(c => string.Join(",", c.Order()))));
        }

        [Fact]
        public void Ljubljana_FindsKnownOptimum_RegularEquivalence()
        {
            // Test 2 of TESTING.md: 3-positional regular equivalence of Hlebec, varieties of nul;reg, optimum 0.8813
            Matrix m = TestData.LoadExample("hlebec.txt");
            BlockImage bi = new("bi3re", 3);
            bi.SetBlocksByPattern("nul;reg");
            List<BlockImage> varieties = BlockmodelSearch.GetSearchBlockImages(bi, GofMethod.Nordlund);
            Assert.Equal(88, varieties.Count);
            SearchSettings settings = new() { Network = m, BlockImage = bi, SearchType = SearchType.Ljubljana, Method = GofMethod.Nordlund, Seed = 7 };
            SearchResult result = BlockmodelSearch.Run(settings, varieties);
            BlockModel bm = Assert.Single(BlockmodelSearch.CreateBlockModels(settings, result));
            Assert.Equal(0.8813, bm.Gof);
            Assert.Equal(CanonicalBlocks(HlebecOptimum()), CanonicalBlocks(bm.BlockImage));
        }

        [Fact]
        public void Search_IsDeterministicForAGivenSeed_RegardlessOfParallelism()
        {
            Matrix m = TestData.LoadExample("baker.txt");
            BlockImage bi = new("bi3", 3);
            bi.SetBlocksByPattern("nul;com");
            string Run(int? parallelism)
            {
                SearchSettings settings = new() { Network = m, BlockImage = bi, SearchType = SearchType.Ljubljana, Method = GofMethod.Hamming, Seed = 123, NbrRestarts = 20, MaxDegreeOfParallelism = parallelism };
                SearchResult result = BlockmodelSearch.Run(settings, [bi]);
                return result.NbrTested + ":" + string.Join("|", result.Solutions.Select(s => string.Join(",", s.Partition)));
            }
            Assert.Equal(Run(1), Run(null));
        }

        [Fact]
        public void Varieties_AreNonTrivialAndNonIsomorphic()
        {
            // Same numbers of varieties as in Socnet.se 1.4 (which used eigenvalues to detect isomorphism)
            BlockImage bi2 = new("bi2re", 2);
            bi2.SetBlocksByPattern("nul;reg");
            List<BlockImage> v2 = BlockImageVarieties.Generate(bi2);
            Assert.Equal(8, v2.Count);
            Assert.Equal([.. Enumerable.Range(0, 8).Select(i => "bi2re_" + i)], v2.Select(v => v.Name));

            BlockImage bi3 = new("bi3re", 3);
            bi3.SetBlocksByPattern("nul;reg");
            List<BlockImage> v3 = BlockImageVarieties.Generate(bi3);
            Assert.Equal(88, v3.Count);
            // The optimal variety for Hlebec is among them
            BlockImage best = Assert.Single(v3, v => CanonicalBlocks(v).SequenceEqual(CanonicalBlocks(HlebecOptimum())));

            // Extended blockimage from the tutorial script: 122 varieties
            Assert.Equal(122, BlockImageVarieties.Generate(best.Extend("nul;reg")).Count);

            // All varieties are mutually non-isomorphic: brute force over all position permutations
            int[][] perms = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
            HashSet<string> forms = [];
            foreach (BlockImage v in v3)
            {
                string[] blocks = Blocks(v);
                string min = perms.Select(p => string.Join(",", Enumerable.Range(0, 9).Select(i => blocks[p[i / 3] * 3 + p[i % 3]]))).Min(StringComparer.Ordinal)!;
                Assert.True(forms.Add(min));
            }
        }

        [Fact]
        public void Varieties_KeepBlockParametersAndPositionNames()
        {
            BlockImage bi = new("cp", 2);
            bi.PositionNames[0] = "C";
            bi.PositionNames[1] = "P";
            bi.SetBlock(0, 0, "pco(0.75)");
            bi.SetBlock(0, 1, "den(0.2);dnc");
            bi.SetBlock(1, 0, "den(0.2);dnc");
            bi.SetBlock(1, 1, "nul");
            List<BlockImage> varieties = BlockImageVarieties.Generate(bi);
            Assert.NotEmpty(varieties);
            Assert.All(varieties, v =>
            {
                Assert.Equal(["C", "P"], v.PositionNames);
                Assert.Equal("pco(0.75)", v.GetBlock(0, 0).ToString());
            });
            Assert.Contains(varieties, v => v.GetBlock(0, 1).ToString() == "den(0.2)");
        }

        [Fact]
        public void Timeout_IsReported()
        {
            Random rng = new(1);
            Matrix m = TestData.RandomNetwork(rng, 60, 0);
            BlockImage bi = new("bi", 6);
            bi.SetBlocksByPattern("nul;com;reg");
            SearchSettings settings = new() { Network = m, BlockImage = bi, SearchType = SearchType.Exhaustive, Method = GofMethod.Hamming, MaxMilliseconds = 200 };
            SearchResult result = BlockmodelSearch.Run(settings, [bi]);
            Assert.True(result.TimedOut);
            Assert.Empty(result.Solutions);
        }

        /// <summary>
        /// The optimal 3-positional regular blockimage for Hlebec (from TESTING.md).
        /// </summary>
        private static BlockImage HlebecOptimum()
            => TestData.CreateBlockImage([["reg"], ["nul"], ["nul"], ["reg"], ["reg"], ["nul"], ["reg"], ["nul"], ["nul"]], 3);

        /// <summary>
        /// The blocks of a 3x3 blockimage in the canonical (lexicographically smallest) ordering of its positions.
        /// </summary>
        private static string[] CanonicalBlocks(BlockImage bi)
        {
            int[][] perms = [[0, 1, 2], [0, 2, 1], [1, 0, 2], [1, 2, 0], [2, 0, 1], [2, 1, 0]];
            string[] blocks = Blocks(bi);
            return perms.Select(p => Enumerable.Range(0, 9).Select(i => blocks[p[i / 3] * 3 + p[i % 3]]).ToArray())
                .OrderBy(b => string.Join(",", b), StringComparer.Ordinal).First();
        }

        private static string[] Blocks(BlockImage bi)
        {
            List<string> blocks = [];
            for (int r = 0; r < bi.NbrPositions; r++)
                for (int c = 0; c < bi.NbrPositions; c++)
                    blocks.Add(bi.GetBlock(r, c).ToString());
            return [.. blocks];
        }
    }
}
