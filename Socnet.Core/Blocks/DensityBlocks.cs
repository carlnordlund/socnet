namespace Socnet.Core.Blocks
{
    /// <summary>
    /// A cell of a block, used when cells have to be sorted by value.
    /// </summary>
    internal struct Cell(int row, int col, double value)
    {
        public int Row = row, Col = col;
        public double Value = value;

        /// <summary>
        /// Returns the cells of a block in row-major order (excluding the diagonal).
        /// </summary>
        public static List<Cell> FromBlock(in BlockRegion b)
        {
            List<Cell> cells = new(b.NbrCells);
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        cells.Add(new Cell(i, j, b.X(i, j)));
            return cells;
        }
    }

    /// <summary>
    /// The density ideal block den(d): a share d of the cells are ties.
    /// </summary>
    public sealed class DenBlock : IdealBlock
    {
        public DenBlock() => Parameter = 0.5;

        public override string Name => "den";
        public override int IsoIndex => 9;
        public override bool HasParameter => true;
        public override void SetParameter(double value) => Parameter = Clamp01(value);
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new DenBlock { Parameter = Parameter };

        public override double Hamming(in BlockRegion b) => Math.Abs(CountPositive(b) - (int)Math.Round((double)b.NbrCells * Parameter));

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            penalty = Math.Abs(s.Pos - (int)Math.Round((double)s.NbrCells * Parameter));
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal)
        {
            int i1 = (int)Math.Round((double)b.NbrCells * Parameter);
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j && b.X(i, j) > 0 && i1 > 0)
                    {
                        ideal[i * b.N + j] = 1;
                        i1--;
                    }
        }

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            // The i1 largest values are ideally 1, the others ideally 0
            int nbrCells = b.NbrCells;
            int i1 = (int)Math.Round((double)nbrCells * Parameter);
            int i0 = nbrCells - i1;
            Span<double> values = b.Scratch.AsSpan(0, nbrCells);
            int k = 0;
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        values[k++] = b.X(i, j);
            values.Sort();
            for (int i = 0; i < i0; i++)
                sink.Add(values[i], 0, 1);
            for (int i = i0; i < nbrCells; i++)
                sink.Add(values[i], 1, 1);
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal)
        {
            List<Cell> cells = Cell.FromBlock(b);
            int i1 = (int)Math.Round((double)cells.Count * Parameter);
            int i0 = cells.Count - i1;
            cells.Sort((s1, s2) => s1.Value.CompareTo(s2.Value));
            for (int i = 0; i < cells.Count; i++)
                ideal[cells[i].Row * b.N + cells[i].Col] = i < i0 ? 0 : 1;
        }
    }

    /// <summary>
    /// The minimum density ideal block denmin(d): at least a share d of the cells are ties.
    /// </summary>
    public sealed class DenminBlock : IdealBlock
    {
        public DenminBlock() => Parameter = 0.5;

        public override string Name => "denmin";
        public override int IsoIndex => 10;
        public override bool HasParameter => true;
        public override void SetParameter(double value) => Parameter = Clamp01(value);
        public override bool SupportsHamming => true;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new DenminBlock { Parameter = Parameter };

        public override double Hamming(in BlockRegion b)
        {
            int i1 = (int)Math.Round((double)b.NbrCells * Parameter);
            int sum = CountPositive(b);
            return sum < i1 ? i1 - sum : 0;
        }

        public override bool TryHamming(in BlockStats s, out double penalty)
        {
            int i1 = (int)Math.Round((double)s.NbrCells * Parameter);
            penalty = s.Pos < i1 ? i1 - s.Pos : 0;
            return true;
        }

        public override void WriteIdealHamming(in BlockRegion b, double[] ideal)
        {
            int i1 = (int)Math.Round((double)b.NbrCells * Parameter);
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j && b.X(i, j) > 0)
                    {
                        if (i1 > 0)
                        {
                            ideal[i * b.N + j] = 1;
                            i1--;
                        }
                        else
                            ideal[i * b.N + j] = b.X(i, j);
                    }
        }

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            // The i1 largest values are ideally 1, the others are ideally what they are
            int nbrCells = b.NbrCells;
            int i1 = (int)Math.Ceiling((double)nbrCells * Parameter);
            Span<double> values = b.Scratch.AsSpan(0, nbrCells);
            int k = 0;
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        values[k++] = b.X(i, j);
            values.Sort();
            // Sorted ascending: the largest i1 values are at the end
            int i0 = nbrCells - i1;
            for (int i = nbrCells - 1; i >= i0; i--)
                sink.Add(values[i], 1, 1);
            for (int i = i0 - 1; i >= 0; i--)
                sink.Add(values[i], values[i], 1);
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal)
        {
            List<Cell> cells = Cell.FromBlock(b);
            int i1 = (int)Math.Ceiling((double)cells.Count * Parameter);
            cells.Sort((s1, s2) => s2.Value.CompareTo(s1.Value));
            for (int i = 0; i < cells.Count; i++)
                ideal[cells[i].Row * b.N + cells[i].Col] = i < i1 ? 1 : cells[i].Value;
        }
    }

    /// <summary>
    /// The uniform density ideal block denuci(d): all cells ideally have the value d. Only used with the
    /// correlation-based measure, e.g. for Borgatti-Everett style core-periphery models.
    /// </summary>
    public sealed class DenuciBlock : IdealBlock
    {
        public DenuciBlock() => Parameter = 0.5;

        public override string Name => "denuci";
        public override int IsoIndex => 8;
        public override bool HasParameter => true;
        public override void SetParameter(double value) => Parameter = value;
        public override bool SupportsHamming => false;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new DenuciBlock { Parameter = Parameter };

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            foreach (int i in b.Rows)
                foreach (int j in b.Cols)
                    if (i != j)
                        sink.Add(b.X(i, j), Parameter, 1);
        }

        public override bool TryNordlund(in BlockStats s, ref CorrSums sums)
        {
            double d = Parameter;
            int cells = s.NbrCells;
            sums.W += cells;
            sums.X += s.Sum;
            sums.XX += s.SumSq;
            sums.Y += cells * d;
            sums.YY += cells * d * d;
            sums.XY += d * s.Sum;
            return true;
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal) => FillIdeal(b, ideal, Parameter);
    }
}
