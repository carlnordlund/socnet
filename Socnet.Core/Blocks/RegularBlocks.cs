namespace Socnet.Core.Blocks
{
    /// <summary>
    /// The regular ideal block: at least one tie in each row and in each column.
    /// </summary>
    public sealed class RegBlock : IdealBlock
    {
        public override string Name => "reg";
        public override int IsoIndex => 3;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new RegBlock();

        public override double Hamming(in BlockRegion b)
        {
            double nr = b.Nr, nc = b.Nc;
            double pr = CountNonNullRows(b), pc = CountNonNullCols(b);
            return (nc - pc) * nr + (nr - pr) * nc;
        }

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = (double)(s.Nc - s.Pc) * s.Nr + (double)(s.Nr - s.Pr) * s.Nc;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal)
        {
            MarkFirstTiePerRow(b, ideal);
            MarkFirstTiePerCol(b, ideal);
        }

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            double w = (double)(b.Nr * b.Nc - (b.Diagonal ? b.Nr : 0)) / (b.Nr + b.Nc);
            foreach (int i in b.Rows)
            {
                RowMax(b, i, double.NegativeInfinity, out double maxVal);
                sink.Add(maxVal, 1, w);
            }
            foreach (int j in b.Cols)
            {
                ColMax(b, j, double.NegativeInfinity, out double maxVal);
                sink.Add(maxVal, 1, w);
            }
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int i in b.Rows)
            {
                int j = RowMax(b, i, double.NegativeInfinity, out _);
                if (j >= 0)
                    ideal[i * b.N + j] = 1;
            }
            foreach (int j in b.Cols)
            {
                int i = ColMax(b, j, double.NegativeInfinity, out _);
                if (i >= 0)
                    ideal[i * b.N + j] = 1;
            }
        }
    }

    /// <summary>
    /// The row-regular ideal block: at least one tie in each row.
    /// </summary>
    public sealed class RreBlock : IdealBlock
    {
        public override string Name => "rre";
        public override int IsoIndex => 4;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new RreBlock();

        public override double Hamming(in BlockRegion b) => (double)(b.Nr - CountNonNullRows(b)) * b.Nc;

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = (double)(s.Nr - s.Pr) * s.Nc;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => MarkFirstTiePerRow(b, ideal);

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            double w = b.Nc - (b.Diagonal ? 1 : 0);
            foreach (int i in b.Rows)
            {
                RowMax(b, i, double.NegativeInfinity, out double maxVal);
                sink.Add(maxVal, 1, w);
            }
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int i in b.Rows)
            {
                int j = RowMax(b, i, double.NegativeInfinity, out _);
                if (j >= 0)
                    ideal[i * b.N + j] = 1;
            }
        }
    }

    /// <summary>
    /// The column-regular ideal block: at least one tie in each column.
    /// </summary>
    public sealed class CreBlock : IdealBlock
    {
        public override string Name => "cre";
        public override int IsoIndex => 5;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new CreBlock();

        public override double Hamming(in BlockRegion b) => (double)(b.Nc - CountNonNullCols(b)) * b.Nr;

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = (double)(s.Nc - s.Pc) * s.Nr;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => MarkFirstTiePerCol(b, ideal);

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            double w = b.Nr - (b.Diagonal ? 1 : 0);
            foreach (int j in b.Cols)
            {
                ColMax(b, j, double.NegativeInfinity, out double maxVal);
                sink.Add(maxVal, 1, w);
            }
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int j in b.Cols)
            {
                int i = ColMax(b, j, double.NegativeInfinity, out _);
                if (i >= 0)
                    ideal[i * b.N + j] = 1;
            }
        }
    }

    /// <summary>
    /// The row-functional ideal block: exactly one tie in each row.
    /// </summary>
    public sealed class RfnBlock : IdealBlock
    {
        public override string Name => "rfn";
        public override int IsoIndex => 6;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new RfnBlock();

        public override double Hamming(in BlockRegion b)
        {
            double pr = CountNonNullRows(b);
            return CountPositive(b) - pr + (b.Nr - pr) * b.Nc;
        }

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = s.Pos - s.Pr + (double)(s.Nr - s.Pr) * s.Nc;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => MarkFirstTiePerRow(b, ideal);

        public override void Nordlund(in BlockRegion b, TripleSink sink) => AddRowFunctional(b, sink, 1);

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal) => WriteRowFunctionalIdeal(b, ideal);

        /// <summary>
        /// Row-functional triplets with the given weight. For each row, the strongest tie is ideally 1 and all
        /// other cells of the row (including the diagonal cell, as in Socnet.se 1.4) ideally 0. Rows without
        /// ties contribute a single (0,1) triplet weighted by the row length.
        /// </summary>
        internal static void AddRowFunctional(in BlockRegion b, TripleSink sink, double weight)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int i in b.Rows)
            {
                int maxCol = RowMax(b, i, 0, out _);
                if (maxCol < 0)
                    sink.Add(0, 1, weight * (b.Nc - (b.Diagonal ? 1 : 0)));
                else
                    foreach (int j in b.Cols)
                        sink.Add(b.X(i, j), j == maxCol ? 1 : 0, weight);
            }
        }

        internal static void WriteRowFunctionalIdeal(in BlockRegion b, double[] ideal)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int i in b.Rows)
            {
                int maxCol = RowMax(b, i, 0, out _);
                if (maxCol >= 0)
                    ideal[i * b.N + maxCol] = 1;
            }
        }
    }

    /// <summary>
    /// The column-functional ideal block: exactly one tie in each column.
    /// </summary>
    public sealed class CfnBlock : IdealBlock
    {
        public override string Name => "cfn";
        public override int IsoIndex => 7;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new CfnBlock();

        public override double Hamming(in BlockRegion b)
        {
            double pc = CountNonNullCols(b);
            return CountPositive(b) - pc + (b.Nc - pc) * b.Nr;
        }

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = s.Pos - s.Pc + (double)(s.Nc - s.Pc) * s.Nr;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => MarkFirstTiePerCol(b, ideal);

        public override void Nordlund(in BlockRegion b, TripleSink sink) => AddColFunctional(b, sink, 1);

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal) => WriteColFunctionalIdeal(b, ideal);

        /// <summary>
        /// Column-functional triplets with the given weight (see <see cref="RfnBlock.AddRowFunctional"/>).
        /// </summary>
        internal static void AddColFunctional(in BlockRegion b, TripleSink sink, double weight)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int j in b.Cols)
            {
                int maxRow = ColMax(b, j, 0, out _);
                if (maxRow < 0)
                    sink.Add(0, 1, weight * (b.Nr - (b.Diagonal ? 1 : 0)));
                else
                    foreach (int i in b.Rows)
                        sink.Add(b.X(i, j), i == maxRow ? 1 : 0, weight);
            }
        }

        internal static void WriteColFunctionalIdeal(in BlockRegion b, double[] ideal)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            foreach (int j in b.Cols)
            {
                int maxRow = ColMax(b, j, 0, out _);
                if (maxRow >= 0)
                    ideal[maxRow * b.N + j] = 1;
            }
        }
    }
}
