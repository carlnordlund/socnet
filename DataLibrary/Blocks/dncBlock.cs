namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the do-not-care (dnc) ideal block
    /// I.e. the 'block' where any kind of pattern is allowed
    /// </summary>
    public class dncBlock : _Block
    {
        /// <summary>
        /// Constructor for the do-not-care ideal block
        /// </summary>
        public dncBlock()
        {
            Name = "dnc";
            isoIndex = 0;
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
