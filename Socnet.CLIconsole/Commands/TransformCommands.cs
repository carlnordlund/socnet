using Socnet.CLIconsole.Runtime;
using Socnet.Core.Model;
using Socnet.Core.Processing;
using Socnet.Core.Utilities;

namespace Socnet.CLIconsole.Commands
{
    /// <summary>
    /// Commands for transforming data structures, and computing block densities.
    /// </summary>
    internal static class TransformCommands
    {
        public static DataStructure? Dichotomize(CommandContext ctx)
        {
            DataStructure? structure = ctx.Dataset.GetStructureByName(ctx.GetString("name"));
            if (structure == null)
            {
                ctx.Add("!Error: Structure not found");
                return null;
            }
            if (structure is not (Matrix or Table or Vector))
            {
                ctx.Add("!Error: Can only dichotomize Matrix, Table and Vector structures");
                return null;
            }
            double threshold = ctx.GetDouble("threshold");
            if (double.IsNaN(threshold))
            {
                ctx.Add("!Error: Couldn't parse 'threshold' argument");
                return null;
            }
            string condition = ctx.GetString("condition").ToLower();
            if (!MatrixFunctions.ConditionAbbrs.Contains(condition))
            {
                ctx.Add($"!Error: Condition '{condition}' not available. Options: ge,gt,le,lt,eq,ne");
                return null;
            }
            string truevalstr = ctx.GetString("truevalue"), falsevalstr = ctx.GetString("falsevalue");
            double truevalue = truevalstr == "" ? 1 : truevalstr == "keep" ? double.NaN : ctx.GetDouble("truevalue");
            double falsevalue = falsevalstr == "" ? 0 : falsevalstr == "keep" ? double.NaN : ctx.GetDouble("falsevalue");
            return MatrixFunctions.Dichotomize(structure, condition, threshold, truevalue, falsevalue);
        }

        public static DataStructure? Symmetrize(CommandContext ctx)
        {
            if (ctx.Dataset.GetStructureByName(ctx.GetString("name")) is not Matrix matrix)
            {
                ctx.Add("!Error: Can only symmetrize Matrix structures");
                return null;
            }
            string method = ctx.GetString("method").ToLower();
            if (!MatrixFunctions.SymmMethods.Contains(method))
            {
                ctx.Add($"!Error: Symmetrization method '{method}' not known");
                return null;
            }
            return MatrixFunctions.Symmetrize(matrix, method);
        }

        public static DataStructure? Rescale(CommandContext ctx)
        {
            DataStructure? structure = ctx.Dataset.GetStructureByName(ctx.GetString("name"));
            if (structure == null)
            {
                ctx.Add("!Error: Structure not found");
                return null;
            }
            if (structure is not Matrix matrix)
            {
                ctx.Add("!Error: Can only rescale Matrix objects at the moment");
                return null;
            }
            double min = ctx.GetDouble("min"), max = ctx.GetDouble("max");
            ctx.Add($"Min value: {Fmt.D(min)}, max:{Fmt.D(max)}");
            min = double.IsNaN(min) ? 0 : min;
            max = double.IsNaN(max) ? 1 : max;
            if (min >= max)
            {
                ctx.Add($"!Error: 'max' ({Fmt.D(max)}) must be larger than 'min ({Fmt.D(min)})");
                return null;
            }
            return MatrixFunctions.Rescale(matrix, min, max, ctx.GetYes("incldiag"));
        }

        public static void Densities(CommandContext ctx)
        {
            Matrix? matrix = ctx.Dataset.Get<Matrix>(ctx.GetString("network"));
            if (matrix == null)
            {
                ctx.Add("!Error: Network not found");
                return;
            }
            Partition? partition = ctx.Dataset.Get<Partition>(ctx.GetString("partition"));
            if (partition == null)
            {
                ctx.Add("!Error: Partition not found");
                return;
            }
            if (partition.Actorset != matrix.Actorset)
            {
                ctx.Add("!Error: Partition and Matrix have different actorsets");
                return;
            }
            Actorset? densitiesActorset = ctx.Dataset.GetActorsetByLabels(partition.ClusterNames);
            if (densitiesActorset == null)
            {
                densitiesActorset = ctx.Dataset.CreateActorsetByLabels(partition.ClusterNames);
                if (densitiesActorset == null)
                {
                    ctx.Add("!Error: Could not create actorset for density matrix");
                    return;
                }
                densitiesActorset.Name = matrix.Name + "_densities_actorset";
                ctx.Add(ctx.Dataset.StoreStructure(densitiesActorset));
            }
            double[,] values = MatrixFunctions.Densities(matrix, partition);
            Matrix densities = new(densitiesActorset, matrix.Name + "_densities");
            for (int r = 0; r < partition.NbrClusters; r++)
                for (int c = 0; c < partition.NbrClusters; c++)
                {
                    // The densities actorset may list the cluster names in another order
                    densitiesActorset.TryGetIndex(partition.ClusterNames[r], out int dr);
                    densitiesActorset.TryGetIndex(partition.ClusterNames[c], out int dc);
                    densities[dr, dc] = values[r, c];
                }
            ctx.Add(ctx.Dataset.StoreStructure(densities));
        }
    }
}
