namespace Socnet.DataLibrary.Blocks
{
    public abstract class _Block
    {
        public string Name = "";
        public int primeIndex;

        public virtual double getPenaltyHamming(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            return 0;
        }

        public virtual List<Triple> getTripletList(Matrix matrix, Cluster rowCluster, Cluster colCluster, Matrix? idealMatrix = null)
        {
            return new List<Triple>();
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
