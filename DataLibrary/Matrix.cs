namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for Matrix object
    /// </summary>
    public class Matrix : DataStructure
    {
        // The data in this matrix and the Actorset this Matrix is associated with
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

        /// <summary>
        /// Constructor for Matrix object
        /// </summary>
        /// <param name="actorset">The Actorset the Matrix is associated to</param>
        /// <param name="name">The name of the Matrix</param>
        /// <param name="cellformat">The number format of cell values in the data 2d array</param>
        public Matrix(Actorset actorset, string name, string cellformat)
        {
            this.actorset = actorset;
            this.data = new double[actorset.Count, actorset.Count];
            this.Name = name;
            this.Cellformat = cellformat;
        }

        /// <summary>
        /// Method to get a cell value
        /// </summary>
        /// <param name="rowActor">Row Actor</param>
        /// <param name="colActor">Column Actor</param>
        /// <returns>The data value in row and column</returns>
        public double Get(Actor rowActor, Actor colActor)
        {
            return data[rowActor.index, colActor.index];
        }

        /// <summary>
        /// Method to set a cell value
        /// </summary>
        /// <param name="rowActor">Row actor</param>
        /// <param name="colActor">Column actor</param>
        /// <param name="value">Value to set</param>
        public void Set(Actor rowActor, Actor colActor, double value)
        {
            data[rowActor.index, colActor.index] = value;
        }

        /// <summary>
        /// Support method for quickly installing content to a Matrix object
        /// Used by SocnetIO when reading from files
        /// </summary>
        /// <param name="labels">Array of actor labels</param>
        /// <param name="data">2d array of values</param>
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
