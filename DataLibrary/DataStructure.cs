using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary
{
    public abstract class DataStructure
    {
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
