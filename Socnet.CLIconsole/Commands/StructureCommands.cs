using Socnet.CLIconsole.Runtime;
using Socnet.Core.Blockmodeling;
using Socnet.Core.Model;
using Socnet.Core.Utilities;

namespace Socnet.CLIconsole.Commands
{
    /// <summary>
    /// Commands for creating, viewing, modifying and deleting data structures.
    /// </summary>
    internal static class StructureCommands
    {
        public static void Structures(CommandContext ctx)
        {
            ctx.Add("Name\tType\tSize");
            ctx.Add("========\t====\t====");
            string type = ctx.GetString("type");
            foreach (DataStructure structure in ctx.Dataset.Structures)
                if (type == "" || structure.DataType.Equals(type, StringComparison.CurrentCultureIgnoreCase))
                    ctx.Add($":{structure.Name}\t{structure.DataType}\t{structure.Size}");
        }

        public static void View(CommandContext ctx)
        {
            string name = ctx.GetString("name");
            DataStructure? structure = ctx.Dataset.GetStructureByName(name);
            if (structure == null)
                ctx.Add($"!Error: Structure '{name}' not found");
            else
                ctx.Response.AddRange(structure.View);
        }

        public static void Delete(CommandContext ctx)
        {
            DataStructure? structure = ctx.Dataset.GetStructureByName(ctx.GetString("name"));
            ctx.Add(structure != null
                ? ctx.Dataset.DeleteStructure(structure)
                : $"!Error: Structure '{ctx.GetString("name")}' not found");
        }

        public static void DeleteAll(CommandContext ctx) => ctx.Add(ctx.Dataset.DeleteAllStructures());

        public static void Rename(CommandContext ctx) => ctx.Add(ctx.Dataset.RenameStructure(ctx.GetString("name"), ctx.GetString("newname")));

        public static void Set(CommandContext ctx)
        {
            string name = ctx.GetString("name");
            DataStructure? structure = ctx.Dataset.GetStructureByName(name);
            if (structure == null)
            {
                ctx.Add($"!Error: Structure '{name}' not found");
                return;
            }
            string rowName = ctx.GetString("row"), colName = ctx.GetString("col");
            switch (structure)
            {
                case Actorset actorset:
                    {
                        if (!actorset.TryGetIndex(rowName, out int index))
                        {
                            ctx.Add($"!Error: Actor '{rowName}' not found in actorset '{actorset.Name}'");
                            return;
                        }
                        string newLabel = ctx.GetString("value");
                        if (!actorset.RenameActor(index, newLabel))
                            ctx.Add($"!Error: Actor label '{newLabel}' either too short or already exists in actorset '{actorset.Name}'");
                        return;
                    }
                case Matrix matrix:
                    {
                        if (!matrix.Actorset.TryGetIndex(rowName, out int from) || !matrix.Actorset.TryGetIndex(colName, out int to))
                        {
                            ctx.Add($"!Error: Actor(s) not found in actorset '{matrix.Actorset.Name}'");
                            return;
                        }
                        double val = ctx.GetDouble("value");
                        if (double.IsNaN(val))
                        {
                            ctx.Add("!Error: 'value' not a number");
                            return;
                        }
                        matrix[from, to] = val;
                        return;
                    }
                case Partition partition:
                    {
                        if (!partition.Actorset.TryGetIndex(rowName, out int actor))
                        {
                            ctx.Add($"!Error: Actor not found in actorset '{partition.Actorset.Name}'");
                            return;
                        }
                        int val = ctx.GetInt("value");
                        if (val < 0 || val >= partition.NbrClusters)
                        {
                            ctx.Add("!Error: 'value' not a valid cluster index");
                            return;
                        }
                        partition.ClusterOf[actor] = val;
                        return;
                    }
                case BlockImage blockimage:
                    if (!blockimage.SetBlock(rowName, colName, ctx.GetString("value")))
                        ctx.Add("!Error: Could not find position name(s) in BlockImage");
                    return;
                default:
                    ctx.Add("!Error: Not implemented for this structure");
                    return;
            }
        }

        public static DataStructure? Actorset(CommandContext ctx)
        {
            int nbrActors = ctx.GetInt("size");
            if (nbrActors < 1)
            {
                ctx.Add("!Error: Actorset must contain at least 1 actor");
                return null;
            }
            string labelstring = ctx.GetString("labelarray");
            if (labelstring.Length > 0)
            {
                string[] labels = labelstring.Split(";");
                if (labels.Length != nbrActors)
                {
                    ctx.Add($"!Error: Length of provided labelarray ({labels.Length}) not same length as specified Actorset size({nbrActors})");
                    return null;
                }
                if (labels.Any(l => l.Length == 0))
                {
                    ctx.Add("!Error: Actor labels must consist of at least one character");
                    return null;
                }
                Actorset? actorset = ctx.Dataset.CreateActorsetByLabels(labels);
                if (actorset == null)
                    ctx.Add("!Error: At least two actors seem to have the same name in the 'labelarray'");
                return actorset;
            }
            string[] defaultLabels = new string[nbrActors];
            for (int i = 0; i < nbrActors; i++)
                defaultLabels[i] = "actor" + i;
            return ctx.Dataset.CreateActorsetByLabels(defaultLabels);
        }

