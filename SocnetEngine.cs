using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet
{
    public class SocnetEngine
    {

        public SocnetEngine()
        {
        }

        internal void startInteractiveMode()
        {

        }

        internal List<string> loadAndExecuteScript(string filepath)
        {
            List<string> response = new List<string>();
            response.Add("1st response line from executing script");
            response.Add("2nd response line from executing script");
            return response;
        }
    }
}
