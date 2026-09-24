// Parts of Socnet.se 1.4 (Functions.cs and Blockmodeling.cs) needed by the legacy ideal blocks,
// copied unchanged. The Legacy folder is used as a test oracle for Socnet.se 2.0.
using SocnetLegacy.DataLibrary;

namespace SocnetLegacy
{
    public static class Functions
    {
        internal static double minMaxRange(double v, int min, int max)
        {
            if (v > max)
                return max;
            else if (v < min)
                return min;
            return v;
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
    }

    public struct Triple
    {
        public double x, y, w;

        public Triple(double x, double y, double w)
        {
            this.x = x;
            this.y = y;
            this.w = w;
        }
    }
}