        public static DataStructure? Matrix(CommandContext ctx)
        {
            string actorsetName = ctx.GetString("actorset");
            if (actorsetName.Length == 0)
            {
                ctx.Add("!Error: No 'actorset' specified");
                return null;
            }
            Actorset? actorset = ctx.Dataset.Get<Actorset>(actorsetName);
            if (actorset == null)
            {
                ctx.Add($"!Error: Actorset '{actorsetName}' not found");
                return null;
            }
            Matrix matrix = new(actorset, "");
            string dataarray = ctx.GetString("data");
            if (dataarray.Length > 0)
            {
                string[] cells = dataarray.Split(";");
                if (cells.Length != actorset.Count * actorset.Count)
                {
                    ctx.Add($"!Error: Size of data array ({cells.Length}) differs from size of Matrix ({actorset.Count * actorset.Count})");
                    return null;
                }
                for (int i = 0; i < cells.Length; i++)
                    if (Fmt.TryParseDouble(cells[i], out double value))
                        matrix.Data[i] = value;
            }
            return matrix;
        }

        public static DataStructure? BlockImage(CommandContext ctx)
        {
            int nbrPositions = ctx.GetInt("size");
            if (nbrPositions < 2)
            {
                ctx.Add("!Error: Blockimage size must be at least 2");
                return null;
            }
            BlockImage bi = new("", nbrPositions);
            string type = ctx.GetString("type"), pattern = ctx.GetString("pattern"), content = ctx.GetString("content");
            if (type != "")
            {
                if (type == "structural")
                    bi.SetBlocksByPattern("nul;com");
                else if (type == "regular")
                    bi.SetBlocksByPattern("nul;reg");
                else
                {
                    ctx.Add("!Error: Type must be 'structural' or 'regular'");
                    return null;
                }
            }
            else if (pattern != "")
                bi.SetBlocksByPattern(pattern);
            else if (content != "")
            {
                string[] contentParts = content.Split('|');
                if (contentParts.Length != nbrPositions * nbrPositions)
                {
                    ctx.Add("!Error: Size mismatch between content and blockimage size");
                    return null;
                }
                bi.SetBlocksByContent(contentParts);
            }
            return bi;
        }

        public static DataStructure? Partition(CommandContext ctx)
        {
            string actorsetName = ctx.GetString("actorset");
            if (actorsetName.Length == 0)
            {
                ctx.Add("!Error: No 'actorset' specified");
                return null;
            }
            Actorset? actorset = ctx.Dataset.Get<Actorset>(actorsetName);
            if (actorset == null)
            {
                ctx.Add($"!Error: Actorset '{actorsetName}' not found");
                return null;
            }
            int nbrClusters = ctx.GetInt("nbrclusters");
            if (nbrClusters < 1)
            {
                ctx.Add("!Error: Number of clusters must be at least 1");
                return null;
            }
            int[] partArray = new int[actorset.Count];
            string partstring = ctx.GetString("partarray");
            if (partstring.Length > 0)
            {
                string[] cells = partstring.Split(";");
                if (cells.Length != actorset.Count)
                {
                    ctx.Add($"!Error: Length of provided partarray ({cells.Length}) not same length as Actorset ({actorset.Count})");
                    return null;
                }
                for (int i = 0; i < cells.Length; i++)
                {
                    if (!Fmt.TryParseInt(cells[i], out partArray[i]))
                    {
                        ctx.Add($"!Error: Couldn't convert '{cells[i]}' to an integer");
                        return null;
                    }
                    if (partArray[i] < 0 || partArray[i] >= nbrClusters)
                    {
                        ctx.Add($"!Error: Partition index '{cells[i]}' out of bounds");
                        return null;
                    }
                }
            }
            return new Partition(actorset, "", Core.Model.Partition.DefaultClusterNames(nbrClusters), partArray);
        }

        public static DataStructure? BiExtend(CommandContext ctx)
        {
            BlockImage? bi = ctx.Dataset.Get<BlockImage>(ctx.GetString("blockimage"));
            if (bi == null)
            {
                ctx.Add("!Error: 'blockimage' not set/recognized");
                return null;
            }
            return bi.Extend(ctx.GetString("pattern"));
        }

        public static void BiVarieties(CommandContext ctx)
        {
            ctx.Add("*** TEST FUNCTION ***");
            ctx.Add("Create varieties from multiblocked blockimage");
            BlockImage? bi = ctx.Dataset.Get<BlockImage>(ctx.GetString("blockimage"));
            if (bi == null)
            {
                ctx.Add("!Error: 'blockimage' not set/recognized");
                return;
            }
            if (!bi.MultiBlocked)
            {
                ctx.Add("!Error: Blockimage not multiblocked");
                return;
            }
            foreach (BlockImage variety in BlockImageVarieties.Generate(bi))
                ctx.Dataset.StoreStructure(variety);
        }
    }
}
