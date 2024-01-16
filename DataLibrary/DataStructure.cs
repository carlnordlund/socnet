namespace Socnet.DataLibrary
{
    /// <summary>
    /// Abstract class for a DataStructure
    /// </summary>
    public abstract class DataStructure
    {
        // Name and cell format for this data structure
        public string Name = "";
        public string Cellformat = "";

        public string DataType
        {
            get { return this.GetType().Name; }
        }

        public string Size
        {
            get { return GetSize(); }
        }

        public List<string> View
        {
            get
            {
                List<string> content = new List<string>();
                content.Add("Name:" + Name + "\tDatatype:" + DataType + "\tSize:" + Size);
                GetContent(content);
                return content;
            }
        }

        internal abstract string GetSize();

        internal abstract void GetContent(List<string> content);
    }
}
