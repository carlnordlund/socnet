using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Socnet.DataLibrary;
using System.Text.RegularExpressions;


namespace Socnet
{
    public class SocnetEngine
    {
        Dataset dataset;
        
        

        List<string> response = new List<string>();
        Dictionary<string, string> args = new Dictionary<string, string>();

        Dictionary<string, string[]> args_must = new Dictionary<string, string[]>()
        {
            {"load", new string[] {"file","type"} },
            {"loadmatrix", new string[] {"file"} },
            {"setwd", new string[] {"dir"} },
            {"view", new string[] {"name"} },
            {"delete", new string[] {"name"} }
        };

        public SocnetEngine()
        {
            // Initiate stuff, particularly initiate new NetworkDataset object
            dataset = new Dataset();
            dataset.Name = "Untitled";
        }

        // Execute a command line (single function)
        internal List<string> executeCommand(string input)
        {
            response.Clear();
            CommandBlob cb = parseCommand(input);
            if (cb.error)
            {
                response.Add("!Error parsing: " + input);
                return response;
            }

            // Set current argument disctionary
            args = cb.arguments;

            // Does this function exist in the engine? (Note additional _ in the method name
            System.Reflection.MethodInfo? methodInfo = typeof(SocnetEngine).GetMethod("f_" + cb.function);
            if (methodInfo == null)
                // No, doesn't exist - send an error back
                response.Add("!Error: function '" + cb.function + "' doesn't exist!");
            else
            {
                if (args_must.ContainsKey(cb.function))
                {
                    foreach (string arg_key in args_must[cb.function])
                    {
                        if (!args.ContainsKey(arg_key) || args[arg_key].Length == 0)
                        {
                            response.Add("!Error: Argument '" + arg_key + "' missing");
                            return response;
                        }
                    }
                }
                // Yes, function exist. All necessary arguments there?
                // Yes, execute this function
                methodInfo.Invoke(this, null);
                // Why do I get a return value here? The functions will store stuff in response anyway
                // and structures will be stored immediately etc.
            }

            return response;
        }

        // Takes a function-style command, tries parsing it, and returns a CommandBlob
        // which is a struct containing the function name and parsed/trimmed argument dictionary
        private CommandBlob parseCommand(string input)
        {
            CommandBlob cb = new CommandBlob();
            Match match = Regex.Match(input.TrimEnd(';'), @"^(\w+)(\((.*?)\))?$");
            if (match.Success)
            {
                cb.function = match.Groups[1].Value.ToLower();
                Dictionary<string, string> args = new Dictionary<string, string>();
                string arg_string = match.Groups[3].Value.Trim();
                if (arg_string.Length>0)
                {
                    // Ok - there is something in the argument, so parse this
                    string[] arg_pairs = arg_string.Split(',');
                    foreach (string arg_pair in arg_pairs)
                    {
                        string[] kv_pair = arg_pair.Trim().Split('=');
                        if (kv_pair.Length==2)
                        {
                            string key = kv_pair[0].Trim().ToLower();
                            string value = kv_pair[1].Trim(new Char[] { ' ', '"', '\'' });
                            if (key.Length > 0 && value.Length > 0)
                            {
                                if (!cb.arguments.ContainsKey(key))
                                    cb.arguments.Add(key, value);
                            }
                            else
                            {
                                cb.error = true;
                                return cb;
                            }
                        }
                        else
                        {
                            cb.error = true;
                            return cb;
                        }
                    }
                }
            }
            return cb;
        }

        internal List<string> executeCommandString(string commands)
        {
            return new List<string>();
        }

        public struct CommandBlob
        {
            public string assigner = "";
            public string function = "";
            public Dictionary<string, string> arguments;
            public bool error = false;

            public CommandBlob()
            {
                this.arguments = new Dictionary<string, string>();
            }
        }
        
        // If an error is encountered while doing a function, this prepares a custom message and returns false
        private bool errorResponse(string errorMessage)
        {
            response.Add(errorMessage);
            return false;
        }

        // This is a lookup for checking if a particular argument key exists - if so, returns the value; otherwise null
        // Good for checking in functions
        private string getArgument(string key)
        {
            if (args.ContainsKey(key) && args[key].Length > 0)
                return args[key];
            return "";
        }




        // METHODS for FUNCTIONS

        public void f_getwd()
        {
            response.Add(Directory.GetCurrentDirectory());
        }

        public void f_setwd()
        {
            string dir = getArgument("dir");
            try
            {
                Directory.SetCurrentDirectory(dir);
                response.Add("Setting working directory: " + dir);
            }
            catch (Exception e)
            {
                response.Add(e.Message);
            }
        }

        public void f_loadmatrix()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getArgument("file"), "matrix", getArgument("name")));
        }

        public void f_load()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getArgument("file"), getArgument("type"), getArgument("name")));
            //string type = getArgument("type");
            //if (type.Equals("matrix"))
            //    f_loadmatrix();
            //else
            //    errorResponse("!Error: type '" + type + "' not recognized");
        }

        public void f_delete()
        {
            DataStructure? structure = dataset.GetStructureByName(getArgument("name"));
            if (structure != null)
                response.Add(dataset.DeleteStructure(structure));
            else
                errorResponse("!Error: Structure '" + getArgument("name") + " not found");


        }

        public void f_structures()
        {
            response.Add("DataType" + "\t" + "Name" + "\t" + "Size");
            string type = getArgument("type");
            if (type == "")
                foreach (DataStructure structure in dataset.structures)
                    response.Add(structure.DataType + "\t" + structure.Name + "\t" + structure.Size);
            else
                foreach (DataStructure structure in dataset.structures)
                    if (structure.GetType().Name == type)
                        response.Add(structure.DataType + "\t" + structure.Name + "\t" + structure.Size);
        }

        public void f_view()
        {
            string name = getArgument("name");
            DataStructure? structure = dataset.GetStructureByName(name);
            if (structure == null) {
                response.Add("!Error: Structure '" + name + "' not found");
                return;
            }
            response.AddRange(structure.View);
        }
    }
}
