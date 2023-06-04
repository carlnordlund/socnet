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
        public static List<string> searchTypes = new List<string>() { "localopt", "exhaustive" };
        public static List<string> gofMethods = new List<string>() { "hamming", "nordlund", "ziberna" };

        public static List<BMSolution> optimalSolutionsGlobal = new List<BMSolution>();
        public static HashSet<string> checkedPartString = new HashSet<string>();

        public delegate void SearchHeuristic();
        public static SearchHeuristic? searchHeuristic;

        public delegate BMSolution GofMethod(Matrix matrix, BlockImage blockimage);
        public static GofMethod? gofMethod;
        public static string gofMethodName = "";


        public static List<string> logLines = new List<string>();

        public static Matrix? matrix = null;
        public static List<BlockImage> blockimages = new List<BlockImage>();

        internal static string InitializeSearch(Dictionary<string, object?> searchParams)
        {
            // Clear optimalSolutionsGlobal & checkedPartStrings
            optimalSolutionsGlobal.Clear();
            checkedPartString.Clear();
            try
            {
                log("Initializing search");
                // Set network to search
                matrix = searchParams["network"] as Matrix;
                log("Network: " + matrix);

                gofMethodName = "" + searchParams["method"] as string;
                if (gofMethodName.Equals("hamming"))
                    gofMethod = binaryHamming;
                else if (gofMethodName.Equals("ziberna"))
                    gofMethod = ziberna2007;
                else if (gofMethodName.Equals("nordlund"))
                    gofMethod = nordlund2020;
                else
                    return "!Error - Method '" + gofMethodName + "' not implemented";

                // Set blockimage(s)
                blockimages.Clear();
                BlockImage? bi = searchParams["blockimage"] as BlockImage;
                if (bi == null)
                    return "!Error - 'blockimage' is null";

                // Ok - when do I need to create varieties? When can I keep a multiblock as it is?
                // If method is "hamming" or "ziberna2007", then I can use a multiblocked blockimage as a singular blockimage: it will sort itself out
                // If method is "nordlund2020", then I can't use multiblocked blockimage: instead I have to create all individual varieties
                if (bi.multiBlocked && gofMethod == nordlund2020)
                {
                    // Multiblocked for Nordlund2020: need to unwrap this and create non-multiblocked blockimages and place into blockimages
                    blockimages.AddRange(Functions.GetBlockImageVarieties(bi));

                }
                else
                {
                    // Ok: either not nordlund2020 or not multiblocked: just add this individual blockimage
                    blockimages.Add(bi);
                }





            }
            catch (Exception e)
            {
                return "!Error - " + e.Message;
            }

            return "ok";
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
    }
    public struct BMSolution
    {
        public Matrix matrix;
        public BlockImage blockimage;


    }
}
