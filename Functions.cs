using Socnet.DataLibrary;
using Socnet.DataLibrary.Blocks;
using System.Text.RegularExpressions;

namespace Socnet
{
    public static class Functions
    {

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
            { 11, "pco" },
            { 12, "pcdd" },
            { 13, "cpdd" }
        };

        public static List<string> conditionAbbrs = new List<string>() { "gt", "ge", "lt", "le", "eq", "ne" };
        public static List<string> symmMethods = new List<string>() { "max", "min", "minnonzero", "average", "sum", "difference", "ut", "lt" };

        internal static _Block? GetBlockInstance(string blockName)
        {
            //string pattern = @"^(([\w]+)\s*?=\s*?)?(\w+)(\((.*?)\))?$";
            //Match match = Regex.Match(command.Trim(), pattern);

            string pattern = @"^([\w]+)(\(([^\)]+)\))?";

            Match match = Regex.Match(blockName, pattern);
            if (!match.Success)
                return null;

            var objecttype = Type.GetType("Socnet.DataLibrary.Blocks." + match.Groups[1].Value + "Block");



            if (objecttype == null)
                return null;
            var instObj = Activator.CreateInstance(objecttype);
            if (instObj is _Block)
            {
                _Block instBlock = (_Block)instObj;
                if (match.Groups[3].Value.Length > 0)
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
                for (int r = 0; r < k; r++)
                    for (int c = 0; c < k; c++)
                    {
                        biIndexMatrix.data[r, c] = blockimageBase.GetBlock(r, c, indices[r, c]).primeIndex;
                        if (!increaseDone && maxIndices[r, c] > 1)
                        {
                            indices[r, c]++;
                            if (indices[r, c] < maxIndices[r, c])
                                increaseDone = true;
                            else
                                indices[r, c] = 0;
                        }
                    }

                bool foundPerfectSE = false;
                for (int a1 = 0; a1 < k && !foundPerfectSE; a1++)
                {
                    for (int a2 = a1 + 1; a2 < k && !foundPerfectSE; a2++)
                    {
                        bool foundError = false;
                        for (int i = 0; i < k && !foundError; i++)
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
            foreach (KeyValuePair<string, List<int[,]>> obj in isomorphDict)
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

        public static double correlateTriplets(List<Triple> triples)
        {
            double mx = 0, my = 0, sx = 0, sy = 0, sxy = 0, w_sum = 0, denom = 0;
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
            denom = Math.Sqrt(sx * sy);
            if (denom == 0)
                return -1;
            return sxy / denom;
        }

        public static DataStructure? Dichotomize(DataStructure structure, string condition, double threshold, double truevalue, double falsevalue)
        {
            if (structure is Matrix)
                return Dichotomize((Matrix)structure, condition, threshold, truevalue, falsevalue);
            else if (structure is Vector)
                return Dichotomize((Vector)structure, condition, threshold, truevalue, falsevalue);
            else if (structure is Table)
                return Dichotomize((Table)structure, condition, threshold, truevalue, falsevalue);
            else
                return null;
        }

        internal static Matrix Dichotomize(Matrix matrix, string condition, double threshold, double truevalue, double falsevalue)
        {
            Matrix dichMatrix = new Matrix(matrix.actorset, "dich_" + condition + threshold + "_" + matrix.Name, matrix.Cellformat);
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                    dichMatrix.Set(rowActor, colActor, Dichotomize(matrix.Get(rowActor, colActor), condition, threshold, truevalue, falsevalue));
            return dichMatrix;
        }

        internal static Table Dichotomize(Table table, string condition, double threshold, double truevalue, double falsevalue)
        {
            Table dichTable = new Table(table.rowActorset, table.colActorset, "dich_" + condition + threshold + "_" + table.Name, table.Cellformat);
            foreach (Actor rowActor in table.rowActorset.actors)
                foreach (Actor colActor in table.colActorset.actors)
                    dichTable.Set(rowActor, colActor, Dichotomize(table.Get(rowActor, colActor), condition, threshold, truevalue, falsevalue));
            return dichTable;
        }

        internal static Vector Dichotomize(Vector vector, string condition, double threshold, double truevalue, double falsevalue)
        {
            Vector dichVector = new Vector(vector.actorset, "dich_" + condition + threshold + "_" + vector.Name, vector.Cellformat);
            foreach (Actor actor in vector.actorset.actors)
                dichVector.Set(actor, Dichotomize(vector.Get(actor), condition, threshold, truevalue, falsevalue));
            return dichVector;
        }

        internal static double Dichotomize(double value, string condition, double threshold, double truevalue, double falsevalue)
        {
            bool conditionMet = (condition.Equals("eq") && value == threshold) ||
                (condition.Equals("ne") && value != threshold) ||
                (condition.Equals("ge") && value >= threshold) ||
                (condition.Equals("gt") && value > threshold) ||
                (condition.Equals("le") && value <= threshold) ||
                (condition.Equals("lt") && value < threshold);
            if (conditionMet)
                return (double.IsNaN(truevalue)) ? value : truevalue;
            else
                return (double.IsNaN(falsevalue)) ? value : falsevalue;
        }

        public static DataStructure? Rescale(DataStructure structure, double min, double max, bool incldiag = false)
        {
            if (structure is Matrix)
                return Rescale((Matrix)structure, min, max);
            return null;
        }

        public static Matrix? Rescale(Matrix matrix, double newMin, double newMax, bool incldiag = false)
        {
            double oldMin = GetMinValue(matrix, incldiag), oldMax = GetMaxValue(matrix, incldiag);
            Matrix rescaled = new Matrix(matrix.actorset, "rescaled" + newMin + "-" + newMax + "_" + matrix.Name, matrix.Cellformat);
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                    if (rowActor != colActor || incldiag)
                        rescaled.Set(rowActor, colActor, newMin + (matrix.Get(rowActor, colActor) - oldMin) * (newMax - newMin) / (oldMax - oldMin));
            return rescaled;
        }

        public static double GetMinValue(DataStructure structure, bool incldiag = false)
        {
            if (structure is Matrix)
                return GetMinValue((Matrix)structure, incldiag);
            return 0;
        }

        public static double GetMinValue(Matrix matrix, bool incldiag = false)
        {
            double min = double.MaxValue, val = 0;
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                    if (rowActor != colActor || incldiag)
                    {
                        val = matrix.Get(rowActor, colActor);
                        if (!double.IsNaN(val) && val < min)
                            min = val;
                    }
            return min;
        }

        public static double GetMaxValue(DataStructure structure, bool incldiag = false)
        {
            if (structure is Matrix)
                return GetMaxValue((Matrix)structure, incldiag);
            return 0;
        }

        public static double GetMaxValue(Matrix matrix, bool incldiag = false)
        {
            double max = double.MinValue, val = 0;
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                    if (rowActor != colActor || incldiag)
                    {
                        val = matrix.Get(rowActor, colActor);
                        if (!double.IsNaN(val) && val > max)
                            max = val;
                    }
            return max;
        }

        public static Matrix? Symmetrize(Matrix matrix, string method)
        {
            Func<double, double, double> symm;
            if (method.Equals("min"))
                symm = SymMin;
            else if (method.Equals("max"))
                symm = SymMax;
            else if (method.Equals("minnonzero"))
                symm = SymMinNonzero;
            else if (method.Equals("average"))
                symm = SymAverage;
            else if (method.Equals("sum"))
                symm = SymSum;
            else if (method.Equals("difference"))
                symm = SymDiff;
            else if (method.Equals("ut"))
                symm = SymUpperTriangle;
            else if (method.Equals("lt"))
                symm = SymLowerTriangle;
            else
                return null;

            double symval = 0;
            Matrix symmMatrix = new Matrix(matrix.actorset, "symm_" + method + "_" + matrix.Name, matrix.Cellformat);
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                    if (rowActor.index < colActor.index)
                    {
                        symval = symm(matrix.Get(rowActor, colActor), matrix.Get(colActor, rowActor));
                        symmMatrix.Set(rowActor, colActor, symval);
                        symmMatrix.Set(colActor, rowActor, symval);
                    }
            return symmMatrix;
        }

        public static double SymMax(double lt, double ut)
        {
            return Math.Max(lt, ut);
        }

        public static double SymMin(double lt, double ut)
        {
            return Math.Min(lt, ut);
        }

        public static double SymMinNonzero(double lt, double ut)
        {
            if (lt == 0 || ut == 0)
                return Math.Max(lt, ut);
            return Math.Min(lt, ut);
        }

        public static double SymAverage(double lt, double ut)
        {
            return (lt + ut) / 2;
        }

        public static double SymSum(double lt, double ut)
        {
            return (ut + lt);
        }

        public static double SymDiff(double lt, double ut)
        {
            return Math.Abs(lt - ut);
        }

        public static double SymUpperTriangle(double lt, double ut)
        {
            return ut;
        }

        public static double SymLowerTriangle(double lt, double ut)
        {
            return lt;
        }

        internal static double minMaxRange(double v, int min, int max)
        {
            if (v > max)
                return max;
            else if (v < min)
                return min;
            return v;
        }

        internal static double GetMedianValue(DataStructure structure, bool incldiag = false)
        {
            List<double> values = GetAllValues(structure, incldiag);
            values.Sort();
            int size = values.Count;
            if (size % 2 == 0)
                return values[size / 2] + values[(size / 2) - 1];
            else
                return values[size / 2];
        }

        private static List<double> GetAllValues(DataStructure structure, bool incldiag)
        {
            List<double> values = new List<double>();
            if (structure is Matrix)
            {
                Matrix mat = (Matrix)structure;
                foreach (Actor rowActor in mat.actorset.actors)
                    foreach (Actor colActor in mat.actorset.actors)
                        if (rowActor != colActor || incldiag && !double.IsNaN(mat.Get(rowActor, colActor)))
                            values.Add(mat.Get(rowActor, colActor));
            }
            else if (structure is Table)
            {
                Table table = (Table)structure;
                foreach (Actor rowActor in table.rowActorset.actors)
                    foreach (Actor colActor in table.colActorset.actors)
                        values.Add(table.Get(rowActor, colActor));
            }
            else if (structure is Vector)
            {
                Vector vec = (Vector)structure;
                foreach (Actor actor in vec.actorset.actors)
                    values.Add(vec.Get(actor));
            }
            return values;
        }
    }
}
