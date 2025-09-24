namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the core-to-periphery dependency-and-dominance ideal block
    /// </summary>
    public class cpddBlock : _Block
    {
        /// <summary>
        /// Constructor for the core-to-periphery dependency-and-dominance ideal block as used in the power-relational core-periphery metric (Nordlund 2018)
        /// This block is typically only used by the 'coreperi()' function in socnet.se when using the power-relational option
        /// </summary>
        public cpddBlock()
        {
            Name = "cpdd";
            isoIndex = 13;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;

            // Checking col-functional (heavy penalty, half weight)
            int nbrRows = rowCluster.actors.Count;
            Actor? maxActor = null;
            double maxVal;
            foreach (Actor colActor in colCluster.actors)
            {
                maxActor = null;
                maxVal = 0;
                foreach (Actor rowActor in rowCluster.actors)
                    if (colActor != rowActor && matrix.Get(rowActor, colActor) > maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = rowActor;
                    }
                if (maxActor == null)
                    triplets.Add(new Triple(0, 1, 0.5 * (double)(nbrRows - ((rowCluster == colCluster) ? 1 : 0))));
                else
                {
                    if (idealMatrix != null)
                        idealMatrix.Set(maxActor, colActor, 1);
                    foreach (Actor rowActor in rowCluster.actors)
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), (rowActor == maxActor) ? 1 : 0, 0.5));
                }
            }

            // Checking row-regular (half weight)
            int nbrCols = colCluster.actors.Count;
            double w = 0.5 * (double)(nbrCols - ((rowCluster == colCluster) ? 1 : 0));

            foreach (Actor rowActor in rowCluster.actors)
            {
                maxActor = null;
                maxVal = double.NegativeInfinity;
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = colActor;
                    }
                triplets.Add(new Triple(maxVal, 1, w));
                if (idealMatrix != null)
                    idealMatrix.Set(rowActor, maxActor!, 1);
            }
            return triplets;
        }

        public override _Block cloneBlock()
        {
            return new pcddBlock();

        }
    }
}
