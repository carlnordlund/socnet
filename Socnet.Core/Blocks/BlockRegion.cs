namespace Socnet.Core.Blocks
{
    /// <summary>
    /// A view of one block of a network: the cells between a set of row actors and a set of column actors.
    /// If <see cref="Diagonal"/> is true, rows and columns are the same cluster and the diagonal cells
    /// (an actor's tie to itself) are not part of the block.
    /// </summary>
    public readonly ref struct BlockRegion
    {
        public BlockRegion(ReadOnlySpan<int> rows, ReadOnlySpan<int> cols, bool diagonal, double[] data, int n, double[] scratch)
        {
            Rows = rows;
            Cols = cols;
            Diagonal = diagonal;
            Data = data;
            N = n;
            Scratch = scratch;
        }

        /// <summary>Actor indices of the block rows.</summary>
        public ReadOnlySpan<int> Rows { get; }

        /// <summary>Actor indices of the block columns.</summary>
        public ReadOnlySpan<int> Cols { get; }

        /// <summary>True if rows and columns are the same cluster (diagonal cells excluded).</summary>
        public bool Diagonal { get; }

        /// <summary>The network values, row-major with row length <see cref="N"/>.</summary>
        public double[] Data { get; }

        /// <summary>The number of actors in the network.</summary>
        public int N { get; }

        /// <summary>A scratch buffer with room for at least N*N values, used by blocks that need sorting.</summary>
        public double[] Scratch { get; }

        /// <summary>Number of rows in the block.</summary>
        public int Nr => Rows.Length;

        /// <summary>Number of columns in the block.</summary>
        public int Nc => Cols.Length;

        /// <summary>Number of cells in the block (excluding the diagonal for diagonal blocks).</summary>
        public int NbrCells => Rows.Length * Cols.Length - (Diagonal ? Rows.Length : 0);

        /// <summary>Returns the value of the cell (i,j).</summary>
        public double X(int i, int j) => Data[i * N + j];
    }

    /// <summary>
    /// Aggregated statistics for a block, maintained incrementally during searches. Allows most ideal blocks
    /// to compute their penalty (and some their correlation contributions) in constant time.
    /// </summary>
    public struct BlockStats
    {
        /// <summary>Number of rows and columns.</summary>
        public int Nr, Nc;
        /// <summary>True if this is a diagonal block.</summary>
        public bool Diagonal;
        /// <summary>Number of cells with value &gt; 0.</summary>
        public int Pos;
        /// <summary>Number of cells with value &gt;= 1.</summary>
        public int Ge1;
        /// <summary>Number of rows with at least one value &gt; 0.</summary>
        public int Pr;
        /// <summary>Number of columns with at least one value &gt; 0.</summary>
        public int Pc;
        /// <summary>Sum of values, and sum of squared values.</summary>
        public double Sum, SumSq;
        /// <summary>True if the maxima below are available.</summary>
        public bool HasMax;
        /// <summary>True if all network values are non-negative and the diagonal is zero.</summary>
        public bool SimpleValues;
        /// <summary>Sum (and sum of squares) over rows of the maximum value in the row (excluding the diagonal).</summary>
        public double SumRowMax, SumRowMaxSq;
        /// <summary>Sum (and sum of squares) over columns of the maximum value in the column (excluding the diagonal).</summary>
        public double SumColMax, SumColMaxSq;

        /// <summary>Number of cells in the block (excluding the diagonal for diagonal blocks).</summary>
        public readonly int NbrCells => Nr * Nc - (Diagonal ? Nr : 0);
    }
}
