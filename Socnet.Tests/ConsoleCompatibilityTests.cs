using Socnet.CLIconsole.Runtime;

namespace Socnet.Tests
{
    [CollectionDefinition("WorkingDirectory", DisableParallelization = true)]
    public class WorkingDirectoryCollection { }

    /// <summary>
    /// Runs a script with all deterministic (non-search) commands and compares the console output with the
    /// output of Socnet.se 1.4 for the same script (Golden/deterministic_script.v14.out).
    /// </summary>
    [Collection("WorkingDirectory")]
    public class ConsoleCompatibilityTests
    {
        /// <summary>
        /// Lines where 2.0 deliberately differs from 1.4, as (preceding line or null for any, 1.4 line, 2.0 line):
        /// - the version string;
        /// - the header line of a Table view, which erroneously started with '_' in 1.4;
        /// - bmextract(type=partition) of a bmtest blockmodel, whose partition is a copy in 2.0.
        /// A partition modified with set() also lists its actors in index order in 2.0 (handled separately).
        /// </summary>
        private static readonly (string? previous, string v14, string v20)[] KnownDifferences =
        [
            (null, "Version 1.4 (October 2025)", SocnetEngine.VersionString),
            (null, "_\tRon__1\tTom__2\tFrank_3\tBoyd_4\tTim__5\tJohn_6\tJeff_7\tJay__8\tSandy_9\tJerry_10\tDarrin_11\tBen_12\tArnie_13",
             "\tRon__1\tTom__2\tFrank_3\tBoyd_4\tTim__5\tJohn_6\tJeff_7\tJay__8\tSandy_9\tJerry_10\tDarrin_11\tBen_12\tArnie_13"),
            ("> bmextract(blockmodel = bm1, type = partition)", "Stored structure 'part1' (Partition)", "Updated structure 'part1' (Partition)"),
        ];

        [Fact]
        public void DeterministicScript_OutputIdenticalToVersion14()
        {
            string golden = Path.Combine(TestData.RepositoryPath, "Socnet.Tests", "Golden");
            string workDir = Path.Combine(Path.GetTempPath(), "socnet_golden_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workDir);
            foreach (string file in Directory.GetFiles(TestData.ExampleDataPath, "*.txt"))
                File.Copy(file, Path.Combine(workDir, Path.GetFileName(file)));
            File.Copy(Path.Combine(golden, "deterministic_script.txt"), Path.Combine(workDir, "deterministic_script.txt"));

            string previousDir = Directory.GetCurrentDirectory();
            List<string> actual;
            try
            {
                Directory.SetCurrentDirectory(workDir);
                SocnetEngine engine = new();
                actual = Render(engine.ExecuteCommand("loadscript(file = deterministic_script.txt)", true));
            }
            finally
            {
                Directory.SetCurrentDirectory(previousDir);
                Directory.Delete(workDir, true);
            }

            // The 1.4 transcript: skip the startup banner (12 lines) and the final 'Exiting...' line,
            // and remove the prompt in front of the first line
            string[] transcript = File.ReadAllLines(Path.Combine(golden, "deterministic_script.v14.out"));
            List<string> expected = [.. transcript.Skip(12).Take(transcript.Length - 13)];
            expected[0] = expected[0][2..];
            for (int i = 0; i < expected.Count; i++)
                foreach (var (previous, v14, v20) in KnownDifferences)
                    if (expected[i] == v14 && (previous == null || (i > 0 && expected[i - 1] == previous)))
                        expected[i] = v20;

            Assert.Equal(expected.Count, actual.Count);
            for (int i = 0; i < expected.Count; i++)
            {
                // After set(name = p, row = Tom_2, value = 0), 1.4 lists Tom_2 last in cluster 0, 2.0 in index order
                if (expected[i] == "Boyd_4\t(3)" && actual[i] == "Tom_2\t(1)" || expected[i] == "Tom_2\t(1)" && actual[i] == "Boyd_4\t(3)")
                    continue;
                Assert.True(expected[i] == actual[i], $"Line {i + 13}: expected '{expected[i]}' but got '{actual[i]}'");
            }
        }

        /// <summary>
        /// Renders response lines as the console does in verbose mode.
        /// </summary>
        private static List<string> Render(List<string> response) => [.. response.Select(l => l.StartsWith(':') ? l[1..] : l)];
    }
}
