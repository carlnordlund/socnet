using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Transactions;
using Socnet.DataLibrary;
using Socnet.DataLibrary.Blocks;

namespace Socnet
{
    public static class Functions
    {
        public static List<string> idealBlockNames = new List<string>() { "dnc", "nul", "com", "reg", "rre", "cre", "rfn", "cfn", "denuci", "den", "denmin", "kco", "pco", "sumreg", "meanreg", "maxreg", "vcom" };

        public static Dictionary<int, string> indexToIdealBlockName = new Dictionary<int, string>() {
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
            { 11, "kco" },
            { 12, "pco" },
            { 13, "sumreg" },
            { 14, "meanreg" },
            { 15, "maxreg" },
            { 16, "vcom" } };

        internal static _Block? GetBlockInstance(string blockName)
        {
            //string pattern = @"^(([\w]+)\s*?=\s*?)?(\w+)(\((.*?)\))?$";
            //Match match = Regex.Match(command.Trim(), pattern);

            string pattern = @"^([\w]+)(\(([^\)]+)\))?";

            Match match = Regex.Match(blockName, pattern);
            if (!match.Success)
                return null;

            var objecttype = Type.GetType("Socnet.DataLibrary.Blocks." + match.Groups[1].Value +"Block");



            if (objecttype == null)
                return null;
            var instObj = Activator.CreateInstance(objecttype);
            if (instObj is _Block)
            {
                _Block instBlock = (_Block)instObj;
                if (match.Groups[3].Value.Length>0)
                {
                    double v = 0;
                    if (double.TryParse(match.Groups[3].Value, out v))
                        instBlock.initArgValue(v);
                }
                return instBlock;
            }
            return null;
        }

        internal static List<_Block> GetBlockInstances(string[] blockNames)
        {
            List<_Block> blocks = new List<_Block>();
            List<string> blocksAlready = new List<string>();
            foreach (string blockName in blockNames)
            {
                if (!blocksAlready.Contains(blockName))
                {
                    _Block? block = GetBlockInstance(blockName);
                    if (block != null)
                    {
                        blocks.Add(block);
                        blocksAlready.Add(blockName);
                    }
                }
            }
            return blocks;
        }

        internal static List<BlockImage> GetBlockImageVarieties(BlockImage blockimageBase)
        {
            int k = blockimageBase.nbrPositions;
            Actorset biActors = new Actorset("biActors");
            for (int i = 0; i < k; i++)
                biActors.actors.Add(new Actor(blockimageBase.positionNames[i], i));
            Matrix biIndexMatrix = new Matrix(biActors, "", "N0");

            Dictionary<string, List<int[,]>> isomorphDict = new Dictionary<string, List<int[,]>>();
            int[,] maxIndices = new int[k, k];
            int[,] indices = new int[k, k];
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                    maxIndices[r, c] = blockimageBase.blocks![r, c].Count;

            bool cont = true;
            while (cont)
            {
                bool increaseDone = false;
                for (int r=0; r<k;r++)
                    for (int c=0;c<k;c++)
                    {
                        biIndexMatrix.data[r, c] = blockimageBase.GetBlock(r, c, indices[r, c]).primeIndex;
                        if (!increaseDone && maxIndices[r,c]>1)
                        {
                            indices[r, c]++;
                            if (indices[r, c] < maxIndices[r, c])
                                increaseDone = true;
                            else
                                indices[r, c] = 0;
                        }
                    }

                bool foundPerfectSE = false;
                for (int a1=0;a1<k && !foundPerfectSE;a1++)
                {
                    for (int a2= a1+1;a2<k && !foundPerfectSE; a2++)
                    {
                        bool foundError = false;
                        for (int i=0; i<k && !foundError; i++)
                        {
                            if (biIndexMatrix.data[a1, 1] != biIndexMatrix.data[a2, 1] || biIndexMatrix.data[i, a1] != biIndexMatrix.data[i, a2])
                                foundError = true;
                        }
                        if (!foundError)
                            foundPerfectSE = true;
                    }
                }
                if (!foundPerfectSE)
                {
                    Dictionary<string, DataStructure> eigenData = Functions.Eigen2(biIndexMatrix);
                    double[] eigenvalues = ((Vector)eigenData["dmds"]).data;
                    Array.Sort(eigenvalues);
                    Array.Reverse(eigenvalues);
                    string keyString = ((Vector)eigenData["dmds"]).GetValueString();

                    int[,] currentData = new int[k, k];
                    for (int r = 0; r < k; r++)
                        for (int c = 0; c < k; c++)
                            currentData[r, c] = (int)biIndexMatrix.data[r, c];
                    if (!isomorphDict.ContainsKey(keyString))
                    {
                        isomorphDict.Add(keyString, new List<int[,]>());
                        isomorphDict[keyString].Add(currentData);
                    }
                    else
                    {
                        bool foundIsomorphic = Functions.CheckForIsomorphism(currentData, isomorphDict[keyString]);
                        if (!foundIsomorphic)
                            isomorphDict[keyString].Add(currentData);
                    }
                }
                if (!increaseDone)
                    cont = false;
            }


            List<BlockImage> blockimages = new List<BlockImage>();
            int index = 0;
            foreach (KeyValuePair<string,List<int[,]>> obj in isomorphDict)
            {
                foreach (int[,] data in obj.Value)
                {
                    BlockImage bi = new BlockImage(blockimageBase.Name + "_" + index, k);
                    bi.setBlocksByPrimeIndices(data);
                    blockimages.Add(bi);
                    index++;
                }
            }
            return blockimages;
        }

