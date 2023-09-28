﻿using socnet.DataLibrary;
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
            this.d = Functions.minMaxRange(d, 0, 1);
            primeIndex = 9;
        }

        public override void initArgValue(double v)
        {
            this.d = Functions.minMaxRange(v, 0, 1);
        }

        public override _Block cloneBlock()
        {
            return new denBlock(this.d);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix idealMatrix)
        {
            List<Triple> triplets = new List<Triple>();
            //List<double> observedList = new List<double>();
            List<Edge> observedEdges = new List<Edge>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        //observedList.Add(matrix.Get(rowActor, colActor));
                        observedEdges.Add(new Edge(rowActor, colActor, matrix.Get(rowActor, colActor)));
                    }
            int nbrCells = observedEdges.Count;
            int i1 = (int)Math.Round((double)nbrCells * d);
            int i0 = nbrCells - i1;
            //observedList.Sort();
            observedEdges.Sort((s1, s2) => s1.value.CompareTo(s2.value));
            for (int i = 0; i < i0; i++)
            {
                triplets.Add(new Triple(observedEdges[i].value, 0, 1));
                idealMatrix.Set(observedEdges[i].from, observedEdges[i].to, 0);
            }
            for (int i = i0; i < nbrCells; i++)
            {
                triplets.Add(new Triple(observedEdges[i].value, 1, 1));
                idealMatrix.Set(observedEdges[i].from, observedEdges[i].to, 1);
            }
            return triplets;
        }

        public override double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix idealMatrix)
        {
            int sum = 0;
            int nbrCells = rowCluster.actors.Count * (colCluster.actors.Count - ((rowCluster == colCluster) ? 1 : 0));

            int i1 = (int)Math.Round((double)nbrCells * d);
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        if (matrix.Get(rowActor, colActor) > 0)
                        {
                            sum++;
                            if (i1 > 0)
                            {
                                idealMatrix.Set(rowActor, colActor, 1);
                                i1--;
                            }
                        }
                    }
            return Math.Abs(sum - (int)Math.Round((double)nbrCells * d));
        }

        public override string ToString()
        {
            return Name + "(" + d + ")";
        }

    }
}
