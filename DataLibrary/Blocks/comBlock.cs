namespace Socnet.DataLibrary.Blocks
{
    public class comBlock : _Block
    {
        public comBlock()
        {
            Name = "com";
            primeIndex = 2;
        }

        public override _Block cloneBlock()
        {
            return new comBlock();
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            double penalty = 0;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        penalty += (matrix.Get(rowActor, colActor) < 1) ? 1 : 0;
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, 1);
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
                            idealMatrix.Set(rowActor, colActor, 1);
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), 1, 1));
                    }
            return triplets;
        }
    }
}
