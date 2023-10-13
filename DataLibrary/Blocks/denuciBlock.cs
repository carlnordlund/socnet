using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class denuciBlock : _Block
    {
        public double d;

        public denuciBlock()
        {
            Name = "denuci";
            this.d = 0.5;
            primeIndex = 8;
        }

        public denuciBlock(double d)
        {
            Name = "denuci";
            this.d = d;
            primeIndex = 8;
        }

        public override void initArgValue(double v)
        {
            this.d = v;
        }

        public override _Block cloneBlock()
        {
            return new denuciBlock(this.d);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            List<Triple> triplets = new List<Triple>();
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        triplets.Add(new Triple(matrix.Get(rowActor, colActor), d, 1));
                        if (idealMatrix != null)
                            idealMatrix.Set(rowActor, colActor, d);
                    }
            return triplets;
        }

        public override string ToString()
        {
            return Name + "(" + d + ")";
        }

    }
}
