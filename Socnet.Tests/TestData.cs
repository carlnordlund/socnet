using Socnet.Core.Model;

namespace Socnet.Tests
{
    /// <summary>
    /// Helpers for generating random test networks and partitions, and for loading the example data.
    /// </summary>
    internal static class TestData
    {
        /// <summary>The block strings usable with the 'hamming' measure.</summary>
        public static readonly string[] HammingBlocks = ["dnc", "nul", "com", "reg", "rre", "cre", "rfn", "cfn", "den(0.3)", "den(0.5)", "denmin(0.25)", "denmin(0.6)"];

        /// <summary>The block strings usable with the 'nordlund' measure.</summary>
        public static readonly string[] NordlundBlocks = [.. HammingBlocks, "denuci(0.4)", "pco(0.5)", "pco(0.75)", "pcdd", "cpdd"];

        /// <summary>
        /// A random network: binary, small integer-valued (with ties), or continuous; diagonal values may be non-zero
        /// unless zeroDiagonal is set.
        /// </summary>
        public static Matrix RandomNetwork(Random rng, int n, int kind, bool zeroDiagonal = false)
        {
            Actorset actorset = Actorset.Create("actors", [.. Enumerable.Range(0, n).Select(i => "a" + i)])!;
            Matrix m = new(actorset, "net");
            double density = 0.15 + 0.6 * rng.NextDouble();
            for (int i = 0; i < m.Data.Length; i++)
            {
                if (rng.NextDouble() > density)
                    continue;
                m.Data[i] = kind switch
                {
                    0 => 1,
                    1 => rng.Next(1, 4),
                    _ => Math.Round(rng.NextDouble() * 3, 2)
                };
            }
            if (zeroDiagonal)
                for (int i = 0; i < n; i++)
                    m[i, i] = 0;
            return m;
        }

        /// <summary>
        /// A random partition into k non-empty clusters.
        /// </summary>
        public static int[] RandomPartition(Random rng, int n, int k)
        {
            int[] labels = new int[n];
            int[] order = [.. Enumerable.Range(0, n)];
            rng.Shuffle(order);
            for (int i = 0; i < n; i++)
                labels[order[i]] = i < k ? i : rng.Next(k);
            return labels;
        }

        /// <summary>
        /// A random k x k blockimage with the given candidate blocks (1..maxPerCell blocks per position).
        /// </summary>
        public static string[][] RandomCells(Random rng, int k, string[] blocks, int maxPerCell)
        {
            string[][] cells = new string[k * k][];
            for (int i = 0; i < cells.Length; i++)
            {
                int count = rng.Next(1, maxPerCell + 1);
                cells[i] = [.. blocks.OrderBy(_ => rng.Next()).Take(count)];
            }
            return cells;
        }

        /// <summary>
        /// Creates a blockimage from block strings per position.
        /// </summary>
        public static BlockImage CreateBlockImage(string[][] cells, int k, string name = "bi")
        {
            BlockImage bi = new(name, k);
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                    bi.SetBlock(r, c, string.Join(";", cells[r * k + c]));
            return bi;
        }

        /// <summary>
        /// The path of the example_data folder of the repository.
        /// </summary>
        public static string ExampleDataPath
        {
            get
            {
                string? dir = AppContext.BaseDirectory;
                while (dir != null && !Directory.Exists(Path.Combine(dir, "example_data")))
                    dir = Path.GetDirectoryName(dir);
                return Path.Combine(dir ?? throw new DirectoryNotFoundException("example_data not found"), "example_data");
            }
        }

        /// <summary>
        /// The path of the repository root folder.
        /// </summary>
        public static string RepositoryPath => Path.GetDirectoryName(ExampleDataPath)!;

        /// <summary>
        /// Loads a matrix from the example_data folder.
        /// </summary>
        public static Matrix LoadExample(string fileName)
        {
            Dataset dataset = new();
            List<string> response = [];
            string status = Core.IO.SocnetIO.LoadDataStructure(response, dataset, Path.Combine(ExampleDataPath, fileName), "matrix", "", "\t");
            Assert.Equal("Loading data structure: OK", status);
            return dataset.GetStructuresByType<Matrix>().Single();
        }
    }
}
