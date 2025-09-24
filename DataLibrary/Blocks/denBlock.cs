using Socnet.DataLibrary;

namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the density ideal block
    /// </summary>
    public class denBlock : _Block
    {
        // Parameter for the specific density for this block
        public double d;

        /// <summary>
        /// Constructor for the density ideal block. By default, the density value is set to 0.5.
        /// </summary>
        public denBlock()
        {
            Name = "den";
            this.d = 0.5;
            isoIndex = 9;
        }

        /// <summary>
        /// Constructor for the density ideal block, also setting the density (default 0.5)
        /// </summary>
        /// <param name="d">The specific density of this block</param>
        public denBlock(double d = 0.5)
        {
            Name = "den";
            this.d = Functions.minMaxRange(d, 0, 1);
            isoIndex = 9;
        }

        /// <summary>
        /// Method to set the density of this density block
        /// </summary>
        /// <param name="v">The density of the block</param>
        public override void initArgValue(double v)
        {
            this.d = Functions.minMaxRange(v, 0, 1);
        }

        public override _Block cloneBlock()
        {
            return new denBlock(this.d);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            List<Edge> observedEdges = new List<Edge>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        observedEdges.Add(new Edge(rowActor, colActor, matrix.Get(rowActor, colActor)));
                    }
            int nbrCells = observedEdges.Count;
            int i1 = (int)Math.Round((double)nbrCells * d);
            int i0 = nbrCells - i1;
            observedEdges.Sort((s1, s2) => s1.value.CompareTo(s2.value));
            for (int i = 0; i < i0; i++)
            {
                triplets.Add(new Triple(observedEdges[i].value, 0, 1));
                if (idealMatrix != null)
                    idealMatrix.Set(observedEdges[i].from, observedEdges[i].to, 0);
            }
            for (int i = i0; i < nbrCells; i++)
            {
                triplets.Add(new Triple(observedEdges[i].value, 1, 1));
                if (idealMatrix != null)
                    idealMatrix.Set(observedEdges[i].from, observedEdges[i].to, 1);
            }
            return triplets;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            int sum = 0;
            int nbrCells = rowCluster.actors.Count * (colCluster.actors.Count - ((rowCluster == colCluster) ? 1 : 0));

            int i1 = (int)Math.Round((double)nbrCells * d);
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        if (matrix.Get(rowActor, colActor) > 0)
                        {
                            sum++;
                            if (i1 > 0)
                            {
                                if (idealMatrix != null)
                                    idealMatrix.Set(rowActor, colActor, 1);
                                i1--;
                            }
                        }
                    }
            return Math.Abs(sum - (int)Math.Round((double)nbrCells * d));
        }

        public override string ToString()
        {
            return Name + "(" + d + ")";
        }
    }
}