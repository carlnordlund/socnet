using Socnet.DataLibrary;

namespace socnet.DataLibrary
{
    /// <summary>
    /// Struct representing an Edge
    /// </summary>
    public struct Edge
    {
        // The Actor objects that this Edge connect, and the value of the edge
        public Actor from, to;
        public double value;

        /// <summary>
        /// Constructor for an Edge struct
        /// </summary>
        /// <param name="from">The Actor that the edge is from</param>
        /// <param name="to">The Actor that the edge is to</param>
        /// <param name="value">The value of the edge</param>
        public Edge(Actor from, Actor to, double value)
        {
            this.from = from;
            this.to = to;
            this.value = value;
        }
    }
}
