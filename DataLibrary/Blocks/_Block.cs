using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public abstract class _Block
    {
        public string Name = "";
        public int primeIndex;

        public virtual double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            return 0;
        }

    }
}
