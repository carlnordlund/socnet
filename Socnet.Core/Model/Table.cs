using Socnet.Core.Utilities;
using System.Text;

namespace Socnet.Core.Model
{
    /// <summary>
    /// A rectangular (two-mode) table of values, with separate Actorsets for rows and columns.
    /// Tables can be loaded, viewed and dichotomized, but are not used in blockmodeling.
    /// </summary>
    public sealed class Table : DataStructure
    {
        public Table(Actorset rowActorset, Actorset colActorset, string name)
        {
            RowActorset = rowActorset;
            ColActorset = colActorset;
            Data = new double[rowActorset.Count, colActorset.Count];
            Name = name;
        }

        public Actorset RowActorset { get; }
        public Actorset ColActorset { get; }
        public double[,] Data { get; }

        public override string Size => $"{RowActorset.Count};{ColActorset.Count}";

        public override void GetContent(List<string> content)
        {
            content.Add($":Actorsets: Rows:{RowActorset.Name} Cols:{ColActorset.Name}");
            StringBuilder sb = new();
            for (int c = 0; c < ColActorset.Count; c++)
                sb.Append('\t').Append(ColActorset.Label(c));
            content.Add(":" + sb);
            for (int r = 0; r < RowActorset.Count; r++)
            {
                sb.Clear();
                sb.Append(RowActorset.Label(r));
                for (int c = 0; c < ColActorset.Count; c++)
                    sb.Append('\t').Append(Fmt.D(Data[r, c]));
                content.Add(":" + sb.ToString().TrimEnd('\t'));
            }
        }
    }
}
