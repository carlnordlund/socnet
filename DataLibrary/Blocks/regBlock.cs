namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the regular ideal block
    /// </summary>
    public class regBlock : _Block
    {
        /// <summary>
        /// Constructor for the regular ideal block
        /// </summary>
        public regBlock()
        {
            Name = "reg";
            primeIndex = 3;
        }

        public override _Block cloneBlock()
        {
            return new regBlock();
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            double pr = 0, pc = 0;
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        pr++;
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, 1);
                        break;
                    }

            foreach (Actor colActor in colCluster.actors)
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        pc++;
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, 1);
                        break;
                    }
            return (nc - pc) * nr + (nr - pr) * nc;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrRows = rowCluster.actors.Count, nbrCols = colCluster.actors.Count;
            double w = (double)(nbrRows * nbrCols - ((rowCluster == colCluster) ? nbrRows : 0)) / (nbrRows + nbrCols);
            Actor? maxActor;
            double maxVal;
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
