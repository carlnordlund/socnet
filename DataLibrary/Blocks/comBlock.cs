using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class comBlock : _Block
    {
        public comBlock()
        {
            Name = "com";
            primeIndex = 2;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix)
        {
            double penalty = 0;
            if (idealMatrix == null)
            {
                foreach (Actor rowActor in rowCluster.actors)
                    foreach (Actor colActor in colCluster.actors)
                        if (rowActor != colActor && matrix.Get(rowActor, colActor) < 1)
                            penalty++;
            }
            return penalty;
        }
    }
}
