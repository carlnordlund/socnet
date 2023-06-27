﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class cfnBlock : _Block
    {
        public cfnBlock()
        {
            Name = "cfn";
            primeIndex = 7;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            double sum_row, sum_tot = 0, pc = 0;
            foreach (Actor colActor in colCluster.actors)
            {
                sum_row = 0;
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        sum_tot++;
                        sum_row++;
                    }
                if (sum_row > 0)
                    pc++;
            }
            return sum_tot - pc + (nc - pc) * nr;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrRows = rowCluster.actors.Count;
            Actor? maxActor = null;
            double maxVal;
            foreach (Actor colActor in colCluster.actors)
            {
                maxActor = null;
                maxVal = 0;
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > maxVal)
                    {
                        maxVal = matrix.Get(rowActor, colActor);
                        maxActor = rowActor;
                    }
                if (maxActor == null)
                    triplets.Add(new Triple(0, 1, (double)(nbrRows - ((rowCluster == colCluster) ? 1 : 0))));
                else
                    foreach (Actor rowActor in rowCluster.actors)
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), (colActor == maxActor) ? 1 : 0, 1));
            }
            return triplets;
        }

    }
}