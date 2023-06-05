using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary
{
    public class Partition : DataStructure
    {
        public Actorset actorset;
        public List<Cluster> clusters;
        public int[] partArray;
        int nbrActors, nbrClusters;

        public Partition(Actorset actorset, string name)
        {
            this.Name = name;
            this.actorset = actorset;
            clusters = new List<Cluster>();
            nbrActors = actorset.Count;
            partArray = new int[nbrActors];
        }



        internal override void GetContent(List<string> content)
        {
            throw new NotImplementedException();
        }

        internal override string GetSize()
        {
            throw new NotImplementedException();
        }
    }
}