        private static bool CheckForIsomorphism(int[,] currentData, List<int[,]> existingDataList)
        {
            int k = currentData.GetLength(0);
            int[] indices = new int[k];
            for (int i = 0; i < k; i++)
                indices[i] = i;
            bool foundIsomorphic = false;

            GetPer(indices, currentData, existingDataList, 0, k - 1, ref foundIsomorphic);

            return foundIsomorphic;
        }

        private static void GetPer(int[] indices, int[,] currentData, List<int[,]> existingDataList, int k, int m, ref bool foundIsomorphic)
        {
            if (foundIsomorphic)
                return;
            if (k == m)
            {
                foreach (int[,] existing in existingDataList)
                {
                    bool foundError = false;
                    for (int r = 0; r < indices.Length && !foundError; r++)
                        for (int c = 0; c < indices.Length && !foundError; c++)
                            if (existing[r, c] != currentData[indices[r], indices[c]])
                                foundError = true;
                    if (!foundError)
                    {
                        foundIsomorphic = true;
                        break;
                    }
                }
            }
            else
            {
                for (int i = k; i <= m; i++)
                {
                    Swap(ref indices[k], ref indices[i]);
                    GetPer(indices, currentData, existingDataList, k + 1, m, ref foundIsomorphic);
                    Swap(ref indices[k], ref indices[i]);
                }
            }
        }

        private static void Swap(ref int a, ref int b)
        {
            if (a == b)
                return;
            var temp = a;
            a = b;
            b = temp;
        }


        public static Dictionary<string, DataStructure> Eigen2(Matrix matrix)
        {
            Dictionary<string, DataStructure> returnStructures = new Dictionary<string, DataStructure>();

            DotNetMatrix.EigenvalueDecomposition evd = new DotNetMatrix.EigenvalueDecomposition(matrix);

            double[] eigenvalues = evd.RealEigenvalues;
            double[] eigenvalues_i = evd.ImagEigenvalues;
            double[][] eigenvectors = evd.V;

            int n = matrix.actorset.Count;
            Matrix vmds = new Matrix(matrix.actorset, "eigenvectors", "N4");
            Vector dmds = new Vector(matrix.actorset, "eigenvalues", "N4");
            Vector imds = new Vector(matrix.actorset, "eigenvalues_i", "N4");
            for (int r = 0; r < n; r++)
            {
                dmds.data[r] = eigenvalues[r];
                imds.data[r] = eigenvalues_i[r];
                for (int c = 0; c < n; c++)
                {
                    vmds.data[r, c] = eigenvectors[r][c];
                }
            }

            returnStructures.Add("vmds", vmds);
            returnStructures.Add("dmds", dmds);
            returnStructures.Add("imds", imds);


            return returnStructures;
        }

        internal static double correlateTriplets(List<Triple> triples)
        {
            double mx = 0, my = 0, sx = 0, sy = 0, sxy = 0, w_sum = 0;
            foreach (Triple t in triples)
            {
                mx += t.w * t.x;
                my += t.w * t.y;
                w_sum += t.w;
            }
            mx /= w_sum;
            my /= w_sum;

            foreach (Triple t in triples)
            {
                sx += t.w * (t.x - mx) * (t.x - mx);
                sy += t.w * (t.y - my) * (t.y - my);
                sxy += t.w * (t.x - mx) * (t.y - my);
            }
            sx /= w_sum;
            sy /= w_sum;
            sxy /= w_sum;

            return sxy / Math.Sqrt(sx * sy);

        }
    }
}
