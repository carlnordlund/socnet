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
        public static List<string> searchTypes = new List<string>() { "localopt", "exhaustive", "testpartition" };
        public static List<string> gofMethods = new List<string>() { "hamming", "nordlund", "ziberna" };

        public static List<BMSolution> optimalSolutionsGlobal = new List<BMSolution>();
        public static HashSet<string> checkedPartString = new HashSet<string>();

        public delegate void SearchHeuristic();
        public static SearchHeuristic? searchHeuristic;

        public static List<string> logLines = new List<string>();

        public static Matrix? matrix = null;
        public static List<BlockImage> blockimages = new List<BlockImage>();

        internal static string InitializeSearch(Dictionary<string, object?> searchParams)
        {
            // Clear optimalSolutionsGlobal, checkedPartStrings
            optimalSolutionsGlobal.Clear();
            checkedPartString.Clear();
            try
            {
                log("Initializing search");
                matrix = searchParams["network"] as Matrix;
                log("Matrix: " + matrix);


                
            }
            catch (Exception e)
            {
                return "!Error - " + e.Message;
            }

            return "ok";
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
