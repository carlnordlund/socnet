using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Socnet.DataLibrary;

namespace Socnet
{
    public static class Blockmodeling
    {
        public static Dictionary<string, List<string>> availableBlocks = new Dictionary<string, List<string>>()
        {
            { "hamming", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn" } },
            { "nordlund", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn" } },
            { "ziberna", new List<string>() { "dnc","nul","com","maxreg","meanreg","sumreg" } }
        };

        public static List<string> searchTypes = new List<string>() { "localopt", "exhaustive" };
        public static List<string> gofMethods = new List<string>() { "hamming", "nordlund", "ziberna" };

        public static List<BMSolution> optimalSolutionsGlobal = new List<BMSolution>();
        public static HashSet<string> checkedPartString = new HashSet<string>();

        public delegate void SearchHeuristic();
        public static SearchHeuristic? searchHeuristic;
        public static string searchTypeName = "";

        public delegate BMSolution GofMethod(Matrix matrix, BlockImage blockimage, Partition partition);
        public static GofMethod? gofMethod;
        public static bool maximizeGof = false;
        public static string gofMethodName = "";

        public static int minClusterSize = 1;

        public static bool initializationOk = false;


        public static List<string> logLines = new List<string>();

        public static Matrix? matrix = null;
        public static List<BlockImage> blockimages = new List<BlockImage>();

        internal static string InitializeSearch(Dictionary<string, object?> searchParams)
        {
            logLines.Clear();
            initializationOk = false;
            try
            {
                log("Initializing direct blockmodeling search...");
                // Set network to search
                matrix = searchParams["network"] as Matrix;
                if (matrix == null)
                    return "!Error - 'network' is null";
                log("Network: " + matrix.Name);

                gofMethodName = "" + searchParams["method"] as string;
                if (gofMethodName.Equals("hamming"))
                {
                    gofMethod = binaryHamming;
                    maximizeGof = false;

                }
                else if (gofMethodName.Equals("ziberna"))
                {
                    gofMethod = ziberna2007;
                    maximizeGof = false;
                }
                else if (gofMethodName.Equals("nordlund"))
                {
                    gofMethod = nordlund2020;
                    maximizeGof = true;
                }
                else
                    return "!Error - Method '" + gofMethodName + "' not implemented";
                log("Method: " + gofMethodName);

                searchTypeName = "" + searchParams["searchtype"] as string;
                if (searchTypeName.Equals("localopt"))
                    searchHeuristic = doLocalOptSearch;
                else if (searchTypeName.Equals("exhaustive"))
                    searchHeuristic = doExhaustiveSearch;
                else
                {
                    return "!Error - Search heuristic '" + searchTypeName + "' not implemented";
                }
                log("Search heuristic: " + searchTypeName);

                // Set blockimage(s)
                BlockImage? bi = searchParams["blockimage"] as BlockImage;
                if (bi == null)
                    return "!Error - 'blockimage' is null";

                List<string> uniqueBlockNames = bi.GetAllUniqueBlockNames();
                foreach (string blockname in uniqueBlockNames)
                {
                    if (!availableBlocks[gofMethodName].Contains(blockname))
                    {
                        return "!Error - Block '" + blockname + "' can't be used in method '" + gofMethodName + "' criteria function";
                    }
                }

                // Remove existing blockimages
                blockimages.Clear();
                if (bi.multiBlocked && gofMethod == nordlund2020)
                {
                    // Multiblocked for Nordlund2020: need to unwrap this and create non-multiblocked blockimages and place into blockimages
                    blockimages.AddRange(Functions.GetBlockImageVarieties(bi));
                    log("Generating from multi-blocked blockimage: " + bi.Name + " (nbr varieties: " + blockimages.Count + ")");
                }
                else
                {
                    // Ok: either not nordlund2020 or not multiblocked: just add this individual blockimage
                    blockimages.Add(bi);
                    log("Blockimage: " + bi.Name);
                }

                if (searchParams.ContainsKey("minclustersize") && searchParams["minclustersize"] != null)
                    Int32.TryParse(searchParams["minclustersize"]!.ToString(), out minClusterSize);
            }
            catch (Exception e)
            {
                return "!Error - " + e.Message;
            }

            // All ok above - direct search successfully initialized
            initializationOk = true;
            log("Clearing previous search caches...");
            // Clear optimalSolutionsGlobal & checkedPartStrings
            optimalSolutionsGlobal.Clear();
            checkedPartString.Clear();
            log("Blockmodeling initialization done!");
            return "ok";
        }

        public static void doExhaustiveSearch()
        {
            log("Doing exhaustive search");
            double bestGof = (maximizeGof) ? double.NegativeInfinity : double.PositiveInfinity;
            
            // List for storing optimal solutions
            List<BMSolution> optimalSolutionsThisSearch = new List<BMSolution>();

            foreach (BlockImage blockimage in blockimages)
            {
                log("Blockimage:" + blockimage.Name);
                blockimage.GetContent(logLines);
                int nbrPositions = blockimage.nbrPositions;
                Partition partition = new Partition(matrix!.actorset, "part_" + blockimage.Name);
                partition.createClusters(nbrPositions);
                partition.setZeroPartition();

                int testindex = 0;
                while (partition.incrementPartition())
                {
                    if (!partition.CheckMinimumClusterSize(minClusterSize))
                        continue;
                    //log("Test " + testindex + ": " + partition.GetPartString());


                    BMSolution solution = gofMethod!(matrix, blockimage, partition);
                    if ((maximizeGof && solution.gofValue >=bestGof) || (!maximizeGof && solution.gofValue <= bestGof))
                    {
                        //log(solution.partString + ":" + solution.gofValue);
                        if (solution.gofValue != bestGof)
                            optimalSolutionsThisSearch.Clear();
                        optimalSolutionsThisSearch.Add(solution);
                        bestGof = solution.gofValue;
                    }


                    testindex++;
                }
                log("Exhaustive search done for this blockimage.");
                log("");
            }
            optimalSolutionsGlobal.AddRange(optimalSolutionsThisSearch);
        }

        public static void doLocalOptSearch()
        {
            log("Doing local optimization search");

        }


        public static BMSolution binaryHamming(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            int nbrPos = blockimage.nbrPositions;
            int[,] blockindices = new int[nbrPos, nbrPos];
            double penalty = 0, currentBlockPenalty, bestBlockPenalty;
            for (int r=0; r<nbrPos;r++)
                for (int c=0;c<nbrPos;c++)
                {
                    bestBlockPenalty = int.MaxValue;
                    for (int i=0; i<blockimage.blocks![r,c].Count;i++)
                    {
                        currentBlockPenalty = blockimage.GetBlock(r, c, i).getPenaltyHamming(matrix, partition.clusters[r], partition.clusters[c]);
                        if (currentBlockPenalty < bestBlockPenalty)
                        {
                            bestBlockPenalty = currentBlockPenalty;
                            blockindices[r, c] = i;
                        }
                    }
                    penalty += bestBlockPenalty;
                }

            return new BMSolution(matrix, blockimage, blockindices, partition.GetPartArrayCopy(), penalty, "hamming");
        }

        public static BMSolution ziberna2007(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            return new BMSolution(matrix, blockimage, new int[blockimage.nbrPositions, blockimage.nbrPositions], partition.GetPartArrayCopy(), 0, "ziberna");
        }

        public static BMSolution nordlund2020(Matrix matrix, BlockImage blockimage, Partition partition)
        {
            int nbrPos = blockimage.nbrPositions;
            List<Triple> triples = new List<Triple>();
            for (int r = 0; r < nbrPos; r++)
                for (int c = 0; c < nbrPos; c++)
                    triples.AddRange(blockimage.GetBlock(r, c).getTripletList(matrix, partition.clusters[r], partition.clusters[c]));
            return new BMSolution(matrix, blockimage, new int[nbrPos,nbrPos], partition.GetPartArrayCopy(), Functions.correlateTriplets(triples), "nordlund");
        }


        private static void log(string line)
        {
            logLines.Add(line);
        }

        internal static string StartSearch()
        {
            if (!initializationOk)
                return "!Error - Direct blockmodeling not yet properly initialized.";
            if (searchHeuristic == null)
                return "!Error - Search heuristic not set.";
            
            searchHeuristic();

            return "ok";
        }

        internal static List<BlockModel> generateBlockmodelStructuresFromBMSolutions()
        {
            List<BlockModel> blockmodels = new List<BlockModel>();
            int index = 0;
            foreach (BMSolution solution in optimalSolutionsGlobal)
            {
                Partition partition = new Partition(solution.matrix.actorset, "");
                partition.createClusters(solution.blockimage.nbrPositions);
                partition.setPartitionByPartArray(solution.partarray);
                string partString = partition.GetPartString();
                partition.Name = "part_" + index + "_" + solution.matrix.Name + "_" + solution.blockimage.Name;



                string bmName = "bm_" + solution.matrix.Name + "_" + solution.blockimage.Name + "_" + index;
                BlockModel blockmodel = new BlockModel(bmName, solution.matrix, solution.blockimage, partition, solution.blockindices,solution.gofValue, solution.criteriaFunction);
                blockmodels.Add(blockmodel);
                index++;
            }
            return blockmodels;
        }

    }

    public struct Triple
    {
        public double x, y, w;

        public Triple(double x, double y, double w)
        {
            this.x = x;
            this.y = y;
            this.w = w;
        }
    }

    public struct BMSolution
    {
        public Matrix matrix;
        public BlockImage blockimage;
        public int[,] blockindices;
        public int[] partarray;
        public double gofValue;
        public string criteriaFunction;

        public BMSolution(Matrix matrix, BlockImage blockimage, int[,] blockindices, int[] partarray, double gofValue, string criteriaFunction)
        {
            this.matrix = matrix;
            this.blockimage = blockimage;
            this.blockindices = blockindices;
            this.partarray = partarray;
            this.gofValue = gofValue;
            this.criteriaFunction = criteriaFunction;
        }

        //BMSolution(matrix, blockimage, new int[nbrPos, nbrPos], partition.GetPartArrayCopy(), Functions.correlateTriplets(triples), "nordlund");
    }


}
