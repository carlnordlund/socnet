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

        public denBlock()
        {
            Name = "den";
            this.d = 0.5;
            primeIndex = 9;
        }

        public denBlock(double d = 0.5)
        {
            Name = "den";
            this.d = d;
            primeIndex = 9;
        }

        public override void initArgValue(double v)
        {
            this.d = v;
        }

        public override _Block cloneBlock()
        {
            return new denBlock(this.d);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            List<double> observedList = new List<double>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                        observedList.Add(matrix.Get(rowActor, colActor));
            int nbrCells = observedList.Count;
            int i1 = (int)Math.Round((double)nbrCells * d);
            int i0 = nbrCells - i1;
            observedList.Sort();
            for (int i = 0; i < i0; i++)
                triplets.Add(new Triple(observedList[i], 0, 1));
            for (int i = i0; i < nbrCells; i++)
                triplets.Add(new Triple(observedList[i], 1, 1));
            return triplets;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            int nbrCells = 0, sum = 0;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        nbrCells++;
                        sum += (matrix.Get(rowActor, colActor) > 0) ? 1 : 0;
                    }
            int approxSum = (int)Math.Round(nbrCells * d);
            return Math.Abs(sum - approxSum);
        }

        public override string ToString()
        {
            return Name + "(" + d + ")";
        }

    }
}
