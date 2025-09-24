namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// This is an example of how a new ideal block is implemented in Socnet.se. This example has an
    /// internal parameter (similar to the ideal density blocks) which can then be used when calculating
    /// penalties/correlations. If the new ideal block doesn't have any such internal parameter, then this
    /// can be excluded and all references to 'param' below can be removed.
    /// 
    /// Implement the 'getTripletList' and/or the 'getPenaltyHamming' methods.
    /// 
    /// Assign a unique isoIndex to this ideal block.
    /// 
    /// Make sure to register this ideal block in the BlockmodelingConstant class!
    /// </summary>
    public class _exampleBlock : _Block
    {
        /// <summary>
        /// If your ideal block takes an internal parameter, this is stored here.
        /// Can of course be renamed to something else.
        /// </summary>
        public double param;

        /// <summary>
        /// Constructor for the ideal block.
        /// If the ideal block has no internal parameter, the 'this.param = 0.5' should be removed.
        /// </summary>
        public _exampleBlock()
        {
            Name = "expl";
            this.param = 0.5;
            isoIndex = 100;
        }

        /// <summary>
        /// Special constructor for those ideal blocks that have internal parameters.
        /// In this case, it is verified that the internal parameter is a value between 0 and 1.
        /// That should be adjusted accordingly.
        /// </summary>
        /// <param name="param"></param>
        public _exampleBlock(double param = 0.5)
        {
            Name = "expl";
            this.param = Functions.minMaxRange(param, 0, 1);
            isoIndex = 100;
        }

        /// <summary>
        /// Support function for ideal blocks that have internal parameters.
        /// If your ideal block doesn't have an internal parameter, this can be removed.
        /// </summary>
        /// <param name="param"></param>
        public override void initArgValue(double param)
        {
            this.param = Functions.minMaxRange(param, 0, 1);
        }

        /// <summary>
        /// Necessary function for cloning an ideal block.
        /// </summary>
        /// <returns></returns>
        public override _Block cloneBlock()
        {
            return new _exampleBlock(this.param);
        }

        /// <summary>
        /// This method collects values for the weighted-correlation-based goodness-of-fit function (called 'nordlund'
        /// in Socnet.se). Given a network matrix and two subsets of actors representing rows and column positions,
        /// this method must determine the specific pair-wise values that are to be correlated, as well as the weight of
        /// that pair. See Nordlund (2020) for details on how this works.
        /// This method thus returns a list of Triple structs (just three double values: see Blockmodeling.cs) representing
        /// the specific pair-wise correlations that capture this particular block, these two positions, for this network.
        /// If an idealMatrix is provided, that should also be populated with the ties that fulfill the criteria for this block.
        /// </summary>
        /// <param name="matrix">The network</param>
        /// <param name="rowCluster">The set of actors corresponding to the row position</param>
        /// <param name="colCluster">The set of actors corresponding to the column position</param>
        /// <param name="idealMatrix">Optional matrix representing how the ideal blockmodel would look like.</param>
        /// <returns>A list of Triple (structs) for this particular block.</returns>
        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();

            // Add code to compare the observed content of this block (as specified by matrix,rowCluster, and colCluster
            // vis-a-vis the ideal structure that should be here, here using the correlation-based approach to measure
            // goodness-of-fit. As specified in Nordlund (2020), the weight of all triplets should correspond to the same
            // weight as if each cell of the observed block was included once.

            return triplets;
        }

        /// <summary>
        /// This method collects penalties when using the deviation-based goodness-of-fit function (i.e. Hamming distances).
        /// Given a network matrix and two subsets of actors representing row and column positions, this method must count the
        /// number of deviations between the ideal content of this block and the observed, returning the number of deviations
        /// for this particular ideal block and these two positions.
        /// If an idealMatrix is provided, that should also be populated with the ties that fulfill the criteria for this block
        /// </summary>
        /// <param name="matrix">The network</param>
        /// <param name="rowCluster">The set of actors corresponding to the row position</param>
        /// <param name="colCluster">The set of actors corresponding to the column position</param>
        /// <param name="idealMatrix">Optional matrix representing how the ideal blockmodel would look like.</param>
        /// <returns>The number of inconsistencies between the observed block and this ideal block</returns>
        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            double penalty = 0;

            // Add code to determine the number of deviations between the observed content of this block (as specified by matrix,
            // rowCluster, and colCluster vis-a-vis the ideal structure that should be here.

            return penalty;
        }


    }
}
