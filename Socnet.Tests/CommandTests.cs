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
        [InlineData("core\u001b[Dperi(bb, ljubljana)", "!Error: Syntax error - the command contains the invisible control character U+001B (e.g. from editing keys): 'core\\u001B[Dperi(bb, ljubljana)'")]
        [InlineData("core-peri(bb, ljubljana)", "!Error: Syntax error - expected '[name =] command(arguments)', got: 'core-peri(bb, ljubljana)'")]
        public void Parser_ExplainsSyntaxErrors(string command, string expected)
        {
            SocnetEngine engine = new();
            Assert.Contains(expected, Run(engine, command));
        }
    
        [Theory]
        [InlineData("coreperi(baker_binary, ljubljana, intercat = denuci(0.5))", "", "coreperi", "baker_binary, ljubljana, intercat = denuci(0.5)")]
        [InlineData("cpx = blockimage(size = 2, content = reg;pco(0.5)|cre|rre|nul)", "cpx", "blockimage", "size = 2, content = reg;pco(0.5)|cre|rre|nul")]
        [InlineData("bi3 = blockimage (3, content = den(0.1)|den(0.2)|(x)|a|b|c|d|e|f)", "bi3", "blockimage", "3, content = den(0.1)|den(0.2)|(x)|a|b|c|d|e|f")]
        [InlineData("x=help", "x", "help", "")]
        [InlineData("bmview", "", "bmview", "")]
        [InlineData("bmstart()", "", "bmstart", "")]
        public void Parser_SplitsCommands(string command, string assigner, string function, string args)
        {
            Assert.True(SocnetEngine.TryParseCommand(command, out string a, out string f, out string g));
            Assert.Equal((assigner, function, args), (a, f, g));
        }

        [Theory]
        [InlineData("a = b = c(1)")]
        [InlineData("core peri(1)")]
        [InlineData("f(1) x")]
        [InlineData("= f(1)")]
        public void Parser_RejectsInvalidCommands(string command)
        {
            Assert.False(SocnetEngine.TryParseCommand(command, out _, out _, out _));
        }
    
        [Fact]
        public void System_ReportsCoresAndVersion()
        {
            List<string> response = Run(new SocnetEngine(), "system");
            Assert.Contains($":Socnet.se: {SocnetEngine.VersionString}", response);
            Assert.Contains($":Processor cores (logical): {Environment.ProcessorCount}", response);
            Assert.All(response, line => Assert.StartsWith(":", line));
        }
    }
}
