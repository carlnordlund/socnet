namespace Socnet.Core.Blocks
{
    /// <summary>
    /// Receives (x, y, w) triplets - observed value, ideal value and weight - that ideal blocks produce
    /// for the weighted correlation ('nordlund') goodness-of-fit measure.
    /// </summary>
    public abstract class TripleSink
    {
        public abstract void Add(double x, double y, double w);
    }

    /// <summary>
    /// The weighted sums needed to compute a weighted correlation coefficient.
    /// Sums are additive, so the sums of a blockmodel are the sums of its blocks.
    /// </summary>
    public struct CorrSums
    {
        public double W, X, Y, XX, YY, XY;

        public void Add(double x, double y, double w)
        {
            W += w;
            X += w * x;
            Y += w * y;
            XX += w * x * x;
            YY += w * y * y;
            XY += w * x * y;
        }

        public void Add(in CorrSums other)
        {
            W += other.W;
            X += other.X;
            Y += other.Y;
            XX += other.XX;
            YY += other.YY;
            XY += other.XY;
        }

        /// <summary>
        /// Weighted correlation coefficient. Returns -1 if either variable has no variance,
        /// and NaN if the total weight is zero.
        /// </summary>
        public readonly double Correlation()
        {
            if (W == 0 || double.IsNaN(W))
                return double.NaN;
            double mx = X / W, my = Y / W;
            double sx = XX / W - mx * mx;
            double sy = YY / W - my * my;
            double sxy = XY / W - mx * my;
            // Guard against round-off producing tiny (or negative) variances where the true variance is zero
            if (sx <= 1e-12 * Math.Max(XX / W, 1e-300))
                sx = 0;
            if (sy <= 1e-12 * Math.Max(YY / W, 1e-300))
                sy = 0;
            double denom = Math.Sqrt(sx * sy);
            if (denom == 0)
                return -1;
            return sxy / denom;
        }
    }

    /// <summary>
    /// Triple sink that accumulates weighted sums (used in searches).
    /// </summary>
    public sealed class CorrAccumulator : TripleSink
    {
        public CorrSums Sums;

        public void Reset() => Sums = default;

        public override void Add(double x, double y, double w) => Sums.Add(x, y, w);
    }

    /// <summary>
    /// Triple sink that stores all triplets, and computes the correlation in two passes exactly as
    /// Socnet.se 1.4 did (used when reporting the goodness-of-fit of a final blockmodel).
    /// </summary>
    public sealed class TripleList : TripleSink
    {
        private readonly List<(double x, double y, double w)> _triples = [];

        public int Count => _triples.Count;

        public override void Add(double x, double y, double w) => _triples.Add((x, y, w));

        public double Correlation()
        {
            double mx = 0, my = 0, sx = 0, sy = 0, sxy = 0, wSum = 0;
            foreach (var (x, y, w) in _triples)
            {
                mx += w * x;
                my += w * y;
                wSum += w;
            }
            mx /= wSum;
            my /= wSum;
            foreach (var (x, y, w) in _triples)
            {
                sx += w * (x - mx) * (x - mx);
                sy += w * (y - my) * (y - my);
                sxy += w * (x - mx) * (y - my);
            }
            sx /= wSum;
            sy /= wSum;
            sxy /= wSum;
            double denom = Math.Sqrt(sx * sy);
            if (denom == 0)
                return -1;
            return sxy / denom;
        }
    }
}
