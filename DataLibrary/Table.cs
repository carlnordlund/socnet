namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for Table objects
    /// </summary>
    public class Table : DataStructure
    {
        public double[,] data;
        public Actorset rowActorset, colActorset;

        internal override string GetSize()
        {
            return rowActorset.Count + ";" + colActorset.Count;
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Actorsets: Rows:" + rowActorset.Name + " Cols:" + colActorset.Name);
            string line = "";
            foreach (Actor colActor in colActorset.actors)
                line += "\t" + colActor.Name;
            content.Add(line);
            foreach (Actor rowActor in rowActorset.actors)
            {
                line = rowActor.Name;
                foreach (Actor colActor in colActorset.actors)
                    line += "\t" + Get(rowActor, colActor);
                content.Add(line.TrimEnd('\t'));
            }
        }

        public Table(Actorset rowActorset, Actorset colActorset, string name, string cellformat)
        {
            this.rowActorset = rowActorset;
            this.colActorset = colActorset;
            this.data = new double[rowActorset.Count, colActorset.Count];
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

        internal void installData(string[] rowLabels, string[] colLabels, double[,] data)
        {
            Dictionary<int, Actor> rowIndexToActor = new Dictionary<int, Actor>();
            for (int i = 0; i < rowLabels.Length; i++)
                rowIndexToActor.Add(i, rowActorset.labelToActor[rowLabels[i]]);
            Dictionary<int, Actor> colIndexToActor = new Dictionary<int, Actor>();
            for (int i = 0; i < colLabels.Length; i++)
                colIndexToActor.Add(i, colActorset.labelToActor[colLabels[i]]);

            for (int r = 0; r < rowLabels.Length; r++)
                for (int c = 0; c < colLabels.Length; c++)
                    Set(rowIndexToActor[r], colIndexToActor[c], data[r, c]);
        }
    }
}
