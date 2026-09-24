using Socnet.Core.Utilities;

namespace Socnet.Core.Model
{
    /// <summary>
    /// A vector of values, one per actor in an <see cref="Actorset"/>.
    /// </summary>
    public sealed class Vector : DataStructure
    {
        public Vector(Actorset actorset, string name)
        {
            Actorset = actorset;
            Data = new double[actorset.Count];
            Name = name;
        }

        public Actorset Actorset { get; }
        public double[] Data { get; }

        public override string Size => Actorset.Count.ToString();

        public override void GetContent(List<string> content)
        {
            content.Add(":Actorset:" + Actorset.Name);
            content.Add(":actor\tvalue");
            for (int i = 0; i < Data.Length; i++)
                content.Add($":{Actorset.Label(i)}\t{Fmt.D(Data[i])}");
        }
    }
}
