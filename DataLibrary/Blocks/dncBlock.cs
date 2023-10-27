namespace Socnet.DataLibrary.Blocks
{
    public class dncBlock : _Block
    {
        public dncBlock()
        {
            Name = "dnc";
            primeIndex = 0;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            if (idealMatrix != null)
            {
                foreach (Actor rowActor in rowCluster.actors)
                    foreach (Actor colActor in colCluster.actors)
                        if (rowActor != colActor)
                            idealMatrix.Set(rowActor, colActor, double.NaN);
            }
            return base.getPenaltyHamming(matrix, rowCluster, colCluster, idealMatrix);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            if (idealMatrix != null)
            {
                foreach (Actor rowActor in rowCluster.actors)
                    foreach (Actor colActor in colCluster.actors)
                        if (rowActor != colActor)
                            idealMatrix.Set(rowActor, colActor, double.NaN);
            }
            return base.getTripletList(matrix, rowCluster, colCluster, idealMatrix);
        }

        public override _Block cloneBlock()
        {
            return new dncBlock();
        }
    }
}
