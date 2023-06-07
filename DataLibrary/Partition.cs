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
        //public List<Cluster> clusters; // Better to have as an array?
        public Cluster[] clusters;
        public int[] partArray;
        int nbrActors, nbrClusters;

        public Partition(Actorset actorset, string name)
        {
            this.Name = name;
            this.actorset = actorset;
            clusters = new Cluster[0];
            //clusters = new List<Cluster>();
            nbrActors = actorset.Count;
            partArray = new int[nbrActors];
        }

        internal void createClusters(int nbrClusters)
        {
            clusters = new Cluster[nbrClusters];
            //clusters.Clear();
            for (int i = 0; i < nbrClusters; i++)
                clusters[i] = new Cluster("P" + i);
                //clusters.Add(new Cluster("P" + i));
            this.nbrClusters = nbrClusters;
        }



        internal override void GetContent(List<string> content)
        {
            content.Add("Actorset:" + actorset.Name);
            for (int i=0;i<nbrClusters;i++)
            {
                content.Add("Cluster " + i + ": " + clusters[i].Name);
                foreach (Actor actor in clusters[i].actors)
                    content.Add(actor.Name);
            }
        }

        internal override string GetSize()
        {
            return nbrClusters + " clusters;" + nbrActors + " actors";
        }

        internal void setZeroPartition()
        {
            // This is an initial partition, where all actors are placed in the first clusters
            // All other potential remaining clusters are empty
            emptyClusters();
            foreach (Actor actor in actorset.actors)
            {
                partArray[actor.index] = 0;
                clusters[0].addActor(actor);
            }
        }

        private void emptyClusters()
        {
            foreach (Cluster cluster in clusters)
                cluster.clear();
        }

        internal bool incrementPartition()
        {
            foreach (Actor actor in actorset.actors)
            {
                int actorIndex = actor.index;
                int clusterIndex = partArray[actorIndex];
                clusters[clusterIndex].removeActor(actor);
                if (clusterIndex == nbrClusters - 1)
                {
                    // Ok - reached the end, so reset
                    clusters[0].addActor(actor);
                    partArray[actorIndex] = 0;
                    // Do not abort: iterate to the next actor
                }
                else
                {
                    // Not yet reached the end, so just move actor to next cluster
                    clusters[clusterIndex + 1].addActor(actor);
                    partArray[actorIndex] = clusterIndex + 1;
                    // ...and then abort - all ok!
                    return true;
                }
            }
            // If reached here without aborting: reached end of partition permutation, return false
            return false;
        }

        internal string GetPartString()
        {
            return String.Join("", partArray);
        }

        internal bool CheckMinimumClusterSize(int minClusterSize)
        {
            foreach (Cluster cluster in clusters)
                if (cluster.actors.Count < minClusterSize)
                    return false;
            return true;
        }
    }
}
