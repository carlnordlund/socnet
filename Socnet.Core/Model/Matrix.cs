using Socnet.Core.Utilities;
using System.Text;

namespace Socnet.Core.Model
{
    /// <summary>
    /// A square (one-mode) matrix of values for the actors of an <see cref="Actorset"/>,
    /// i.e. a network. Values are stored row-major in a flat array.
    /// </summary>
    public sealed class Matrix : DataStructure
    {
        /// <summary>
        /// Creates a zero-valued matrix for the given Actorset.
        /// </summary>
        public Matrix(Actorset actorset, string name)
        {
            Actorset = actorset;
            N = actorset.Count;
            Data = new double[N * N];
            Name = name;
        }

        /// <summary>
        /// The Actorset of the rows and columns of this matrix.
        /// </summary>
        public Actorset Actorset { get; }

        /// <summary>
        /// The number of actors (rows and columns).
        /// </summary>
        public int N { get; }

        /// <summary>
        /// The cell values, stored row-major: cell (r,c) is at index r*N+c.
        /// </summary>
        public double[] Data { get; }

        /// <summary>
        /// Gets or sets the value of cell (row, col).
        /// </summary>
        public double this[int row, int col]
        {
            get => Data[row * N + col];
            set => Data[row * N + col] = value;
        }

        public override string Size => $"{N};{N}";

        public override void GetContent(List<string> content)
        {
            content.Add(":Actorset:" + Actorset.Name);
            StringBuilder sb = new();
            for (int c = 0; c < N; c++)
                sb.Append('\t').Append(Actorset.Label(c));
            content.Add(":" + sb);
            for (int r = 0; r < N; r++)
            {
                sb.Clear();
                sb.Append(Actorset.Label(r));
                for (int c = 0; c < N; c++)
                    sb.Append('\t').Append(Fmt.D(this[r, c]));
                content.Add(":" + sb.ToString().TrimEnd('\t'));
            }
        }
    }
}
