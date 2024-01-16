namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class specifying an Actor object, i.e. a node/vertex in a network
    /// </summary>
    public class Actor : DataStructure
    {
        public int index;

        /// <summary>
        /// Constructor for Actor object, specifying its name and its index
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        public Actor(string name, int index)
        {
            this.Name = name;
            this.index = index;
        }

        internal override void GetContent(List<string> content)
        {
            content.Add(Name + " (" + index + ")");
        }

        internal override string GetSize()
        {
            return "1";
        }
    }
}