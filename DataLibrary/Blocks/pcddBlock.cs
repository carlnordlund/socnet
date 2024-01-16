namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the periphery-to-core dependency-and-dominance ideal block
    /// </summary>
    public class pcddBlock : _Block
    {
        /// <summary>
        /// Constructor for the periphery-to-core dependency-and-dominance ideal block as used in the power-relational core-periphery metric (Nordlund 2018)
        /// This block is typically only used by the 'coreperi()' function in socnet.se when using the power-relational option
        /// </summary>
        public pcddBlock()
        {
            Name = "pcdd";
            primeIndex = 12;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;

            // Checking row-functional (heavy penalty, half weight)
            int nbrCols = colCluster.actors.Count;
            Actor? maxActor = null;
            double maxVal;
            foreach (Actor rowActor in rowCluster.actors)
            {
                maxActor = null;
                maxVal = 0;
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = colActor;
                    }
                if (maxActor == null)
                    triplets.Add(new Triple(0, 1, 0.5 * (double)(nbrCols - ((rowCluster == colCluster) ? 1 : 0))));
                else
                {
                    if (idealMatrix != null)
                        idealMatrix.Set(rowActor, maxActor, 1);
                    foreach (Actor colActor in colCluster.actors)
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), (colActor == maxActor) ? 1 : 0, 0.5));
                }
            }

            // Checking col-regular (half weight)
            int nbrRows = rowCluster.actors.Count;
            double w = 0.5 * (double)(nbrRows - ((rowCluster == colCluster) ? 1 : 0));

            foreach (Actor colActor in colCluster.actors)
            {
                maxActor = null;
                maxVal = double.NegativeInfinity;
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = rowActor;
                    }
                triplets.Add(new Triple(maxVal, 1, w));
                if (idealMatrix != null)
                    idealMatrix.Set(maxActor!, colActor, 1);
            }
            return triplets;
        }

        public override _Block cloneBlock()
        {
            return new pcddBlock();
        }
    }
}