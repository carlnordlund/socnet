namespace Socnet.DataLibrary
{
    public class Matrix : DataStructure
    {
        public double[,] data;
        public Actorset actorset;


        internal override string GetSize()
        {
            int nbrActors = actorset.Count;
            return nbrActors + ";" + nbrActors;
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Actorset:" + actorset.Name);
            string line = "";
            foreach (Actor colActor in actorset.actors)
                line += "\t" + colActor.Name;
            content.Add(line);
            foreach (Actor rowActor in actorset.actors)
            {
                line = rowActor.Name;
                foreach (Actor colActor in actorset.actors)
                    line += "\t" + Get(rowActor, colActor);
                content.Add(line.TrimEnd('\t'));
            }
        }

        public Matrix(Actorset actorset, string name, string cellformat)
        {
            this.actorset = actorset;
            this.data = new double[actorset.Count, actorset.Count];
            this.Name = name;
            this.Cellformat = cellformat;
        }

        public double Get(Actor rowActor, Actor colActor)
        {
            return data[rowActor.index, colActor.index];
        }

        public void Set(Actor rowActor, Actor colActor, double value)
        {
            data[rowActor.index, colActor.index] = value;
        }

        internal void installData(string[] labels, double[,] data)
        {
            // Given array with string labels, and a data.
            // Rather than just directly match array indices and data indices, use the stored index values
            // for each Actor in the current Actorset, using the labels to first access the Actors and then their indices
            // That is where I will store it in the data object of the Matrix
            // So take from original position in in:data, but store in the Actor indexed positions in the Matrix.data
            // Build up a lookup: from in:data,labels index to actual Actors
            Dictionary<int, Actor> indexToActor = new Dictionary<int, Actor>();
            for (int i = 0; i < labels.Length; i++)
                indexToActor.Add(i, actorset.labelToActor[labels[i]]);
            for (int r = 0; r < labels.Length; r++)
                for (int c = 0; c < labels.Length; c++)
                    Set(indexToActor[r], indexToActor[c], data[r, c]);
        }
    }
}
