using Socnet.CLIconsole.Runtime;

namespace Socnet.Tests
{
    /// <summary>
    /// Tests of console commands that are not covered by the compatibility test.
    /// </summary>
    [Collection("WorkingDirectory")]
    public class CommandTests
    {
        private static List<string> Run(SocnetEngine engine, string command) => [.. engine.ExecuteCommand(command, true)];

        [Fact]
        public void Threads_LimitsCoresAndDoesNotChangeResults()
        {
            string results(string threadsArg)
            {
                SocnetEngine engine = new();
                Run(engine, "randomseed(5)");
                Run(engine, $"loadmatrix(file = {Path.Combine(TestData.ExampleDataPath, "little_league_ti.txt")}, name = llti)");
                Run(engine, "bi = blockimage(size = 3, type = structural)");
                List<string> init = Run(engine, $"bminit(llti, bi, ljubljana, hamming{threadsArg})");
                Assert.Contains("Initialization seems to have gone ok!", init);
                if (threadsArg.Length > 0)
                    Assert.Contains(init, l => l.StartsWith("threads: 1 (of "));
                else
                    Assert.DoesNotContain(init, l => l.StartsWith("threads:"));
                List<string> start = Run(engine, "bmstart()");
                return string.Join("\n", start.Where(l => !l.StartsWith("Execution time")));
            }
            Assert.Equal(results(""), results(", threads = 1"));
        }
    }
}
