using Socnet.Core.Model;
using Socnet.Core.Utilities;

namespace Socnet.Core.Processing
{
    /// <summary>
    /// Functions for transforming and summarizing matrices and other data structures.
    /// </summary>
    public static class MatrixFunctions
    {
        /// <summary>
        /// The available conditions for dichotomization.
        /// </summary>
        public static readonly string[] ConditionAbbrs = ["gt", "ge", "lt", "le", "eq", "ne"];

        /// <summary>
        /// The available symmetrization methods.
        /// </summary>
        public static readonly string[] SymmMethods = ["max", "min", "minnonzero", "average", "sum", "difference", "ut", "lt"];

        /// <summary>
        /// Dichotomizes a Matrix, Table or Vector. Values meeting the condition are set to truevalue, others to
        /// falsevalue; if either of these is NaN, the original value is kept.
        /// </summary>
        public static DataStructure? Dichotomize(DataStructure structure, string condition, double threshold, double truevalue, double falsevalue)
        {
            string name = "dich_" + condition + Fmt.D(threshold) + "_" + structure.Name;
            switch (structure)
            {
                case Matrix matrix:
                    Matrix dm = new(matrix.Actorset, name);
                    for (int i = 0; i < matrix.Data.Length; i++)
                        dm.Data[i] = Dichotomize(matrix.Data[i], condition, threshold, truevalue, falsevalue);
                    return dm;
                case Table table:
                    Table dt = new(table.RowActorset, table.ColActorset, name);
                    for (int r = 0; r < table.RowActorset.Count; r++)
                        for (int c = 0; c < table.ColActorset.Count; c++)
                            dt.Data[r, c] = Dichotomize(table.Data[r, c], condition, threshold, truevalue, falsevalue);
                    return dt;
                case Vector vector:
                    Vector dv = new(vector.Actorset, name);
                    for (int i = 0; i < vector.Data.Length; i++)
                        dv.Data[i] = Dichotomize(vector.Data[i], condition, threshold, truevalue, falsevalue);
                    return dv;
                default:
                    return null;
            }
        }

        private static double Dichotomize(double value, string condition, double threshold, double truevalue, double falsevalue)
        {
            bool conditionMet = condition switch
            {
                "eq" => value == threshold,
                "ne" => value != threshold,
                "ge" => value >= threshold,
                "gt" => value > threshold,
                "le" => value <= threshold,
                "lt" => value < threshold,
                _ => false
            };
            if (conditionMet)
                return double.IsNaN(truevalue) ? value : truevalue;
            return double.IsNaN(falsevalue) ? value : falsevalue;
        }

        /// <summary>
        /// Symmetrizes a matrix with the given method. Returns null if the method is unknown.
        /// The diagonal of the resulting matrix is zero.
        /// </summary>
        public static Matrix? Symmetrize(Matrix matrix, string method)
        {
            Func<double, double, double>? symm = method switch
            {
                "min" => Math.Min,
                "max" => Math.Max,
                "minnonzero" => (lt, ut) => (lt == 0 || ut == 0) ? Math.Max(lt, ut) : Math.Min(lt, ut),
                "average" => (lt, ut) => (lt + ut) / 2,
                "sum" => (lt, ut) => ut + lt,
                "difference" => (lt, ut) => Math.Abs(lt - ut),
                "ut" => (lt, ut) => ut,
                "lt" => (lt, ut) => lt,
                _ => null
            };
            if (symm == null)
                return null;
            int n = matrix.N;
            Matrix sm = new(matrix.Actorset, "symm_" + method + "_" + matrix.Name);
            for (int r = 0; r < n; r++)
                for (int c = r + 1; c < n; c++)
                {
                    double v = symm(matrix[r, c], matrix[c, r]);
                    sm[r, c] = v;
                    sm[c, r] = v;
                }
            return sm;
        }

        /// <summary>
        /// Rescales the values of a matrix linearly to the range [newMin, newMax]. Unless incldiag is set,
        /// the diagonal is ignored (and set to zero).
        /// </summary>
        public static Matrix Rescale(Matrix matrix, double newMin, double newMax, bool incldiag = false)
        {
            int n = matrix.N;
            double oldMin = GetMinValue(matrix.Data, n, incldiag), oldMax = GetMaxValue(matrix.Data, n, incldiag);
            Matrix rescaled = new(matrix.Actorset, "rescaled" + Fmt.D(newMin) + "-" + Fmt.D(newMax) + "_" + matrix.Name);
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (r != c || incldiag)
                        rescaled[r, c] = newMin + (matrix[r, c] - oldMin) * (newMax - newMin) / (oldMax - oldMin);
            return rescaled;
        }

        /// <summary>
        /// Minimum non-NaN value of an n x n row-major array (optionally including the diagonal).
        /// </summary>
        public static double GetMinValue(double[] data, int n, bool incldiag)
        {
            double min = double.MaxValue;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (r != c || incldiag)
                    {
                        double val = data[r * n + c];
                        if (!double.IsNaN(val) && val < min)
                            min = val;
                    }
            return min;
        }

        /// <summary>
        /// Maximum non-NaN value of an n x n row-major array (optionally including the diagonal).
        /// </summary>
        public static double GetMaxValue(double[] data, int n, bool incldiag)
        {
            double max = double.MinValue;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (r != c || incldiag)
                    {
                        double val = data[r * n + c];
                        if (!double.IsNaN(val) && val > max)
                            max = val;
                    }
            return max;
        }

        /// <summary>
        /// The threshold used when displaying valued blockmodels: computed exactly as in Socnet.se 1.4, where
        /// the 'median' of the off-diagonal values is the sum (not the average) of the two middle values when
        /// their number is even. Kept as is so that displayed blockmodels are identical to earlier versions.
        /// </summary>
        public static double GetDisplayMedian(double[] data, int n)
        {
            List<double> values = new(n * n);
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                    if (r != c)
                        values.Add(data[r * n + c]);
            values.Sort();
            int size = values.Count;
            if (size == 0)
                return double.NaN;
            if (size % 2 == 0)
                return values[size / 2] + values[(size / 2) - 1];
            return values[size / 2];
        }

        /// <summary>
        /// Returns the k x k matrix of block densities (average value in each block, excluding the diagonal),
        /// rounded to 4 decimals. Blocks without cells get NaN.
        /// </summary>
        public static double[,] Densities(Matrix matrix, Partition partition)
        {
            int k = partition.NbrClusters, n = matrix.N;
            double[,] sums = new double[k, k];
            double[,] counts = new double[k, k];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (i != j)
                    {
                        int r = partition.ClusterOf[i], c = partition.ClusterOf[j];
                        sums[r, c] += matrix[i, j];
                        counts[r, c]++;
                    }
            double[,] densities = new double[k, k];
            for (int r = 0; r < k; r++)
                for (int c = 0; c < k; c++)
                    densities[r, c] = Math.Round(sums[r, c] / counts[r, c], 4);
            return densities;
        }
    }
}
