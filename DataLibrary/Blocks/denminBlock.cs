using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class denminBlock : _Block
    {
        public double d;

        public denminBlock()
        {
            Name = "denmin";
            this.d = 0.5;
            primeIndex = 10;
        }

        public denminBlock(double d = 0.5)
        {
            Name = "denmin";
            this.d = d;
            primeIndex = 10;
        }

        public override void initArgValue(double v)
        {
            this.d = v;
        }

        public override _Block cloneBlock()
        {
            return new denminBlock(this.d);
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
            observedList.Reverse();
            for (int i = 0; i < i1; i++)
                triplets.Add(new Triple(observedList[i], 1, 1));
            for (int i = i1; i < nbrCells; i++)
                triplets.Add(new Triple(observedList[i], observedList[i], 1));
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
            int minSum = (int)Math.Ceiling(nbrCells * d);
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
