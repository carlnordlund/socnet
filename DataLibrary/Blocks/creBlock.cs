﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Socnet.DataLibrary.Blocks
{
    public class creBlock : _Block
    {
        public creBlock()
        {
            Name = "cre";
            primeIndex = 5;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            double pc = 0;
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            foreach (Actor colActor in colCluster.actors)
                foreach (Actor rowActor in rowCluster.actors)
                    if (rowActor != colActor && matrix.Get(rowActor, colActor) > 0)
                    {
                        pc++;
                        break;
                    }
            return (nc - pc) * nr;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            if (rowCluster == colCluster && rowCluster.actors.Count == 1)
                return triplets;
            int nbrRows = rowCluster.actors.Count;
            double w = (double)(nbrRows - ((rowCluster == colCluster) ? 1 : 0));
            double maxVal;
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