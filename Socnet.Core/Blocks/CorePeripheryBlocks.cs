namespace Socnet.Core.Blocks
{
    /// <summary>
    /// The p-core (proportional core) ideal block pco(p): in each row and each column, at least a share p
    /// of the cells are ties. Estévez and Nordlund (2025).
    /// </summary>
    public sealed class PcoBlock : IdealBlock
    {
        public PcoBlock() => Parameter = 0.5;

        public override string Name => "pco";
        public override int IsoIndex => 11;
        public override bool HasParameter => true;
        public override void SetParameter(double value) => Parameter = Clamp01(value);
        public override bool SupportsHamming => false;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new PcoBlock { Parameter = Parameter };

        private (int kr, int kc) GetK(in BlockRegion b)
        {
            int diag = b.Diagonal ? 1 : 0;
            return ((int)Math.Ceiling((b.Nc - diag) * Parameter), (int)Math.Ceiling((b.Nr - diag) * Parameter));
        }

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            // In each row (and column): the k largest values are ideally 1, the others are ideally what they are
            var (kr, kc) = GetK(b);
            Span<double> values = b.Scratch.AsSpan(0, Math.Max(b.Nr, b.Nc));
            foreach (int i in b.Rows)
            {
                int count = 0;
                foreach (int j in b.Cols)
                    if (i != j)
                        values[count++] = b.X(i, j);
                AddSorted(values[..count], kr, sink);
            }
            foreach (int j in b.Cols)
            {
                int count = 0;
                foreach (int i in b.Rows)
                    if (i != j)
                        values[count++] = b.X(i, j);
                AddSorted(values[..count], kc, sink);
            }
        }

        private static void AddSorted(Span<double> values, int k, TripleSink sink)
        {
            values.Sort();
            int n = values.Length;
            for (int m = 0; m < k && m < n; m++)
                sink.Add(values[n - 1 - m], 1, 0.5);
            for (int m = k; m < n; m++)
                sink.Add(values[n - 1 - m], values[n - 1 - m], 0.5);
        }

        public override void WriteIdealNordlund(in BlockRegion b, double[] ideal)
        {
            var (kr, kc) = GetK(b);
            foreach (int i in b.Rows)
            {
                List<Cell> cells = [];
                foreach (int j in b.Cols)
                    if (i != j)
                        cells.Add(new Cell(i, j, b.X(i, j)));
                cells.Sort((s1, s2) => s2.Value.CompareTo(s1.Value));
                for (int m = 0; m < kr && m < cells.Count; m++)
                    ideal[cells[m].Row * b.N + cells[m].Col] = 1;
            }
            foreach (int j in b.Cols)
            {
                List<Cell> cells = [];
                foreach (int i in b.Rows)
                    if (i != j)
                        cells.Add(new Cell(i, j, b.X(i, j)));
                cells.Sort((s1, s2) => s2.Value.CompareTo(s1.Value));
                for (int m = 0; m < kc && m < cells.Count; m++)
                    ideal[cells[m].Row * b.N + cells[m].Col] = 1;
            }
        }
    }

    /// <summary>
    /// The power-relational ideal block for peripheral dependency and core dominance (periphery-to-core):
    /// row-functional and column-regular, each with half weight. Nordlund (2018).
    /// </summary>
    public sealed class PcddBlock : IdealBlock
    {
        public override string Name => "pcdd";
        public override int IsoIndex => 12;
        public override bool SupportsHamming => false;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new PcddBlock();

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            RfnBlock.AddRowFunctional(b, sink, 0.5);
            double w = 0.5 * (b.Nr - (b.Diagonal ? 1 : 0));
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
            RfnBlock.WriteRowFunctionalIdeal(b, ideal);
            foreach (int j in b.Cols)
            {
                int i = ColMax(b, j, double.NegativeInfinity, out _);
                if (i >= 0)
                    ideal[i * b.N + j] = 1;
            }
        }
    }

    /// <summary>
    /// The power-relational ideal block for core dominance and peripheral dependency (core-to-periphery):
    /// column-functional and row-regular, each with half weight. Nordlund (2018).
    /// </summary>
    public sealed class CpddBlock : IdealBlock
    {
        public override string Name => "cpdd";
        public override int IsoIndex => 13;
        public override bool SupportsHamming => false;
        public override bool SupportsNordlund => true;
        public override IdealBlock Clone() => new CpddBlock();

        public override void Nordlund(in BlockRegion b, TripleSink sink)
        {
            if (b.Diagonal && b.Nr == 1)
                return;
            CfnBlock.AddColFunctional(b, sink, 0.5);
            double w = 0.5 * (b.Nc - (b.Diagonal ? 1 : 0));
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
            CfnBlock.WriteColFunctionalIdeal(b, ideal);
            foreach (int i in b.Rows)
            {
                int j = RowMax(b, i, double.NegativeInfinity, out _);
                if (j >= 0)
                    ideal[i * b.N + j] = 1;
            }
        }
    }
}
