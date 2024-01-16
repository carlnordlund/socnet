namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for Partition, where the Actor objects of an Actorset are partitioned into non-overlapping clusters
    /// </summary>
    public class Partition : DataStructure
    {
        // Attributes for the Actorset, the array of Cluster objects, the partition array
        public Actorset actorset;
        public Cluster[] clusters;
        public int[] partArray;
        int nbrActors, nbrClusters;

        /// <summary>
        /// Constructor for Partition object, providing the Actorset to use and the name of the Partition
        /// Creates a blank partition, i.e. with no clusters
        /// </summary>
        /// <param name="actorset">Actorset that is partitioned here</param>
        /// <param name="name">The name of this Partition object</param>
        public Partition(Actorset actorset, string name)
        {
            this.Name = name;
            this.actorset = actorset;
            clusters = new Cluster[0];
            nbrActors = actorset.Count;
            partArray = new int[nbrActors];
        }

        /// <summary>
        /// Constructor for Partition objects, providing an existing Partition as a template to clone
        /// The resulting partition has the same name as the existing one with _clone as postfix
        /// </summary>
        /// <param name="otherPartition">Existing Partition object to clone</param>
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

        /// <summary>
        /// Method to initialize a given number of (empty) clusters
        /// </summary>
        /// <param name="nbrClusters">Number of clusters</param>
        public void createClusters(int nbrClusters)
        {
            clusters = new Cluster[nbrClusters];
            for (int i = 0; i < nbrClusters; i++)
                clusters[i] = new Cluster("P" + i);
            this.nbrClusters = nbrClusters;
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Actorset:" + actorset.Name);
            for (int i = 0; i < nbrClusters; i++)
            {
                content.Add("Cluster " + i + ": " + clusters[i].Name);
                foreach (Actor actor in clusters[i].actors)
                    content.Add(actor.Name + "\t(" + actor.index + ")");
            }
        }

        internal override string GetSize()
        {
            return nbrClusters + " clusters;" + nbrActors + " actors";
        }

        /// <summary>
        /// Method to initialize a zero-partition, i.e. where all Actors are placed in the first cluster
        /// </summary>
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

        /// <summary>
        /// Method to empty all clusters
        /// </summary>
        private void emptyClusters()
        {
            foreach (Cluster cluster in clusters)
                cluster.clear();
            partArray = new int[actorset.Count];
        }

        /// <summary>
        /// Method to 'increment' a partition: move an actor between two clusters in an incremental fashion so that all possible permutations will have been achieved
        /// Primarily used for the exhaustive search algorithm (but doesn't check for minimum clustering size here)
        /// </summary>
        /// <returns>Returns true if successful in incrementing the partition. Returns false if reached the end, i.e. where all Actors were already in the 'last' cluster</returns>
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

        /// <summary>
        /// Method returning a string representation of the specific cluster, consisting of cluster indices for each actor separated by semicolons
        /// </summary>
        /// <param name="sep">Character to separate cluster indices (optional; by default semicolon)</param>
        /// <returns>Returns the so-called partition string</returns>
        public string GetPartString(string sep = ";")
        {
            return String.Join(sep, partArray);
        }

        /// <summary>
        /// Method to check that each cluster contain the minimum number of actors
        /// </summary>
        /// <param name="minClusterSize">Minimum number of Actors that each cluster must have</param>
        /// <returns>Returns true if all clusters have the minimum number of Actor objects</returns>
        internal bool CheckMinimumClusterSize(int minClusterSize)
        {
            foreach (Cluster cluster in clusters)
                if (cluster.actors.Count < minClusterSize)
                    return false;
            return true;
        }

        /// <summary>
        /// Method to retrieve a copy of the partition array
        /// </summary>
        /// <returns>Array representing this partition</returns>
        internal int[] GetPartArrayCopy()
        {
            int[] partArrayCopy = new int[partArray.Length];
            for (int c = 0; c < clusters.Length; c++)
                foreach (Actor actor in clusters[c].actors)
                    partArrayCopy[actor.index] = c;
            return partArrayCopy;
        }

        /// <summary>
        /// Method to set the current partition given the provided partition array
        /// </summary>
        /// <param name="partarray">The partition array where the cluster index of each actor is provided</param>
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

        /// <summary>
        /// Method for setting a random partition
        /// </summary>
        /// <param name="minClusterSize">Minimum number of actors in each partition</param>
        /// <param name="random">An instance of the Random class</param>
        /// <returns>Returns the partition string for the set partition</returns>
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

        /// <summary>
        /// Method to recreate the internal partition array based on current cluster content
        /// </summary>
        internal void recreatePartArrayFromClusters()
        {
            for (int i = 0; i < clusters.Length; i++)
                foreach (Actor actor in clusters[i].actors)
                    partArray[actor.index] = i;
        }

        /// <summary>
        /// Method for switching two actors in different clusters with each other
        /// I.e. Actor 1 in Cluster 1 is moved to Cluster 2, and Actor 2 in Cluster 2 is moved to Cluster 1
        /// </summary>
        /// <param name="a1">Actor 1</param>
        /// <param name="c1">Cluster 1 that contains Actor 1</param>
        /// <param name="a2">Actor 2</param>
        /// <param name="c2">Cluster 2 that contains Actor 2</param>
        internal void switchActors(Actor a1, int c1, Actor a2, int c2)
        {
            clusters[c1].removeActor(a1);
            clusters[c2].addActor(a1);
            clusters[c2].removeActor(a2);
            clusters[c1].addActor(a2);
            partArray[a1.index] = c2;
            partArray[a2.index] = c1;
        }

        /// <summary>
        /// Method for moving an Actor from one cluster to another
        /// I.e. Actor is moved from Cluster 1 to Cluster 2
        /// </summary>
        /// <param name="actor">Actor to move</param>
        /// <param name="c1">Cluster 1 where the Actor originally is</param>
        /// <param name="c2">Cluster 2 to where the Actor is to be moved</param>
        internal void moveActor(Actor actor, int c1, int c2)
        {
            clusters[c1].removeActor(actor);
            clusters[c2].addActor(actor);
            partArray[actor.index] = c2;
        }
    }
}