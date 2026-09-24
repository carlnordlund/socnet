using Socnet.CLIconsole.Runtime;
using Socnet.Core.Model;

namespace Socnet.CLIconsole.Commands
{
    /// <summary>
    /// A console command.
    /// </summary>
    /// <param name="Name">The command name.</param>
    /// <param name="RequiredArgs">The required arguments, in the order they can be given positionally.</param>
    /// <param name="Handler">The implementation: returns a data structure (to be stored or displayed) or null.</param>
    public sealed record Command(string Name, string[] RequiredArgs, Func<CommandContext, DataStructure?> Handler)
    {
        public DataStructure? Execute(CommandContext ctx) => Handler(ctx);
    }

    /// <summary>
    /// All available console commands.
    /// </summary>
    public static class CommandRegistry
    {
        private static readonly Dictionary<string, Command> Commands = [];

        static CommandRegistry()
        {
            // General
            Add("help", [], GeneralCommands.Help);
            Add("citeinfo", [], GeneralCommands.CiteInfo);
            Add("randomseed", ["seed"], GeneralCommands.RandomSeed);
            Add("system", [], GeneralCommands.SystemInfo);
            Add("getwd", [], GeneralCommands.GetWd);
            Add("setwd", ["dir"], GeneralCommands.SetWd);
            Add("dir", [], GeneralCommands.Dir);
            Add("loadscript", ["file"], GeneralCommands.LoadScript);

            // Loading and saving
            Add("load", ["file", "type"], IOCommands.Load);
            Add("loadactorset", ["file"], ctx => IOCommands.LoadType(ctx, "actorset"));
            Add("loadmatrix", ["file"], ctx => IOCommands.LoadType(ctx, "matrix"));
            Add("loadblockimage", ["file"], ctx => IOCommands.LoadType(ctx, "blockimage"));
            Add("loadpartition", ["file"], ctx => IOCommands.LoadType(ctx, "partition"));
            Add("loadedgelist", ["file", "col1", "col2"], IOCommands.LoadEdgelist);
            Add("save", ["name", "file"], IOCommands.Save);

            // Managing structures
            Add("structures", [], StructureCommands.Structures);
            Add("view", ["name"], StructureCommands.View);
            Add("delete", ["name"], StructureCommands.Delete);
            Add("deleteall", [], StructureCommands.DeleteAll);
            Add("rename", ["name", "newname"], StructureCommands.Rename);
            Add("set", ["name", "value", "row"], StructureCommands.Set);
            Add("actorset", ["size"], StructureCommands.Actorset);
            Add("matrix", ["actorset"], StructureCommands.Matrix);
            Add("blockimage", ["size"], StructureCommands.BlockImage);
            Add("partition", ["actorset", "nbrclusters"], StructureCommands.Partition);
            Add("biextend", ["blockimage"], StructureCommands.BiExtend);
            Add("bivarieties", ["blockimage"], StructureCommands.BiVarieties);

            // Transformations
            Add("dichotomize", ["name", "condition", "threshold"], TransformCommands.Dichotomize);
            Add("symmetrize", ["name", "method"], TransformCommands.Symmetrize);
            Add("rescale", ["name"], TransformCommands.Rescale);
            Add("densities", ["network", "partition"], TransformCommands.Densities);

            // Blockmodeling
            Add("bminit", ["network", "blockimage", "searchtype", "method"], BlockmodelCommands.BmInit);
            Add("bmstart", [], BlockmodelCommands.BmStart);
            Add("coreperi", ["network", "searchtype"], BlockmodelCommands.CorePeri);
            Add("bmtest", ["network", "blockimage", "partition", "method"], BlockmodelCommands.BmTest);
            Add("bmview", ["blockmodel"], BlockmodelCommands.BmView);
            Add("bmextract", ["blockmodel", "type"], BlockmodelCommands.BmExtract);
        }

        private static void Add(string name, string[] requiredArgs, Func<CommandContext, DataStructure?> handler)
            => Commands[name] = new Command(name, requiredArgs, handler);

        private static void Add(string name, string[] requiredArgs, Action<CommandContext> handler)
            => Commands[name] = new Command(name, requiredArgs, ctx => { handler(ctx); return null; });

        /// <summary>
        /// Looks up a command by name.
        /// </summary>
        public static bool TryGet(string name, out Command? command) => Commands.TryGetValue(name, out command);

        /// <summary>
        /// The names of all commands.
        /// </summary>
        public static IEnumerable<string> Names => Commands.Keys;
    }
}
