﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class denuciBlock : _Block
    {
        public double d;

        public denuciBlock(double d = 0.5)
        {
            Name = "denuci";
            this.d = d;
            primeIndex = 8;
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), d, 1));
            return triplets;
        }

        public override string ToString()
        {
            return Name + "_" + d;
        }

    }
}