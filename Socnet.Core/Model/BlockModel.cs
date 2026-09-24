using Socnet.Core.Blocks;
using Socnet.Core.Processing;
using Socnet.Core.Utilities;
using System.Text;

namespace Socnet.Core.Model
{
    /// <summary>
    /// A blockmodel: a network, a (single-blocked) blockimage and a partition, with the resulting
    /// goodness-of-fit and ideal matrix.
    /// </summary>
    public sealed class BlockModel : DataStructure
    {
        // The name the blockmodel had when created: used for naming extracted structures (as in Socnet.se 1.4)
        private readonly string _baseName;
        private Matrix? _bmMatrix, _bmIdealMatrix;

        /// <summary>
        /// Creates a blockmodel.
        /// </summary>
        /// <param name="name">Name of the blockmodel.</param>
        /// <param name="matrix">The network.</param>
        /// <param name="blockimage">The single-blocked blockimage.</param>
        /// <param name="partition">The partition of the network actors.</param>
        /// <param name="gof">The goodness-of-fit (rounded to 4 decimals when stored).</param>
        /// <param name="method">The goodness-of-fit method.</param>
        /// <param name="ideal">The ideal values, row-major in the actor order of the network.</param>
        public BlockModel(string name, Matrix matrix, BlockImage blockimage, Partition partition, double gof, GofMethod method, double[] ideal)
        {
            Name = name;
            _baseName = name;
            Matrix = matrix;
            BlockImage = blockimage;
            Partition = partition;
            Gof = Math.Round(gof, 4);
            Method = method;
            Ideal = ideal;
        }

        public Matrix Matrix { get; }
        public BlockImage BlockImage { get; }
        public Partition Partition { get; }
        public double Gof { get; }
        public GofMethod Method { get; }
        public double[] Ideal { get; }

        /// <summary>
        /// The name of the goodness-of-fit method ('hamming' or 'nordlund').
        /// </summary>
        public string MethodName => Method == GofMethod.Hamming ? "hamming" : "nordlund";

        /// <summary>
        /// The goodness-of-fit and method as text, e.g. "20 (hamming)".
        /// </summary>
        public string GofString => $"{Fmt.D(Gof)} ({MethodName})";

        /// <summary>
        /// The network matrix with actors sorted by cluster (created on first use).
        /// </summary>
        public Matrix BmMatrix
        {
            get
            {
                CreateBlockmodelMatrices();
                return _bmMatrix!;
            }
        }

        /// <summary>
        /// The ideal matrix with actors sorted by cluster (created on first use).
        /// </summary>
        public Matrix BmIdealMatrix
        {
            get
            {
                CreateBlockmodelMatrices();
                return _bmIdealMatrix!;
            }
        }

        private void CreateBlockmodelMatrices()
        {
            if (_bmMatrix != null)
                return;
            int[][] members = Partition.AllMembers();
            List<string> labels = [];
            List<int> order = [];
            for (int c = 0; c < members.Length; c++)
                foreach (int actor in members[c])
                {
                    labels.Add(c + "_" + Matrix.Actorset.Label(actor));
                    order.Add(actor);
                }
            Actorset bmActorset = Actorset.Create(_baseName + "_actors", labels)!;
            _bmMatrix = new Matrix(bmActorset, _baseName + "_matrix");
            _bmIdealMatrix = new Matrix(bmActorset, _baseName + "_idealmatrix");
            int n = order.Count;
            for (int r = 0; r < n; r++)
                for (int c = 0; c < n; c++)
                {
                    _bmMatrix[r, c] = Matrix[order[r], order[c]];
                    _bmIdealMatrix[r, c] = Ideal[order[r] * Matrix.N + order[c]];
                }
        }

        public override string Size => "[Blockmodel size]";

        public override void GetContent(List<string> content)
        {
            content.Add("Matrix:" + Matrix.Name);
            content.Add("Blockimage:" + BlockImage.Name);
            content.Add($"Partition:{Partition.Name} ({Partition.GetPartString()})");
            content.Add("GoF:" + GofString);
        }

        /// <summary>
        /// Returns the lines displaying the blockimage of this blockmodel.
        /// </summary>
        public List<string> DisplayBlockimage()
        {
            List<string> lines = [];
            int k = BlockImage.NbrPositions;
            StringBuilder sb = new();
            for (int c = 0; c < k; c++)
                sb.Append('\t').Append(BlockImage.PositionNames[c]);
            lines.Add(":" + sb);
            for (int r = 0; r < k; r++)
            {
                sb.Clear();
                sb.Append(BlockImage.PositionNames[r]);
                for (int c = 0; c < k; c++)
                    sb.Append('\t').Append(BlockImage.GetBlock(r, c).ToString());
                lines.Add(":" + sb);
            }
            return lines;
        }

        /// <summary>
        /// Returns the lines displaying the network, sorted by cluster.
        /// </summary>
        public List<string> DisplayBlockmodelMatrix() => DisplayBlockmodel(Matrix.Data, 'X', ' ');

        /// <summary>
        /// Returns the lines displaying the ideal matrix, sorted by cluster.
        /// </summary>
        public List<string> DisplayIdealMatrix() => DisplayBlockmodel(Ideal, '1', '0');

        private List<string> DisplayBlockmodel(double[] data, char tieChar, char noTieChar)
        {
            List<string> lines = [];
            int n = Matrix.N;
            double max = MatrixFunctions.GetMaxValue(data, n, false);
            double threshold = (max == 1) ? 0.5 : MatrixFunctions.GetDisplayMedian(data, n);
            int[][] members = Partition.AllMembers();
            string separator = ":+" + new string('-', n + members.Length - 1) + "+";
            lines.Add(separator);
            StringBuilder sb = new();
            for (int r = 0; r < members.Length; r++)
            {
                foreach (int rowActor in members[r])
                {
                    sb.Clear();
                    sb.Append('|');
                    for (int c = 0; c < members.Length; c++)
                    {
                        foreach (int colActor in members[c])
                        {
                            double val = data[rowActor * n + colActor];
                            if (rowActor != colActor)
                                sb.Append(val > threshold ? tieChar : double.IsNaN(val) ? '.' : noTieChar);
                            else
                                sb.Append('\\');
                        }
                        sb.Append('|');
                    }
                    sb.Append('\t').Append(r).Append('_').Append(Matrix.Actorset.Label(rowActor));
                    lines.Add(":" + sb);
                }
                lines.Add(separator);
            }
            return lines;
        }
    }
}
