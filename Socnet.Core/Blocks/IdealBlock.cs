using Socnet.Core.Utilities;
using System.Text.RegularExpressions;

namespace Socnet.Core.Blocks
{
    /// <summary>
    /// Abstract base class for an ideal block.
    ///
    /// To add a new ideal block:
    /// 1. Create a subclass (see the existing blocks for examples), give it a unique <see cref="Name"/>
    ///    and a unique <see cref="IsoIndex"/>.
    /// 2. Implement <see cref="Hamming"/> and <see cref="WriteIdealHamming"/> if it can be used with the
    ///    'hamming' goodness-of-fit measure, and/or <see cref="Nordlund"/> and <see cref="WriteIdealNordlund"/>
    ///    if it can be used with the 'nordlund' measure, and set <see cref="SupportsHamming"/> /
    ///    <see cref="SupportsNordlund"/> accordingly.
    /// 3. Optionally implement the fast paths <see cref="TryHamming"/> / <see cref="TryNordlund"/>, which
    ///    compute the same result from aggregated <see cref="BlockStats"/> in constant time. Searches use
    ///    these when available and otherwise fall back to the full evaluation.
    /// 4. Register the block in <see cref="BlockFactory"/>.
    /// </summary>
    public abstract class IdealBlock
    {
        /// <summary>
        /// The name of the ideal block, as used in blockimage patterns.
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Unique index of this ideal block type, used when determining whether blockimages are isomorphic.
        /// </summary>
        public abstract int IsoIndex { get; }

        /// <summary>
        /// True if this block type takes a parameter, e.g. den(0.5).
        /// </summary>
        public virtual bool HasParameter => false;

        /// <summary>
        /// The parameter value (only meaningful if <see cref="HasParameter"/> is true).
        /// </summary>
        public double Parameter { get; protected set; }

        /// <summary>
        /// Sets the parameter value (ignored by blocks without parameter).
        /// </summary>
        public virtual void SetParameter(double value) { }

        /// <summary>True if the block can be used with the 'hamming' goodness-of-fit measure.</summary>
        public abstract bool SupportsHamming { get; }

        /// <summary>True if the block can be used with the 'nordlund' goodness-of-fit measure.</summary>
        public abstract bool SupportsNordlund { get; }

        /// <summary>
        /// Returns a new instance of this ideal block (with the same parameter).
        /// </summary>
        public abstract IdealBlock Clone();

        /// <summary>
        /// Returns the number of inconsistencies between the block and this ideal block.
        /// </summary>
        public virtual double Hamming(in BlockRegion b) => 0;

        /// <summary>
        /// Computes the Hamming penalty from aggregated block statistics. Returns false if not supported.
        /// </summary>
        public virtual bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = 0;
            return false;
        }

        /// <summary>
        /// Adds the (observed, ideal, weight) triplets of this block for the weighted correlation measure.
        /// </summary>
        public virtual void Nordlund(in BlockRegion b, TripleSink sink) { }

        /// <summary>
        /// Adds the weighted correlation sums of this block computed from aggregated block statistics.
        /// Returns false if not supported.
        /// </summary>
        public virtual bool TryNordlund(in BlockStats s, ref CorrSums sums) => false;

        /// <summary>
        /// Writes the ideal pattern of this block (for the 'hamming' measure) into an n*n row-major array.
        /// </summary>
        public virtual void WriteIdealHamming(in BlockRegion b, double[] ideal) { }

        /// <summary>
        /// Writes the ideal pattern of this block (for the 'nordlund' measure) into an n*n row-major array.
        /// </summary>
        public virtual void WriteIdealNordlund(in BlockRegion b, double[] ideal) { }

        /// <summary>
        /// Returns true if the block supports the given goodness-of-fit method.
        /// </summary>
        public bool Supports(GofMethod method) => method == GofMethod.Hamming ? SupportsHamming : SupportsNordlund;

        public override string ToString() => HasParameter ? $"{Name}({Fmt.D(Parameter)})" : Name;

        /// <summary>
        /// Clamps a value to the interval [min, max].
        /// </summary>
        protected static double Clamp01(double v) => v > 1 ? 1 : v < 0 ? 0 : v;

        /// <summary>
        /// Sets all cells of the block to the given value in an ideal matrix.
        /// </summary>
        protected static void FillIdeal(in BlockRegion b, double[] ideal, double value)
        {
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        ideal[i * b.N + j] = value;
        }

