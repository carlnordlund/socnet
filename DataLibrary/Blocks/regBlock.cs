using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class regBlock : _Block
    {
        public regBlock()
        {
            Name = "reg";
            primeIndex = 3;
        }

        public override _Block cloneBlock()
        {
            return new regBlock();
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            double pr = 0, pc = 0;
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        pr++;
                        break;
                    }

            foreach (Actor colActor in colCluster.actors)
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        pc++;
                        break;
                    }
            return (nc - pc) * nr + (nr - pr) * nc;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrRows = rowCluster.actors.Count, nbrCols = colCluster.actors.Count;
            double w = (double)(nbrRows * nbrCols - ((rowCluster == colCluster) ? nbrRows : 0)) / (nbrRows + nbrCols);
            //Actor maxActor;
            double maxVal;
            foreach (Actor rowActor in rowCluster.actors)
            {
                //maxActor = null;
                maxVal = double.NegativeInfinity;
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor!=colActor && matrix.Get(rowActor,colActor)>maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        //maxActor = colActor;
                    }
                triplets.Add(new Triple(maxVal, 1, w));
            }
            foreach (Actor colActor in colCluster.actors)
            {
                maxVal = double.NegativeInfinity;
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > maxVal)
                        maxVal = matrix.Get(rowActor, colActor);
                triplets.Add(new Triple(maxVal, 1, w));
            }
            return triplets;
        }

    }
}
