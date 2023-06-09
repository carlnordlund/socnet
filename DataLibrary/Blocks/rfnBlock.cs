using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class rfnBlock : _Block
    {
        public rfnBlock()
        {
            Name = "rfn";
            primeIndex = 6;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            double sum_row, sum_tot = 0, pr = 0;
            foreach (Actor rowActor in rowCluster.actors)
            {
                sum_row = 0;
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor!=colActor && matrix.Get(rowActor,colActor)>0)
                    {
                        sum_tot++;
                        sum_row++;
                    }
                if (sum_row > 0)
                    pr++;
            }
            return sum_tot - pr + (nr - pr) * nc;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrCols = colCluster.actors.Count;
            Actor? maxActor = null;
            double maxVal;
            foreach (Actor rowActor in rowCluster.actors)
            {
                maxActor = null;
                maxVal = 0;
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor!=colActor && matrix.Get(rowActor,colActor)>maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = colActor;
                    }
                if (maxActor == null)
                    triplets.Add(new Triple(0, 1, (double)(nbrCols - ((rowCluster == colCluster) ? 1 : 0))));
                else
                    foreach (Actor colActor in colCluster.actors)
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), (colActor == maxActor) ? 1 : 0, 1));
            }
            return triplets;
        }
    }
}
