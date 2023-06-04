using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary
{
    public class Actor : DataStructure
    {
        public int index; 
        public string label;
        //Dictionary<string, string> supplementaryLabels = new Dictionary<string, string>();

        //public Actor(string label)
        //{
        //    this.label = label;
        //}

        public Actor(string label, int index)
        {
            this.label = label;
            this.index = index;
        }

        internal override void GetContent(List<string> content)
        {
            content.Add(label + " (" + index + ")");
        }

        internal override string GetSize()
        {
            return "1";
        }
    }
}
