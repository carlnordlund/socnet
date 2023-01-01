using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet
{
    public class SocnetEngine
    {
        // By default quite verbose, but could also be set by a command
        // so that the engine becomes less verbose
        // Potentially ordinal variable, for different levels of responses
        bool verbose = true;
        int sleep = 2000;


        public SocnetEngine()
        {
            // Initiate stuff, particularly initiate new NetworkDataset object
        }

        internal List<string> loadAndExecuteScript(string filepath)
        {
            // Either load script and execute each line here, or let this just be a shortcut
            // for an engine command for loading and executing a script
            // In this way, can have nested script, i.e. a script calling another script!
            List<string> response = new List<string>();
            response.Add("1st response line from executing script");
            response.Add("2nd response line from executing script");
            return response;
        }

        internal List<string> executeCommand(string input)
        {
            List<string> response = new List<string>();
            
            if (input.Equals("help"))
            {
                response.Add("Provide helpers here for commands, e.g. where to find documentation");
            }
            else
            {
                response.Add("Executing " + input);
                response.Add("response line from doing the above");
                Thread.Sleep(sleep);
            }
            return response;
        }
    }
}
