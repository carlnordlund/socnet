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

        public Partition(Partition otherPartition)
        {
            this.Name = otherPartition.Name + "_clone";
            this.actorset = otherPartition.actorset;
            this.nbrActors = this.actorset.Count;
            this.nbrClusters = otherPartition.nbrClusters;
            this.partArray = new int[nbrActors];
            this.clusters = new Cluster[nbrClusters];
            for (int i = 0; i < otherPartition.clusters.Length; i++)
            {
                this.clusters[i] = new Cluster(otherPartition.clusters[i].Name);
                foreach (Actor actor in otherPartition.clusters[i].actors)
                {
                    this.clusters[i].addActor(actor);
                    this.partArray[actor.index] = i;
                }
            }
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
            for (int i = 0; i < nbrClusters; i++)
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
            partArray = new int[actorset.Count];
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
            return String.Join(";", partArray);
        }

        internal bool CheckMinimumClusterSize(int minClusterSize)
        {
            foreach (Cluster cluster in clusters)
                if (cluster.actors.Count < minClusterSize)
                    return false;
            return true;
        }

        internal int[] GetPartArrayCopy()
        {
            int[] partArrayCopy = new int[partArray.Length];
            for (int c = 0; c < clusters.Length; c++)
                foreach (Actor actor in clusters[c].actors)
                    partArrayCopy[actor.index] = c;
            return partArrayCopy;
        }

        internal void setPartitionByPartArray(int[] partarray)
        {
            emptyClusters();
            foreach (Actor actor in actorset.actors)
            {
                int actor_index = actor.index;
                int cluster_index = partarray[actor_index];
                clusters[cluster_index].addActor(actor);
                partArray[actor_index] = cluster_index;
            }
        }

        internal string setRandomPartition(int minClusterSize, Random random)
        {
            if (nbrClusters > 0)
            {
                while (true)
                {
                    emptyClusters();
                    foreach (Actor actor in actorset.actors)
                        clusters[random.Next(0, nbrClusters)].addActor(actor);
                    if (CheckMinimumClusterSize(minClusterSize))
                        break;
                }
                recreatePartArrayFromClusters();
                return GetPartString();

            }
            return "";
        }

        internal void recreatePartArrayFromClusters()
        {
            for (int i = 0; i < clusters.Length; i++)
                foreach (Actor actor in clusters[i].actors)
                    partArray[actor.index] = i;
        }

        internal void switchActors(Actor a1, int c1, Actor a2, int c2)
        {
            clusters[c1].removeActor(a1);
            clusters[c2].addActor(a1);
            clusters[c2].removeActor(a2);
            clusters[c1].addActor(a2);
            partArray[a1.index] = c2;
            partArray[a2.index] = c1;
        }

        internal void moveActor(Actor actor, int c1, int c2)
        {
            clusters[c1].removeActor(actor);
            clusters[c2].addActor(actor);
            partArray[actor.index] = c2;
        }
    }
}