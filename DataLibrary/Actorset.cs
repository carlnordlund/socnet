namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for Actorset object, i.e. a set of ordered Actors for a given network/table
    /// </summary>
    public class Actorset : DataStructure
    {
        public List<Actor> actors;
        public Dictionary<string, Actor> labelToActor;
        public Dictionary<int, Actor> indexToActor;

        /// <summary>
        /// Returns the number of actors in the Actorset
        /// </summary>
        public int Count
        {
            get { return actors.Count; }
        }

        /// <summary>
        /// Get the number of actors in the Actorset as a string
        /// </summary>
        /// <returns>Number of actors as string</returns>
        internal override string GetSize()
        {
            return actors.Count.ToString();
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("index" + "\t" + "label");
            foreach (Actor actor in actors)
                content.Add(actor.index + "\t" + actor.Name);
        }

        /// <summary>
        /// Constructor for Actorset object
        /// </summary>
        /// <param name="name">Name of actorset</param>
        public Actorset(string name)
        {
            this.Name = name;
            this.actors = new List<Actor>();
            this.labelToActor = new Dictionary<string, Actor>();
            this.indexToActor = new Dictionary<int, Actor>();
        }

        /// <summary>
        /// Get specific Actor object by the name of the actor
        /// </summary>
        /// <param name="label">Name of Actor</param>
        /// <returns>Returns Actor object or null if no Actor with that name was found.</returns>
        internal Actor? GetActorByLabel(string label)
        {
            if (labelToActor.ContainsKey(label))
                return labelToActor[label];
            return null;
        }

        /// <summary>
        /// Method for regenerating lookup dictionaries for Actorsets
        /// </summary>
        internal void recreateLabelAndIndexToActor()
        {
            labelToActor.Clear();
            indexToActor.Clear();
            foreach (Actor actor in actors)
            {
                labelToActor.Add(actor.Name, actor);
                indexToActor.Add(actor.index, actor);
            }
        }

        /// <summary>
        /// Method for creating string array with actor labels with optional quote character
        /// </summary>
        /// <param name="quote">Optional quote character surrounding each string</param>
        /// <returns>String array of actor names</returns>
        internal string[] GetActorLabelArray(string quote = "")
        {
            recreateLabelAndIndexToActor();
            string[] actorLabels = new string[Count];
            for (int i = 0; i < Count; i++)
                actorLabels[i] = quote + indexToActor[i].Name + quote;
            return actorLabels;
        }
    }
}