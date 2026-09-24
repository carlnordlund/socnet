namespace Socnet.Core.Model
{
    /// <summary>
    /// A partition of the actors of an <see cref="Actorset"/> into a number of non-overlapping,
    /// named clusters. Stored as a cluster index per actor.
    /// </summary>
    public sealed class Partition : DataStructure
    {
        /// <summary>
        /// Creates a partition where all actors are in the first cluster. Clusters are named P0, P1, ...
        /// </summary>
        public Partition(Actorset actorset, string name, int nbrClusters)
            : this(actorset, name, DefaultClusterNames(nbrClusters), new int[actorset.Count])
        {
        }

        /// <summary>
        /// Creates a partition with the given cluster names and cluster assignments.
        /// </summary>
        /// <param name="actorset">The partitioned Actorset.</param>
        /// <param name="name">Name of the partition.</param>
        /// <param name="clusterNames">Names of the clusters.</param>
        /// <param name="clusterOf">Cluster index for each actor (copied).</param>
        public Partition(Actorset actorset, string name, string[] clusterNames, int[] clusterOf)
        {
            if (clusterOf.Length != actorset.Count)
                throw new ArgumentException("Partition array length differs from actorset size");
            Actorset = actorset;
            Name = name;
            ClusterNames = (string[])clusterNames.Clone();
            ClusterOf = (int[])clusterOf.Clone();
        }

        /// <summary>
        /// The Actorset that is partitioned.
        /// </summary>
        public Actorset Actorset { get; }

        /// <summary>
        /// Cluster index for each actor.
        /// </summary>
        public int[] ClusterOf { get; }

        /// <summary>
        /// The names of the clusters.
        /// </summary>
        public string[] ClusterNames { get; }

        /// <summary>
        /// Number of clusters.
        /// </summary>
        public int NbrClusters => ClusterNames.Length;

        /// <summary>
        /// Returns the actor indices of a cluster, in actor index order.
        /// </summary>
        public int[] Members(int cluster)
        {
            List<int> members = [];
            for (int i = 0; i < ClusterOf.Length; i++)
                if (ClusterOf[i] == cluster)
                    members.Add(i);
            return [.. members];
        }

        /// <summary>
        /// Returns the actor indices of all clusters, in actor index order within each cluster.
        /// </summary>
        public int[][] AllMembers()
        {
            List<int>[] lists = new List<int>[NbrClusters];
            for (int c = 0; c < NbrClusters; c++)
                lists[c] = [];
            for (int i = 0; i < ClusterOf.Length; i++)
                lists[ClusterOf[i]].Add(i);
            return [.. lists.Select(l => l.ToArray())];
        }

        /// <summary>
        /// Returns the partition string: the cluster index of each actor, separated by the given separator.
        /// </summary>
        public string GetPartString(string sep = ";") => string.Join(sep, ClusterOf);

        /// <summary>
        /// Returns a copy of this partition with a new name.
        /// </summary>
        public Partition Clone(string name) => new(Actorset, name, ClusterNames, ClusterOf);

        /// <summary>
        /// Returns the default cluster names P0, P1, ...
        /// </summary>
        public static string[] DefaultClusterNames(int nbrClusters)
        {
            string[] names = new string[nbrClusters];
            for (int i = 0; i < nbrClusters; i++)
                names[i] = "P" + i;
            return names;
        }

        public override string Size => $"{NbrClusters} clusters;{Actorset.Count} actors";

        public override void GetContent(List<string> content)
        {
            content.Add(":Actorset:" + Actorset.Name);
            int[][] members = AllMembers();
            for (int c = 0; c < NbrClusters; c++)
            {
                content.Add($":Cluster {c}: {ClusterNames[c]}");
                foreach (int actor in members[c])
                    content.Add($":{Actorset.Label(actor)}\t({actor})");
            }
        }
    }
}
