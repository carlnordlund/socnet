using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet
{
    /// <summary>
    /// This class contains static information about the ideal blocks, the goodness-of-fit measures,
    /// and the search algorithms that are currently implemented. As certain goodness-of-fit measures
    /// only work for specific ideal blocks, this is also specified here.
    /// When expanding Socnet.se by either implementing a new ideal block, a new goodness-of-fit measure,
    /// or a new search algorithm, this must also be updated here.
    /// </summary>
    internal static class BlockmodelingConstants
    {
        // Dictionary for ideal Blocks, mapping iso indices to their respective Block names
        // Note: these must be identical to the isoIndex values specified in each block class
        public static Dictionary<int, string> IndexToIdealBlockName = new Dictionary<int, string>() {
            { 0, "dnc" },
            { 1, "nul" },
            { 2, "com" },
            { 3, "reg" },
            { 4, "rre" },
            { 5, "cre" },
            { 6, "rfn" },
            { 7, "cfn" },
            { 8, "denuci" },
            { 9, "den" },
            { 10, "denmin" },
            { 11, "pco" },
            { 12, "pcdd" },
            { 13, "cpdd" }
        };

        // Specifies the types of goodness-of-fit measures currently implemented
        public static List<string> GofMethods = new List<string>() { "hamming", "nordlund" };

        // Specifies which ideal blocks that are implemented (i.e. may be used) for respective goodness-of-fit measure
        public static Dictionary<string, List<string>> AvailableBlocks = new Dictionary<string, List<string>>()
        {
            { "hamming", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn", "den", "denmin" } },
            { "nordlund", new List<string>() { "dnc","nul","com","reg","rre","cre","rfn","cfn", "denuci", "den", "denmin","pco","cpdd","pcdd" } }
        };

        // Specifies the types of search algorithms currently implemented
        public static List<string> SearchTypes = new List<string>() { "localopt", "exhaustive", "ljubljana" };
    }
}
