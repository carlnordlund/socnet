using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class pcoBlock : _Block
    {
        public double p;

        public pcoBlock()
        {
            Name = "pco";
            this.p = 0.5;
            primeIndex = 11;
        }

        public pcoBlock(double p=0.5)
        {
            Name = "pco";
            this.p = p;
            primeIndex = 11;
        }

        public override void initArgValue(double v)
        {
            this.p = v;
        }
        public override _Block cloneBlock()
        {
            return new pcoBlock(this.p);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster)
        {
            List<Triple> triplets = new List<Triple>();
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;
            double kr = (nc - ((rowCluster == colCluster ? 1 : 0))) * p, kc = (nr - ((rowCluster == colCluster ? 1 : 0))) * p;
            // rowLists: dict with row values for each of the row actors
            // colLists: corresponding storage of column values, for each column actor
            Dictionary<Actor, List<double>> rowLists = new Dictionary<Actor, List<double>>();
            Dictionary<Actor, List<double>> colLists = new Dictionary<Actor, List<double>>();
            // Prepare dictionaries
            foreach (Actor rowActor in rowCluster.actors)
                rowLists.Add(rowActor, new List<double>());
            foreach (Actor colActor in colCluster.actors)
                colLists.Add(colActor, new List<double>());
            double v;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor!=colActor)
                    {
                        v = matrix.Get(rowActor, colActor);
                        rowLists[rowActor].Add(v);
                        colLists[colActor].Add(v);
                    }
            foreach (KeyValuePair<Actor, List<double>> rowObj in rowLists)
            {
                rowObj.Value.Sort();
                rowObj.Value.Reverse();
                for (int k = 0; k < kr; k++)
                    triplets.Add(new Triple(rowObj.Value[k], 1, 1)); // Fix weight here

            }

            foreach (KeyValuePair<Actor, List<double>> colObj in colLists)
            {
                colObj.Value.Sort();
                colObj.Value.Reverse();
                for (int k = 0; k < kc; k++)
                    triplets.Add(new Triple(colObj.Value[k], 1, 1)); // Fix weight here
            }

            // Fix weights above!!


            return triplets;
        }



        public override string ToString()
        {
            return Name + "_" + p;
        }


    }
}
