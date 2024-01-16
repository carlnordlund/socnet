namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the column-regular ideal block
    /// </summary>
    public class creBlock : _Block
    {
        /// <summary>
        /// Constructor for the column-regular ideal block
        /// </summary>
        public creBlock()
        {
            Name = "cre";
            primeIndex = 5;
        }

        public override _Block cloneBlock()
        {
            return new creBlock();
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            double pc = 0;
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            foreach (Actor colActor in colCluster.actors)
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        pc++;
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, 1);
                        break;
                    }
            return (nc - pc) * nr;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrRows = rowCluster.actors.Count;
            double w = (double)(nbrRows - ((rowCluster == colCluster) ? 1 : 0));
            double maxVal;
            Actor? maxActor;
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
    }
}
