﻿using socnet.DataLibrary;

namespace Socnet.DataLibrary.Blocks
{
    /// <summary>
    /// Class for the minimum-density ideal block
    /// </summary>
    public class denminBlock : _Block
    {
        // Parameter for the specific minimum density for this block
        public double d;

        /// <summary>
        /// Constructor for the minimum-density ideal block. By default, the minimum density value is set to 0.5.
        /// </summary>
        public denminBlock()
        {
            Name = "denmin";
            this.d = 0.5;
            primeIndex = 10;
        }

        /// <summary>
        /// Constructor for the minimum-density ideal block, also setting the minimum density (default 0.5)
        /// </summary>
        /// <param name="d">The specific minimum density of this block</param>
        public denminBlock(double d = 0.5)
        {
            Name = "denmin";
            this.d = Functions.minMaxRange(d, 0, 1);
            primeIndex = 10;
        }

        /// <summary>
        /// Method to set the minimum density of this minimum-density block
        /// </summary>
        /// <param name="v">The minimum density of the block</param>
        public override void initArgValue(double v)
        {
            this.d = Functions.minMaxRange(v, 0, 1);
        }

        public override _Block cloneBlock()
        {
            return new denminBlock(this.d);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            List<Edge> observedEdges = new List<Edge>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        //observedList.Add(matrix.Get(rowActor, colActor));
                        observedEdges.Add(new Edge(rowActor, colActor, matrix.Get(rowActor, colActor)));
                    }
            int nbrCells = observedEdges.Count;
            int i1 = (int)Math.Ceiling((double)nbrCells * d);
            
            int i0 = nbrCells - i1;
            observedEdges.Sort((s1, s2) => s2.value.CompareTo(s1.value));
            for (int i = 0; i < i1; i++)
            {
                triplets.Add(new Triple(observedEdges[i].value, 1, 1));
                if (idealMatrix != null)
                    idealMatrix.Set(observedEdges[i].from, observedEdges[i].to, 1);
            }
            for (int i = i1; i < nbrCells; i++)
            {
                triplets.Add(new Triple(observedEdges[i].value, observedEdges[i].value, 1));
                if (idealMatrix != null)
                    idealMatrix.Set(observedEdges[i].from, observedEdges[i].to, observedEdges[i].value);
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
                            else if (idealMatrix != null)
                                idealMatrix.Set(rowActor, colActor, matrix.Get(rowActor, colActor));
                        }
                    }
            i1 = (int)Math.Round((double)nbrCells * d);
            if (sum < i1)
                return i1 - sum;
            return 0;
        }

        public override string ToString()
        {
            return Name + "(" + d + ")";
        }
    }
}