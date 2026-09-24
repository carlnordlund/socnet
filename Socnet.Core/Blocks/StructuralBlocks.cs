namespace Socnet.Core.Blocks
{
    /// <summary>
    /// The 'do not care' ideal block: never penalized, and ignored in the correlation measure.
    /// </summary>
    public sealed class DncBlock : IdealBlock
    {
        public override string Name => "dnc";
        public override int IsoIndex => 0;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new DncBlock();
        public override bool HasFastHamming => true;
        public override bool HasFastNordlund(bool simpleValues) => true;

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = 0;
            return true;
        }

        public override bool TryNordlund(in BlockStats s, ref CorrSums sums) => true;

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, double.NaN);

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, double.NaN);
    }

    /// <summary>
    /// The null ideal block: no ties.
    /// </summary>
    public sealed class NulBlock : IdealBlock
    {
        public override string Name => "nul";
        public override int IsoIndex => 1;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new NulBlock();
        public override bool HasFastHamming => true;
        public override bool HasFastNordlund(bool simpleValues) => true;

        public override double Hamming(in BlockRegion b) => CountPositive(b);

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = s.Pos;
            return true;
        }

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        sink.Add(b.X(i, j), 0, 1);
        }

        public override bool TryNordlund(in BlockStats s, ref CorrSums sums)
        {
            sums.W += s.NbrCells;
            sums.X += s.Sum;
            sums.XX += s.SumSq;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, 0);

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, 0);
    }

    /// <summary>
    /// The complete ideal block: all possible ties present.
    /// </summary>
    public sealed class ComBlock : IdealBlock
    {
        public override string Name => "com";
        public override int IsoIndex => 2;
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new ComBlock();
        public override bool HasFastHamming => true;
        public override bool HasFastNordlund(bool simpleValues) => true;

        public override double Hamming(in BlockRegion b)
        {
            int count = 0;
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j && b.X(i, j) < 1)
                        count++;
            return count;
        }

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = s.NbrCells - s.Ge1;
            return true;
        }

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        sink.Add(b.X(i, j), 1, 1);
        }

        public override bool TryNordlund(in BlockStats s, ref CorrSums sums)
        {
            int cells = s.NbrCells;
            sums.W += cells;
            sums.X += s.Sum;
            sums.XX += s.SumSq;
            sums.Y += cells;
            sums.YY += cells;
            sums.XY += s.Sum;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, 1);

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, 1);
    }
}
