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

        public delegate BMSolution GofMethod(Matrix matrix, BlockImage blockimage);
        public static GofMethod? gofMethod;
        public static string gofMethodName = "";

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
                    gofMethod = binaryHamming;
                else if (gofMethodName.Equals("ziberna"))
                    gofMethod = ziberna2007;
                else if (gofMethodName.Equals("nordlund"))
                    gofMethod = nordlund2020;
                else
                    return "!Error - Method '" + gofMethodName + "' not implemented";
                log("Method: " + gofMethodName);

                searchTypeName = "" + searchParams["searchtype"] as string;
                if (searchTypeName.Equals("localopt"))
                    searchHeuristic = doExhaustiveSearch;
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
                // Ok - when do I need to create varieties? When can I keep a multiblock as it is?
                // If method is "hamming" or "ziberna", then I can use a multiblocked blockimage as a singular blockimage: it will sort itself out
                // If method is "nordlund", then I can't use multiblocked blockimage: instead I have to create all individual varieties
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

        }

        public static void doLocalOptSearch()
        {

        }


        public static BMSolution binaryHamming(Matrix matrix, BlockImage blockimage)
        {
            return new BMSolution();
        }

        public static BMSolution ziberna2007(Matrix matrix, BlockImage blockimage)
        {
            return new BMSolution();
        }

        public static BMSolution nordlund2020(Matrix matrix, BlockImage blockimage)
        {
            return new BMSolution();
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


            return "ok";
        }
    }
    public struct BMSolution
    {
        public Matrix matrix;
        public BlockImage blockimage;


    }
}
