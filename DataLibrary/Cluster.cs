namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class to represent a Cluster of Actor objects
    /// </summary>
    public class Cluster : DataStructure
    {
        // List of Actor objects in this cluster
        public List<Actor> actors;

        /// <summary>
        /// Constructor for a Cluster object
        /// </summary>
        /// <param name="name">Name of this cluster object</param>
        public Cluster(string name)
        {
            this.Name = name;
            actors = new List<Actor>();
        }

        /// <summary>
        /// Method to add an Actor to this cluster
        /// </summary>
        /// <param name="actor">Actor object to add</param>
        public void addActor(Actor actor)
        {
            if (!actors.Contains(actor))
                actors.Add(actor);
        }

        /// <summary>
        /// Method to remove an actor from this cluster
        /// </summary>
        /// <param name="actor">Actor object to remove</param>
        public void removeActor(Actor actor)
        {
            if (actors.Contains(actor))
                actors.Remove(actor);
        }

        /// <summary>
        /// Remove all actors from the cluster
        /// </summary>
        public void clear()
        {
            actors.Clear();
        }

        internal override string GetSize()
        {
            return actors.Count.ToString();
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Cluster:" + Name);
            foreach (Actor actor in actors)
                content.Add("Actor: " + actor.Name);
        }
    }
}
