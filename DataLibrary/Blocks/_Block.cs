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

        public virtual double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix idealMatrix)
        {
            return 0;
        }

        public virtual List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix idealMatrix)
        {
            return new List<Triple>();
        }

        public virtual double getPenaltyZiberna(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix idealmatrix)
        {
            return 0;
        }

        public abstract _Block cloneBlock();

        public virtual void initArgValue(double v)
        {

        }

        public override string ToString()
        {
            return Name;
        }

    }
}
