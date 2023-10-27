using socnet.DataLibrary;

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

        public pcoBlock(double p = 0.5)
        {
            Name = "pco";
            this.p = Functions.minMaxRange(p, 0, 1);
            primeIndex = 11;
        }

        public override void initArgValue(double v)
        {
            this.p = Functions.minMaxRange(v, 0, 1);
        }
        public override _Block cloneBlock()
        {
            return new pcoBlock(this.p);
        }

        public override List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            // Non-weighted approach: simply correlate remaining with themselves
            List<Triple> triplets = new List<Triple>();
            // nr, nc : Nbr of rows/columns
            double nr = rowCluster.actors.Count, nc = colCluster.actors.Count;

            // kr, kc : Nbr of cells to check in each row/column
            double kr = Math.Ceiling((nc - ((rowCluster == colCluster ? 1 : 0))) * p), kc = Math.Ceiling((nr - ((rowCluster == colCluster ? 1 : 0))) * p);

            // n : Total number of cells in the block
            double n = nr * (nc - ((rowCluster == colCluster) ? 1 : 0));

            // rowLists: dict with row values for each of the row actors
            // colLists: corresponding storage of column values, for each column actor
            Dictionary<Actor, List<Edge>> rowLists = new Dictionary<Actor, List<Edge>>();
            Dictionary<Actor, List<Edge>> colLists = new Dictionary<Actor, List<Edge>>();

            // Prepare dictionaries
            foreach (Actor rowActor in rowCluster.actors)
                rowLists.Add(rowActor, new List<Edge>());
            foreach (Actor colActor in colCluster.actors)
                colLists.Add(colActor, new List<Edge>());
            double v;
            foreach (Actor rowActor in rowCluster.actors)
                foreach (Actor colActor in colCluster.actors)
                    if (rowActor != colActor)
                    {
                        Edge edge = new Edge(rowActor, colActor, matrix.Get(rowActor, colActor));
                        v = matrix.Get(rowActor, colActor);
                        rowLists[rowActor].Add(edge);
                        colLists[colActor].Add(edge);
                    }
            foreach (KeyValuePair<Actor, List<Edge>> rowObj in rowLists)
            {
                rowObj.Value.Sort((s1, s2) => s2.value.CompareTo(s1.value));

                for (int k = 0; k < kr; k++)
                {
                    triplets.Add(new Triple(rowObj.Value[k].value, 1, 0.5));
                    if (idealMatrix != null)
                        idealMatrix.Set(rowObj.Value[k].from, rowObj.Value[k].to, 1);
                }
                for (int k = (int)Math.Ceiling(kr); k < rowObj.Value.Count; k++)
                    triplets.Add(new Triple(rowObj.Value[k].value, rowObj.Value[k].value, 0.5));
            }

            foreach (KeyValuePair<Actor, List<Edge>> colObj in colLists)
            {
                colObj.Value.Sort((s1, s2) => s2.value.CompareTo(s1.value));
                for (int k = 0; k < kc; k++)
                {
                    triplets.Add(new Triple(colObj.Value[k].value, 1, 0.5));
                    if (idealMatrix != null)
                        idealMatrix.Set(colObj.Value[k].from, colObj.Value[k].to, 1);
                }
                for (int k = (int)Math.Ceiling(kc); k < colObj.Value.Count; k++)
                    triplets.Add(new Triple(colObj.Value[k].value, colObj.Value[k].value, 0.5));
            }
            return triplets;
        }

        public override string ToString()
        {
            return Name + "(" + p + ")";
        }
    }
}
