using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class rreBlock : _Block
    {

        public rreBlock()
        {
            Name = "rre";
            primeIndex = 4;
        }

        public override _Block cloneBlock()
        {
            return new rreBlock();
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            double pr = 0;
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
            return (nr - pr) * nc;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrCols = colCluster.actors.Count;
            double w = (double)(nbrCols - ((rowCluster == colCluster) ? 1 : 0));
            double maxVal;
            Actor? maxActor;
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
    }
}
