namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the Ucinet-style density block, with common methods and attributes
    /// </summary>
    public class denuciBlock : _Block
    {
        /// <summary>
        /// Constructor for the Ucinet-style density ideal block
        /// </summary>
        public double d;

        public denuciBlock()
        {
            Name = "denuci";
            this.d = 0.5;
            primeIndex = 8;
        }

        /// <summary>
        /// Constructor for the Ucinet-style 'density' ideal block, setting the optimal 'density'
        /// </summary>
        public denuciBlock(double d)
        {
            Name = "denuci";
            this.d = d;
            primeIndex = 8;
        }

        /// <summary>
        /// Method to set the 'density' of this Ucinet-style 'density' block
        /// </summary>
        /// <param name="v">The 'density' of the block, i.e. the value with which all tie values will be correlated with</param>
        public override void initArgValue(double v)
        {
            this.d = v;
        }

        public override _Block cloneBlock()
        {
            return new denuciBlock(this.d);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), d, 1));
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, d);
                    }
            return triplets;
        }

        public override string ToString()
        {
            return Name + "(" + d + ")";
        }
    }
}
