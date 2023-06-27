using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class denBlock : _Block
    {
        public double d;

        public denBlock(double d = 0.5)
        {
            Name = "den";
            this.d = d;
            primeIndex = 9;
        }

        public override _Block cloneBlock()
        {
            return new denBlock(this.d);
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            double nbrCells = 0, sum = 0;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor!=colActor)
                    {
                        nbrCells++;
                        sum += (matrix.Get(rowActor, colActor) > 0) ? 1 : 0;
                    }
            double minSum = Math.Ceiling(nbrCells * d);
            if (sum < minSum)
                return minSum - sum;
            return 0;
        }

        public override string ToString()
        {
            return Name + "_" + d;
        }

    }
}
