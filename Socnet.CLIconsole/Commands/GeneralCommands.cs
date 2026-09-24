using Socnet.CLIconsole.Runtime;
using Socnet.Core.IO;

namespace Socnet.CLIconsole.Commands
{
    /// <summary>
    /// General commands: help, citation info, random seed, working directory and scripts.
    /// </summary>
    internal static class GeneralCommands
    {
        public static void Help(CommandContext ctx)
        {
            ctx.Add(":HELP SECTION");
            ctx.Add(":============");
            ctx.Add(":" + SocnetEngine.VersionString);
            ctx.Add(":See website for documentation:");
            ctx.Add(":https://socnet.se");
            ctx.Add(": ");
            ctx.Add(":The socnet client is used to do blockmodeling analyses, with some extra functionality for data management.");
            ctx.Add(":You type in commands on the prompt to do various things with data: load, save, transform,  analyze etc.");
            ctx.Add(":Check www.socnet.se for command references, how-to, and quick-start tutorial!");
        }

        public static void CiteInfo(CommandContext ctx)
        {
            ctx.Add(":The Socnet.se software client:");
            ctx.Add(":  Nordlund, C., Roy, C. (2024). Socnet.se: The Blockmodeling Console App [computer software]. https://socnet.se");
            ctx.Add(": ");
            ctx.Add(":Direct blockmodeling using the 'nordlund' method (i.e. weighted correlation coefficients):");
            ctx.Add(":  Nordlund, C. (2020). Direct blockmodeling of valued and binary networks: a dichotomization-free approach. Social Networks, 61, 128-143.");
            ctx.Add(":  https://doi.org/10.1016/j.socnet.2019.10.004");
            ctx.Add(": ");
            ctx.Add(":Correlation-based core-periphery approach, ignoring inter-categorical blocks or using the 'denuci(d)' ideal blocks:");
            ctx.Add(":  Borgatti, S.P., Everett, M.G. (2000). Models of core/periphery structures. Social Networks, 21(4), 375-395.");
            ctx.Add(":  https://doi.org/10.1016/S0378-8733(99)00019-2");
            ctx.Add(": ");
            ctx.Add(":Correlation-based core-periphery approach using 'pco(p)' for intra-core and/or 'den(d)' or 'denmin(d)' for inter-categorical ties:");
            ctx.Add(":  Estévez, J.L., Nordlund, C. (2025). Revising the Borgatti-Everett core-periphery model: Inter-categorical density blocks and partially connected cores. Social Networks, 81, 31-51.");
            ctx.Add(":  https://doi.org/10.1016/j.socnet.2024.11.002");
            ctx.Add(": ");
            ctx.Add(":Power-relational core-periphery approach with core dominance and/or peripheral dependency (i.e. 'powerrelational' argument in 'coreperi'):");
            ctx.Add(":  Nordlund, C. (2018). Power-relational core–periphery structures: Peripheral dependency and core dominance in binary and valued networks. Network Science, 6(3), 348-369.");
            ctx.Add(":  https://doi.org/10.1017/nws.2018.15");
            ctx.Add(": ");
            ctx.Add(":Direct generalized blockmodeling using 'hamming' distances as penalty function:");
            ctx.Add(":  Doreian, P., Batagelj, V., Ferligoj, A. (2004). Generalized Blockmodeling. Cambridge: Cambridge University Press.");
            ctx.Add(":  https://doi.org/10.1017/CBO9780511584176");
        }

        /// <summary>
        /// Command 'system()': information about the computer and the Socnet.se process, e.g. to decide on
        /// the 'threads' argument of bminit/coreperi.
        /// </summary>
        public static void SystemInfo(CommandContext ctx)
        {
            string priority;
            try
            {
                priority = System.Diagnostics.Process.GetCurrentProcess().PriorityClass switch
                {
                    System.Diagnostics.ProcessPriorityClass.BelowNormal => "below normal (start with --normalpriority for normal priority)",
                    System.Diagnostics.ProcessPriorityClass.Normal => "normal",
                    var other => other.ToString()
                };
            }
            catch (Exception)
            {
                priority = "unknown";
            }
            long availableMemory = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
            long usedMemory = Environment.WorkingSet;
            ctx.Add(":Socnet.se: " + SocnetEngine.VersionString);
            ctx.Add(":.NET runtime: " + System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            ctx.Add(":Operating system: " + System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            ctx.Add(":Architecture: " + System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture.ToString().ToLower());
            ctx.Add(":Processor cores (logical): " + Environment.ProcessorCount);
            ctx.Add(":Cores used by searches: all, unless limited with the 'threads' argument of bminit/coreperi");
            ctx.Add(":Process priority: " + priority);
            ctx.Add(":Memory available: " + FormatBytes(availableMemory));
            ctx.Add(":Memory used by Socnet.se: " + FormatBytes(usedMemory));
            ctx.Add(":Working directory: " + Directory.GetCurrentDirectory());
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = ["bytes", "KB", "MB", "GB", "TB"];
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return unit == 0 ? $"{bytes} bytes" : $"{value.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture)} {units[unit]}";
        }

        public static void GetWd(CommandContext ctx) => ctx.Add(":" + Directory.GetCurrentDirectory());

        public static void RandomSeed(CommandContext ctx)
        {
            int seed = ctx.GetInt("seed");
            if (seed < 0)
            {
                ctx.Add("!Error: Seed value 'seed' not set.");
                return;
            }
            ctx.Blockmodeling.SetRandomSeed(seed);
            ctx.Add("New random seed set: " + seed);
        }

        public static void SetWd(CommandContext ctx)
        {
            string dir = ctx.GetString("dir");
            try
            {
                if (dir == "user")
                {
                    if (ctx.UserDirectoryPath.Length > 0)
                        Directory.SetCurrentDirectory(ctx.UserDirectoryPath);
                }
                else
                    Directory.SetCurrentDirectory(dir);
                ctx.Add("Setting working directory: " + Directory.GetCurrentDirectory());
            }
            catch (Exception e)
            {
                ctx.Add("!Error: " + e.Message);
            }
        }

        public static void Dir(CommandContext ctx)
        {
            try
            {
                string cwd = Directory.GetCurrentDirectory();
                foreach (string dir in Directory.GetDirectories(cwd))
                    ctx.Add(":/" + Path.GetFileName(dir) + "/");
                foreach (string file in Directory.GetFiles(cwd))
                    ctx.Add(":" + Path.GetFileName(file));
            }
            catch (Exception e)
            {
                ctx.Add($"!Error: Could not list content of directory '{Directory.GetCurrentDirectory()}'. Make sure that it is not a symbolic link!");
                ctx.Add(e.Message);
            }
        }

        public static void LoadScript(CommandContext ctx)
        {
            string file = ctx.GetString("file");
            string[]? commands = SocnetIO.ReadAllLines(file, ctx.Response);
            if (commands == null)
                return;
            ctx.Add($"Loading and executing '{file}'...");
            foreach (string command in commands)
                if (command.Length > 0 && command[0] != '#')
                {
                    ctx.Add("> " + command);
                    ctx.Engine.ExecuteCommand(command);
                }
        }
    }
}
