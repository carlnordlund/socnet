namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Abstract class for an ideal block, with common methods and attributes
    /// </summary>
    public abstract class _Block
    {
        // Name of ideal block
        public string Name = "";

        // Index of ideal block (manually incremented for respective ideal block; used for determining isomorphic block images)
        public int primeIndex;

        /// <summary>
        /// Virtual method to get Hamming distance for a particular block in a matrix
        /// </summary>
        /// <param name="matrix">The Matrix object of the network</param>
        /// <param name="rowCluster">Cluster of actors constituting the block row</param>
        /// <param name="colCluster">Cluster of actors constituting the block column</param>
        /// <param name="idealMatrix">Matrix where the ideal pattern will be stored (optional)</param>
        /// <returns>Number of inconsistencies between actual ties and ideal block</returns>
        public virtual double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            return 0;
        }

        /// <summary>
        /// Virtual method to get list of triplets for a particular block in a matrix, used by the weighted correlation coefficient goodness-of-fit method
        /// </summary>
        /// <param name="matrix">The Matrix object of the network</param>
        /// <param name="rowCluster">Cluster of actors constituting the block row</param>
        /// <param name="colCluster">Cluster of actors constituting the block column</param>
        /// <param name="idealMatrix">Matrix where the ideal pattern will be stored (optional)</param>
        /// <returns>List of Triplet objects for this particular block, as input to the weighted correlation coefficient goodness-of-fit measure</returns>
        public virtual List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            return new List<Triple>();
        }

        /// <summary>
        /// Method for cloning a block, i.e. returning a new instance of this Block object
        /// </summary>
        /// <returns>A new instance of this particular Block object</returns>
        public abstract _Block cloneBlock();

        /// <summary>
        /// Method to initialize a Block-specific parameter (if applicable)
        /// </summary>
        /// <param name="v">The value of the internal parameter</param>
        public virtual void initArgValue(double v)
        {

        }

        /// <summary>
        /// Returns the name of this ideal block type
        /// </summary>
        /// <returns>Name of block</returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