        /// <summary>
        /// Number of rows with at least one value &gt; 0 (excluding the diagonal).
        /// </summary>
        protected static int CountNonNullRows(in BlockRegion b)
        {
            int pr = 0;
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j && b.X(i, j) > 0)
                    {
                        pr++;
                        break;
                    }
            return pr;
        }

        /// <summary>
        /// Number of columns with at least one value &gt; 0 (excluding the diagonal).
        /// </summary>
        protected static int CountNonNullCols(in BlockRegion b)
        {
            int pc = 0;
            foreach (int j in b.Cols)
                foreach (int i in b.Rows)
                    if (i != j && b.X(i, j) > 0)
                    {
                        pc++;
                        break;
                    }
            return pc;
        }

        /// <summary>
        /// Number of cells with value &gt; 0 (excluding the diagonal).
        /// </summary>
        protected static int CountPositive(in BlockRegion b)
        {
            int count = 0;
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j && b.X(i, j) > 0)
                        count++;
            return count;
        }

        /// <summary>
        /// For the ideal matrix: marks the first tie (value &gt; 0) in each row with 1.
        /// </summary>
        protected static void MarkFirstTiePerRow(in BlockRegion b, double[] ideal)
        {
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j && b.X(i, j) > 0)
                    {
                        ideal[i * b.N + j] = 1;
                        break;
                    }
        }

        /// <summary>
        /// For the ideal matrix: marks the first tie (value &gt; 0) in each column with 1.
        /// </summary>
        protected static void MarkFirstTiePerCol(in BlockRegion b, double[] ideal)
        {
            foreach (int j in b.Cols)
                foreach (int i in b.Rows)
                    if (i != j && b.X(i, j) > 0)
                    {
                        ideal[i * b.N + j] = 1;
                        break;
                    }
        }

        /// <summary>
        /// Finds the column with the maximum value in row i (strictly larger than 'start'), excluding the diagonal.
        /// Returns -1 if none.
        /// </summary>
        protected static int RowMax(in BlockRegion b, int i, double start, out double maxVal)
        {
            maxVal = start;
            int maxCol = -1;
            foreach (int j in b.Cols)
                if (i != j && b.X(i, j) > maxVal)
                {
                    maxVal = b.X(i, j);
                    maxCol = j;
                }
            return maxCol;
        }

        /// <summary>
        /// Finds the row with the maximum value in column j (strictly larger than 'start'), excluding the diagonal.
        /// Returns -1 if none.
        /// </summary>
        protected static int ColMax(in BlockRegion b, int j, double start, out double maxVal)
        {
            maxVal = start;
            int maxRow = -1;
            foreach (int i in b.Rows)
                if (i != j && b.X(i, j) > maxVal)
                {
                    maxVal = b.X(i, j);
                    maxRow = i;
                }
            return maxRow;
        }
    }

    /// <summary>
    /// The goodness-of-fit methods available for direct blockmodeling.
    /// </summary>
    public enum GofMethod
    {
        /// <summary>Hamming distances, i.e. the number of inconsistencies (minimized).</summary>
        Hamming,
        /// <summary>Weighted correlation between observed and ideal values, Nordlund (2020) (maximized).</summary>
        Nordlund
    }

    /// <summary>
    /// Creates ideal blocks from their names, e.g. "com", "den(0.25)".
    /// </summary>
    public static class BlockFactory
    {
        private static readonly Dictionary<string, Func<IdealBlock>> Factories = new()
        {
            { "dnc", () => new DncBlock() },
            { "nul", () => new NulBlock() },
            { "com", () => new ComBlock() },
            { "reg", () => new RegBlock() },
            { "rre", () => new RreBlock() },
            { "cre", () => new CreBlock() },
            { "rfn", () => new RfnBlock() },
            { "cfn", () => new CfnBlock() },
            { "denuci", () => new DenuciBlock() },
            { "den", () => new DenBlock() },
            { "denmin", () => new DenminBlock() },
            { "pco", () => new PcoBlock() },
            { "pcdd", () => new PcddBlock() },
            { "cpdd", () => new CpddBlock() },
        };

        private static readonly Regex StripRegex = new(@"[^a-zA-Z0-9\(\)\.]", RegexOptions.Compiled);
        private static readonly Regex BlockRegex = new(@"^([\w]+)(\(([^\)]+)\))?", RegexOptions.Compiled);

        /// <summary>
        /// Names of all available ideal blocks.
        /// </summary>
        public static IEnumerable<string> BlockNames => Factories.Keys;

        /// <summary>
        /// Creates an ideal block from a block string such as "reg" or "pco(0.75)".
        /// Returns null if the block is not recognized.
        /// </summary>
        public static IdealBlock? Create(string blockString)
        {
            blockString = StripRegex.Replace(blockString, "");
            Match match = BlockRegex.Match(blockString);
            if (!match.Success)
                return null;
            if (!Factories.TryGetValue(match.Groups[1].Value, out var factory))
                return null;
            IdealBlock block = factory();
            if (match.Groups[3].Value.Length > 0 && Fmt.TryParseDouble(match.Groups[3].Value, out double v))
                block.SetParameter(v);
            return block;
        }

        /// <summary>
        /// Creates a list of ideal blocks from an array of block strings, skipping unrecognized and duplicate ones.
        /// </summary>
        public static List<IdealBlock> CreateList(IEnumerable<string> blockStrings)
        {
            List<IdealBlock> blocks = [];
            HashSet<string> already = [];
            foreach (string blockString in blockStrings)
            {
                if (already.Contains(blockString))
                    continue;
                IdealBlock? block = Create(blockString);
                if (block != null)
                {
                    blocks.Add(block);
                    already.Add(blockString);
                }
            }
            return blocks;
        }
    }
}
