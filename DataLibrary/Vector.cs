namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for Vector objects
    /// </summary>
    public class Vector : DataStructure
    {
        public double[] data;
        public Actorset actorset;

        public Vector(Actorset actorset, string name, string cellformat)
        {
            this.actorset = actorset;
            this.data = new double[actorset.Count];
            this.Name = name;
            this.Cellformat = cellformat;
        }

        public double Get(Actor actor)
        {
            return data[actor.index];
        }

        public void Set(Actor actor, double value)
        {
            data[actor.index] = value;
        }

        internal override string GetSize()
        {
            int nbrActors = actorset.Count;
            return nbrActors.ToString();
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Actorset:" + actorset.Name);
            content.Add("actor" + "\t" + "value");
            foreach (Actor actor in actorset.actors)
                content.Add(actor.Name + "\t" + Get(actor));
        }

        internal string GetValueString()
        {
            string line = "[";
            foreach (Actor actor in actorset.actors)
            {
                line += Get(actor).ToString(Cellformat) + ";";
            }
            return line.TrimEnd(';') + "]";
        }
    }
}
