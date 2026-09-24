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
    
        [Theory]
        [InlineData("coreperi(bb, ljubljana,intercat=denuci(0.5))")]
        [InlineData("coreperi (bb, ljubljana, intercat = denuci(0.5))")]
        [InlineData("coreperi(bb,\u00a0ljubljana,intercat=denuci(0.5))\u00a0")]
        [InlineData("coreperi(bb, ljubljana,intercat=denuci(0.5))\u200b")]
        [InlineData("\ufeffcoreperi(bb, ljubljana,intercat=denuci(0.5))")]
        public void Parser_AcceptsNestedBracketsAndIgnoresInvisibleCharacters(string command)
        {
            SocnetEngine engine = new();
            Run(engine, $"loadmatrix(file = {Path.Combine(TestData.ExampleDataPath, "baker_original.txt")})");
            Run(engine, "bb = dichotomize(name = baker_original, condition = gt, threshold = 0)");
            List<string> response = Run(engine, command);
            Assert.DoesNotContain(response, l => l.StartsWith('!'));
            Assert.Contains(response, l => l.StartsWith("Goodness-of-fit (1st BlockModel): "));
        }

        [Theory]
        [InlineData("coreperi(bb, ljubljana, intercat=denuci(0.5)", "!Error: Syntax error - unbalanced brackets (2 opening, 1 closing)")]
        [InlineData("coreperi(bb, ljubljana) x", "!Error: Syntax error - unexpected text after the closing bracket: ' x'")]
        [InlineData("coreperi\uff08bb, ljubljana\uff09", "!Error: Syntax error - the command contains the unexpected character '\uff08' (U+FF08)")]
        public void Parser_ExplainsSyntaxErrors(string command, string expected)
        {
            SocnetEngine engine = new();
            Assert.Contains(expected, Run(engine, command));
        }
    }
}
