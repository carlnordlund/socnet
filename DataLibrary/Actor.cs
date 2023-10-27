namespace Socnet.DataLibrary
{
    public class Actor : DataStructure
    {
        public int index;

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
