using Socnet.DataLibrary.Blocks;
using Socnet.DataLibrary;
using Socnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace Socnet.DataLibrary.Blocks
{
    public class cpddBlock : _Block
    {
        public cpddBlock()
        {
            Name = "cpdd";
            primeIndex = 13;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;

            // Checking col-functional (heavy penalty, half weight)
            int nbrRows = rowCluster.actors.Count;
            Actor? maxActor = null;
            double maxVal;
            foreach (Actor colActor in colCluster.actors)
            {
                maxActor = null;
                maxVal = 0;
                foreach (Actor rowActor in rowCluster.actors)
                    if (colActor != rowActor && matrix.Get(rowActor, colActor) > maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = rowActor;
                    }
                if (maxActor == null)
                    triplets.Add(new Triple(0, 1, 0.5 * (double)(nbrRows - ((rowCluster == colCluster) ? 1 : 0))));
                else
                {
                    if (idealMatrix != null)
                        idealMatrix.Set(maxActor, colActor, 1);
                    foreach (Actor rowActor in rowCluster.actors)
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), (rowActor == maxActor) ? 1 : 0, 0.5));
                }
            }

            // Checking row-regular (half weight)
            int nbrCols = colCluster.actors.Count;
            double w = 0.5 * (double)(nbrCols - ((rowCluster == colCluster) ? 1 : 0));

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
            return triplets;
        }

        public override _Block cloneBlock()
        {
            return new pcddBlock();

        }
    }
}
