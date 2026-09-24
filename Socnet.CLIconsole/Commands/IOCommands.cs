using Socnet.CLIconsole.Runtime;
using Socnet.Core.IO;

namespace Socnet.CLIconsole.Commands
{
    /// <summary>
    /// Commands for loading and saving data structures.
    /// </summary>
    internal static class IOCommands
    {
        public static void Load(CommandContext ctx) => LoadType(ctx, ctx.GetString("type"));

        public static void LoadType(CommandContext ctx, string type)
            => ctx.Add(SocnetIO.LoadDataStructure(ctx.Response, ctx.Dataset, ctx.GetString("file"), type, ctx.GetString("name"), SepOrDefault(ctx)));

        public static void LoadEdgelist(CommandContext ctx)
            => ctx.Add(SocnetIO.LoadEdgelist(ctx.Response, ctx.Dataset,
                ctx.GetString("file"),
                ctx.GetInt("col1"),
                ctx.GetInt("col2"),
                ctx.GetString("symmetric"),
                ctx.GetString("actorset"),
                ctx.GetInt("colval"),
                ctx.GetString("headers"),
                ctx.GetString("sep")));

        public static void Save(CommandContext ctx)
            => ctx.Add(SocnetIO.SaveDataStructure(ctx.Dataset, ctx.GetString("name"), ctx.GetString("file")));

        private static string SepOrDefault(CommandContext ctx)
        {
            string sep = ctx.GetString("sep");
            return sep.Length > 0 ? sep : "\t";
        }
    }
}
