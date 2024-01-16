namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the null ideal block
    /// </summary>
    public class nulBlock : _Block
    {
        /// <summary>
        /// Constructor for the null ideal block
        /// </summary>
        public nulBlock()
        {
            Name = "nul";
            primeIndex = 1;
        }

        public override _Block cloneBlock()
        {
            return new nulBlock();
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            double penalty = 0;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        penalty += (matrix.Get(rowActor, colActor) > 0) ? 1 : 0;
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, 0);
                    }
            return penalty;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, 0);
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), 0, 1));
                    }
            return triplets;
        }
    }
}
