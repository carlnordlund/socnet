using Socnet.CLIconsole.Runtime;
using Socnet.Core.Blockmodeling;
using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.CLIconsole.Commands
{
    /// <summary>
    /// Commands for direct blockmodeling: initializing and running searches, testing hypothetical
    /// blockmodels, and viewing and extracting blockmodels.
    /// </summary>
    internal static class BlockmodelCommands
    {
        public static void BmInit(CommandContext ctx)
        {
            Matrix? network = ctx.Dataset.Get<Matrix>(ctx.GetString("network"));
            if (network == null)
            {
                ctx.Add("!Error: Network not found (parameter: network)");
                return;
            }
            BlockImage? blockimage = ctx.Dataset.Get<BlockImage>(ctx.GetString("blockimage"));
            if (blockimage == null)
            {
                ctx.Add("!Error: Blockimage not found (parameter: blockimage)");
                return;
            }
            if (!blockimage.HasBlocks())
            {
                ctx.Add($"!Error: Blockimage '{blockimage.Name}' has unspecified blocks");
                return;
            }
            string searchType = ctx.GetString("searchtype");
            if (!BlockmodelingSession.SearchTypes.Contains(searchType))
            {
                ctx.Add("!Error: Search type not recognized/set (parameter: searchtype");
                return;
            }
            string method = ctx.GetString("method");
            if (!BlockmodelingSession.GofMethods.Contains(method))
            {
                ctx.Add("!Error: Method not recognized/set (parameter: method)");
                return;
            }
            Initialize(ctx, network, blockimage, searchType, method);
        }

        private static bool Initialize(CommandContext ctx, Matrix network, BlockImage blockimage, string searchType, string method)
        {
            List<string> log = [];
            string status = ctx.Blockmodeling.Initialize(network, blockimage, searchType, method, ctx, log);
            if (status == "ok")
            {
                ctx.Response.AddRange(log);
                return true;
            }
            ctx.Add(status);
            return false;
        }

        public static void BmStart(CommandContext ctx) => ctx.Blockmodeling.Start(ctx);

        public static void CorePeri(CommandContext ctx)
        {
            Matrix? network = ctx.Dataset.Get<Matrix>(ctx.GetString("network"));
            if (network == null)
            {
                ctx.Add("!Error: Network not found (parameter: network)");
                return;
            }
            string searchType = ctx.GetString("searchtype");
            if (!BlockmodelingSession.SearchTypes.Contains(searchType))
            {
                ctx.Add($"!Error: Search type '{searchType}' not recognized (check 'searchtype' parameter)");
                return;
            }

            BlockImage cpbi = new("cp", 2);
            cpbi.PositionNames[0] = "C";
            cpbi.PositionNames[1] = "P";
            cpbi.SetBlock(1, 1, "nul");
            string core = ctx.GetString("core");
            cpbi.SetBlock(0, 0, core.StartsWith("pco") ? core : "com");

            string intercat = ctx.GetString("intercat");
            string powerrelational = ctx.GetString("powerrelational");
            if (powerrelational != "")
            {
                switch (powerrelational)
                {
                    case "dep":
                        cpbi.SetBlock(1, 0, "rfn");
                        cpbi.SetBlock(0, 1, "cfn");
                        break;
                    case "dom":
                        cpbi.SetBlock(1, 0, "cre");
                        cpbi.SetBlock(0, 1, "rre");
                        break;
                    case "depdom":
                        cpbi.SetBlock(1, 0, "pcdd");
                        cpbi.SetBlock(0, 1, "cpdd");
                        break;
                    default:
                        ctx.Add($"!Error: Power-relational pattern '{powerrelational}' not recognized; use 'dep', 'dom' or 'depdom'");
                        return;
                }
                cpbi.Name = "cp" + powerrelational;
            }
            else if (intercat != "")
            {
                cpbi.SetBlock(1, 0, intercat);
                cpbi.SetBlock(0, 1, intercat);
            }
            else
            {
                string ptoc = ctx.GetString("ptoc"), ctop = ctx.GetString("ctop");
                cpbi.SetBlock(1, 0, ptoc != "" ? ptoc : "dnc");
                cpbi.SetBlock(0, 1, ctop != "" ? ctop : "dnc");
            }

            if (!cpbi.HasBlocks())
            {
                ctx.Add("!Error: Something wrong with inter-categorical blocks");
                return;
            }
            if (Initialize(ctx, network, cpbi, searchType, "nordlund"))
                ctx.Blockmodeling.Start(ctx);
        }

        public static DataStructure? BmTest(CommandContext ctx)
        {
            Matrix? network = ctx.Dataset.Get<Matrix>(ctx.GetString("network"));
            if (network == null)
            {
                ctx.Add($"!Error: Network '{ctx.GetString("network")}' not found (parameter: network)");
                return null;
            }
            BlockImage? blockimage = ctx.Dataset.Get<BlockImage>(ctx.GetString("blockimage"));
            if (blockimage == null)
            {
                ctx.Add($"!Error: Blockimage '{ctx.GetString("blockimage")}'not found (parameter: blockimage)");
                return null;
            }
            Partition? partition = ctx.Dataset.Get<Partition>(ctx.GetString("partition"));
            if (partition == null)
            {
                ctx.Add($"!Error: Partition '{ctx.GetString("partition")}' not found (parameter: partition)");
                return null;
            }
            string methodName = ctx.GetString("method");
            if (!BlockmodelingSession.GofMethods.Contains(methodName))
            {
                ctx.Add($"!Error: Method '{methodName}' not recognized/set (parameter: method)");
                return null;
            }
            GofMethod method = methodName == "hamming" ? GofMethod.Hamming : GofMethod.Nordlund;
            if (method == GofMethod.Nordlund && blockimage.MultiBlocked)
            {
                ctx.Add("!Error: Can't do 'bmtest()' with method 'nordlund' and multiblocked 'blockimage'");
                return null;
            }
            if (!blockimage.HasBlocks())
            {
                ctx.Add($"!Error: Blockimage '{blockimage.Name}' has unspecified blocks");
                return null;
            }
            foreach (string blockName in blockimage.GetAllUniqueBlockNames())
                if (BlockFactory.Create(blockName) is IdealBlock block && !block.Supports(method))
                {
                    ctx.Add($"!Error - Block '{blockName}' can't be used in method '{methodName}' criteria function");
                    return null;
                }
            if (partition.Actorset != network.Actorset)
            {
                ctx.Add("!Error: Partition and network have different actorsets");
                return null;
            }
            if (partition.NbrClusters != blockimage.NbrPositions)
            {
                ctx.Add($"!Error: Partition has {partition.NbrClusters} clusters but blockimage has {blockimage.NbrPositions} positions");
                return null;
            }
            string name = $"bm_{network.Name}_{blockimage.Name}_{partition.Name}";
            return BlockmodelEvaluator.CreateBlockModel(name, network, blockimage, partition.Clone(partition.Name), method);
        }

        public static void BmView(CommandContext ctx)
        {
            BlockModel? bm;
            string name = ctx.GetString("blockmodel");
            if (name.Length == 0)
            {
                List<BlockModel> blockmodels = ctx.Dataset.GetStructuresByType<BlockModel>();
                if (blockmodels.Count != 1)
                {
                    ctx.Add(blockmodels.Count == 0
                        ? "!Error: No BlockModel objects found"
                        : "!Error: Found several BlockModel objects - specify which with 'blockmodel' parameter");
                    return;
                }
                bm = blockmodels[0];
            }
            else
            {
                bm = ctx.Dataset.Get<BlockModel>(name);
                if (bm == null)
                {
                    ctx.Add($"!Error: Blockmodel '{name}' not found (parameter: blockmodel)");
                    return;
                }
            }
            ctx.Add(":Blockmodel:");
            ctx.Response.AddRange(bm.DisplayBlockmodelMatrix());
            if (ctx.GetYes("ideal"))
            {
                ctx.Add(":Ideal blockmodel:");
                ctx.Response.AddRange(bm.DisplayIdealMatrix());
            }
            ctx.Add(":Blockimage:");
            ctx.Response.AddRange(bm.DisplayBlockimage());
            ctx.Add(":Goodness-of-fit: " + bm.GofString);
        }

        public static void BmExtract(CommandContext ctx)
        {
            string outname = ctx.GetString("outname");
            bool autoname = outname.Length == 0;
            BlockModel? blockmodel = ctx.Dataset.Get<BlockModel>(ctx.GetString("blockmodel"));
            if (blockmodel == null)
            {
                ctx.Add("!Error: Blockmodel not found");
                return;
            }
            switch (ctx.GetString("type"))
            {
                case "blockimage":
                    BlockImage bi = blockmodel.BlockImage.Clone(autoname ? ctx.Dataset.GetAutoName(blockmodel.BlockImage.Name) : outname);
                    ctx.Add(ctx.Dataset.StoreStructure(bi));
                    break;
                case "matrix":
                    Matrix bmMatrix = blockmodel.BmMatrix, bmIdealMatrix = blockmodel.BmIdealMatrix;
                    if (!autoname)
                    {
                        bmMatrix.Name = outname;
                        bmMatrix.Actorset.Name = outname + "_actors";
                        bmIdealMatrix.Name = outname + "_ideal";
                    }
                    ctx.Add(ctx.Dataset.StoreStructure(bmMatrix.Actorset));
                    ctx.Add(ctx.Dataset.StoreStructure(bmMatrix));
                    ctx.Add(ctx.Dataset.StoreStructure(bmIdealMatrix));
                    break;
                case "partition":
                    Partition partition = blockmodel.Partition;
                    if (!autoname)
                        partition.Name = outname;
                    ctx.Add(ctx.Dataset.StoreStructure(partition));
                    break;
                case "gof":
                    ctx.Add(":" + blockmodel.GofString);
                    break;
            }
        }
    }
}
