using Socnet.DataLibrary;

namespace socnet.DataLibrary
{
    public struct Edge
    {
        public Actor from, to;
        public double value;

        public Edge(Actor from, Actor to, double value)
        {
            this.from = from;
            this.to = to;
            this.value = value;
        }
    }
}
