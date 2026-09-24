using Socnet.Core.Blocks;
using Socnet.Core.Model;

namespace Socnet.Core.Blockmodeling
{
    /// <summary>
    /// An immutable description of one blockmodeling search problem: a network and a blockimage to fit it to,
    /// with a goodness-of-fit method. Shared (read-only) by all search workers.
    /// </summary>
    internal sealed class SearchProblem
    {
        public SearchProblem(Matrix matrix, BlockImage blockimage, GofMethod method, int minClusterSize)
        {
            N = matrix.N;
            K = blockimage.NbrPositions;
            X = matrix.Data;
            XT = new double[N * N];
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                    XT[j * N + i] = X[i * N + j];
            Method = method;
            BlockImage = blockimage;
            MinClusterSize = minClusterSize;

            // With the correlation-based measure, only the first ideal block in each position is used
            // (multi-blocked blockimages are split into single-blocked varieties before searching)
            Cells = new IdealBlock[K * K][];
            for (int r = 0; r < K; r++)
                for (int c = 0; c < K; c++)
                    Cells[r * K + c] = method == GofMethod.Hamming
                        ? [.. blockimage.Blocks(r, c)]
                        : [blockimage.GetBlock(r, c)];

            // Blockimages where all diagonal positions are identical and all off-diagonal positions are identical
            // are invariant under any permutation of positions
            string diag = CellKey(0, 0), offDiag = K > 1 ? CellKey(0, 1) : "";
            bool symmetric = true;
            for (int r = 0; r < K && symmetric; r++)
                for (int c = 0; c < K && symmetric; c++)
                    symmetric = CellKey(r, c) == (r == c ? diag : offDiag);
            IsFullySymmetric = symmetric;

            // Properties deciding which fast evaluation paths can be used
            bool simple = true, hasNaN = false;
            for (int i = 0; i < N; i++)
                for (int j = 0; j < N; j++)
                {
                    double x = X[i * N + j];
                    hasNaN |= double.IsNaN(x);
                    simple &= i == j ? x == 0 : x >= 0;
                }
            SimpleValues = simple;
            // Maxima are not tracked for networks with missing (NaN) values, where the full evaluation must be used
            NeedsMaxStats = !hasNaN && method == GofMethod.Nordlund && Cells.Any(cell => cell.Any(b => b.UsesMaxStats && b.HasFastNordlund(simple)));
            AllCellsFast = Cells.All(cell => cell.All(b => b.HasFastPath(method, simple) && !(hasNaN && b.UsesMaxStats)));
        }

        /// <summary>
        /// True if all network values are non-negative (not NaN) and the diagonal is zero.
        /// </summary>
        public bool SimpleValues { get; }

        /// <summary>
        /// True if some ideal block needs row/column maxima for its fast evaluation.
        /// </summary>
        public bool NeedsMaxStats { get; }

        /// <summary>
        /// True if all ideal blocks can be evaluated from aggregated statistics, so that moves can be evaluated
        /// without changing the search state.
        /// </summary>
        public bool AllCellsFast { get; }

        private string CellKey(int r, int c) => string.Join(";", Cells[r * K + c].Select(b => b.ToString()));

        /// <summary>Number of actors.</summary>
        public int N { get; }

        /// <summary>Number of positions/clusters.</summary>
        public int K { get; }

        /// <summary>The network values (row-major).</summary>
        public double[] X { get; }

        /// <summary>The transposed network values (row-major), for fast column access.</summary>
        public double[] XT { get; }

        /// <summary>The goodness-of-fit method.</summary>
        public GofMethod Method { get; }

        /// <summary>True if the goodness-of-fit is to be maximized (correlation), false if minimized (penalty).</summary>
        public bool Maximize => Method == GofMethod.Nordlund;

        /// <summary>The blockimage being fitted.</summary>
        public BlockImage BlockImage { get; }

        /// <summary>Candidate ideal blocks for each block position (row-major k x k).</summary>
        public IdealBlock[][] Cells { get; }

        /// <summary>Minimum number of actors in each cluster.</summary>
        public int MinClusterSize { get; }

        /// <summary>
        /// True if the blockimage is invariant under all permutations of its positions, so that partitions
        /// only differing in the labelling of clusters are equivalent.
        /// </summary>
        public bool IsFullySymmetric { get; }
    }
}
