using Socnet.DataLibrary;
using System.Text.RegularExpressions;

namespace Socnet
{
    /// <summary>
    /// Class for SocnetEngine, which will receive and execute commands
    /// </summary>
    public class SocnetEngine
    {
        // Set version info
        public string versionString = "Version 1.4 (October 2025)";

        // Prepare Dataset object
        Dataset dataset;

        // Characters used when trimming from csv files etc
        char[] trimChars = new Char[] { ' ', '"', '\'' };

        // the userDirectoryPath
        public string userDirectoryPath = "";

        // Buffer for storing response lines when executing commands
        public List<string> response = new List<string>();

        // Dictionary with key-value argument list
        Dictionary<string, string> args_input = new Dictionary<string, string>();

        // Dictionary holding all available commands and compulsory arguments to each (might have more arguments)
        Dictionary<string, string[]> args_required = new Dictionary<string, string[]>()
        {
            {"load", new string[] {"file","type" } },
            {"save", new string[] {"name","file"} },
            {"loadscript", new string[] {"file" } },
            {"loadactorset", new string[] {"file" } },
            {"loadmatrix", new string[] {"file" } },
            {"loadblockimage", new string[] {"file" } },
            {"loadpartition", new string[] {"file" } },
            {"loadedgelist", new string[] {"file", "col1","col2" } },
            {"setwd", new string[] {"dir" } },
            {"view", new string[] {"name" } },
            {"delete", new string[] {"name" } },
            {"rename", new string[] {"name", "newname" } },
            {"actorset", new string[] {"size"} },
            {"matrix", new string[] {"actorset"} },
            {"blockimage", new string[] {"size"} },
            {"partition", new string[] {"actorset","nbrclusters"} },
            {"bminit", new string[] {"network", "blockimage", "searchtype", "method" } },
            {"bivarieties", new string[] {"blockimage"} },
            {"biextend", new string[] {"blockimage"} },
            {"bmtest", new string[] {"network","blockimage","partition","method"} },
            {"bmview", new string[] {"blockmodel"} },
            {"bmextract", new string[] {"blockmodel", "type"} },
            {"densities", new string[] {"network", "partition" } },
            {"coreperi", new string[] {"network", "searchtype" } },
            {"dichotomize", new string[] {"name", "condition", "threshold" } },
            {"symmetrize", new string[] {"name", "method" } },
            {"rescale", new string[] {"name" } },
            {"set", new string[] {"name","value","row" } },
            {"randomseed", new string[] {"seed"} }
        };

        /// <summary>
        /// Constructor for SocnetEngine
        /// </summary>
        public SocnetEngine()
        {
            // Initiate stuff, particularly initiate new NetworkDataset object
            dataset = new Dataset();
            dataset.Name = "Untitled";
        }

        /// <summary>
        /// Method for parsing and executing a Socnet command
        /// </summary>
        /// <param name="command">Command (string) to execute</param>
        /// <param name="clearResponse">Whether the response buffer should be cleared at the start (default false)</param>
        /// <returns>List of string responses</returns>
        public List<string> executeCommand(string command, bool clearResponse = false)
        {
            if (clearResponse)
                response.Clear();
            if (command.Length>0 && command[0].Equals('#'))
                return response;
            string pattern = @"^(([\w]+)\s*?=\s*?)?(\w+)(\((.*?)\))?$";
            Match match = Regex.Match(command.Trim(), pattern);
            if (match.Success)
            {
                // Get assigner, function, argstring
                string assigner = match.Groups[2].Value;
                string function = match.Groups[3].Value;
                string argstring = match.Groups[5].Value.Trim();

                args_input.Clear();
                if (argstring.Length > 0)
                {
                    string[] args = argstring.Split(',');
                    string key, value;
                    for (int i = 0; i < args.Length; i++)
                    {
                        string[] kv = args[i].Split('=', 2);
                        if (kv.Length == 1)
                        {
                            if (args_required.ContainsKey(function) && i < args_required[function].Length)
                                key = args_required[function][i];
                            else
                            {
                                response.Add("!Error - Argument index out of range");
                                return response;
                            }
                            value = kv[0];
                        }
                        else if (kv.Length == 2)
                        {
                            key = kv[0].Trim();
                            value = kv[1];
                        }
                        else
                        {
                            response.Add("Something went wrong: " + kv.Length);
                            return response;
                        }

                        if (args_input.ContainsKey(key))
                        {
                            response.Add("!Error: Argument '" + key + "' declared twice");
                            return response;
                        }
                        args_input[key] = value.Trim(trimChars);
                    }
                    if (args_required.ContainsKey(function))
                        // Ok - now check if I got all necessary arguments
                        foreach (string arg in args_required[function])
                            if (!args_input.ContainsKey(arg))
                            {
                                response.Add("!Error: Argument '" + arg + "' missing");
                                return response;
                            }
                }

                DataStructure? returnStructure = null;
                System.Reflection.MethodInfo? methodInfo = typeof(SocnetEngine).GetMethod("f_" + function);
                if (methodInfo != null)
                {
                    object? obj = methodInfo.Invoke(this, null);
                    if (obj != null && obj is DataStructure)
                    {
                        returnStructure = (DataStructure)obj;

                        if (assigner.Length > 0)
                        {
                            returnStructure.Name = assigner;
                            response.Add(dataset.StoreStructure(returnStructure));
                            return response;
                        }
                    }
                    else if (assigner.Length > 0)
                    {
                        response.Add("!Error: Function '" + function + "' returns null, can't be assigned");
                        return response;
                    }
                }
                else
                {
                    returnStructure = dataset.GetStructureByName(function);
                    if (returnStructure == null)
                    {
                        response.Add("!Error: '" + function + "' neither function nor structure");
                        return response;
                    }
                    if (assigner.Length > 0)
                    {
                        response.Add("!Error: Use 'rename()' function to rename");
                        return response;
                    }
                }

                if (returnStructure != null)
                    response.AddRange(returnStructure.View);
                return response;
            }
            if (command.Trim().Length > 0)
                response.Add("!Error: Syntax error!");
            return response;
        }

        /// <summary>
        /// Wrapper for method executeCommand to send in multiple lines of commands separated by newline (\n)
        /// </summary>
        /// <param name="commandString">string of commands separated by newline (\n)</param>
        /// <returns></returns>
        internal List<string> executeCommandString(string commandString)
        {
            string[] commands = commandString.Split('\n');
            response.Clear();
            foreach (string command in commands)
            {
                response.Add("> " + command);
                response.AddRange(executeCommand(command));
            }
            return response;
        }

        /// <summary>
        /// Method to get string value of specific argument in the global args_input dictionary.
        /// Returns empty string if not found.
        /// </summary>
        /// <param name="key">The argument key (string)</param>
        /// <returns>The argument value (string)</returns>
        private string getStringArgument(string key)
        {
            if (args_input.ContainsKey(key) && args_input[key].Length > 0)
                return args_input[key];
            return "";
        }

        /// <summary>
        /// Method to get integer value of specific argument in the global args_input dictionary.
        /// Returns -1 if not found.
        /// </summary>
        /// <param name="key">The argument key (string)</param>
        /// <returns>The argument value (integer)</returns>
        private int getIntegerArgument(string key)
        {
            int retval = -1;
            Int32.TryParse(getStringArgument(key), out retval);
            return retval;
        }

        /// <summary>
        /// Method to get double value of specific argument in the global args_input dictionary.
        /// Return double.NaN if not found.
        /// </summary>
        /// <param name="key">The argument key (string)</param>
        /// <returns>The argument value (double)</returns>
        private double getDoubleArgument(string key)
        {
            double retval = double.NaN;
            if (double.TryParse(getStringArgument(key), out retval))
                return retval;
            return double.NaN;
        }

        // METHODS for FUNCTIONS
        // Each of these functions are called from code, through System.Reflection, so not called directly.
        // All functions are named f_[command] where [command] is the actual socnet.se command.
        // All these functions utilize the various getXXXArgument methods above to obtain command arguments
        // from the global args_input dictionary.
        // These functions add lines to the global response list of strings, which will then constitute the
        // textual output on the console resulting from each command.
        public void f_help()
        {
            response.Add(":HELP SECTION");
            response.Add(":============");
            response.Add(":" + versionString);
            response.Add(":See website for documentation:");
            response.Add(":https://socnet.se");
            response.Add(": ");
            response.Add(":The socnet client is used to do blockmodeling analyses, with some extra functionality for data management.");
            response.Add(":You type in commands on the prompt to do various things with data: load, save, transform,  analyze etc.");
            response.Add(":Check www.socnet.se for command references, how-to, and quick-start tutorial!");
        }

        public void f_citeinfo()
        {
            response.Add(":The Socnet.se software client:");
            response.Add(":  Nordlund, C., Roy, C. (2024). Socnet.se: The Blockmodeling Console App [computer software]. https://socnet.se");
            response.Add(": ");
            response.Add(":Direct blockmodeling using the 'nordlund' method (i.e. weighted correlation coefficients):");
            response.Add(":  Nordlund, C. (2020). Direct blockmodeling of valued and binary networks: a dichotomization-free approach. Social Networks, 61, 128-143.");
            response.Add(":  https://doi.org/10.1016/j.socnet.2019.10.004");
            response.Add(": ");
            response.Add(":Correlation-based core-periphery approach, ignoring inter-categorical blocks or using the 'denuci(d)' ideal blocks:");
            response.Add(":  Borgatti, S.P., Everett, M.G. (2000). Models of core/periphery structures. Social Networks, 21(4), 375-395.");
            response.Add(":  https://doi.org/10.1016/S0378-8733(99)00019-2");
            response.Add(": ");
            response.Add(":Correlation-based core-periphery approach using 'pco(p)' for intra-core and/or 'den(d)' or 'denmin(d)' for inter-categorical ties:");
            response.Add(":  Estévez, J.L., Nordlund, C. (2025). Revising the Borgatti-Everett core-periphery model: Inter-categorical density blocks and partially connected cores. Social Networks, 81, 31-51.");
            response.Add(":  https://doi.org/10.1016/j.socnet.2024.11.002");
            response.Add(": ");
            response.Add(":Power-relational core-periphery approach with core dominance and/or peripheral dependency (i.e. 'powerrelational' argument in 'coreperi'):");
            response.Add(":  Nordlund, C. (2018). Power-relational core–periphery structures: Peripheral dependency and core dominance in binary and valued networks. Network Science, 6(3), 348-369.");
            response.Add(":  https://doi.org/10.1017/nws.2018.15");
            response.Add(": ");
            response.Add(":Direct generalized blockmodeling using 'hamming' distances as penalty function:");
            response.Add(":  Doreian, P., Batagelj, V., Ferligoj, A. (2004). Generalized Blockmodeling. Cambridge: Cambridge University Press.");
            response.Add(":  https://doi.org/10.1017/CBO9780511584176");
        }

        /// <summary>
        /// Command 'getwd()' to display the current working directory
        /// </summary>
        public void f_getwd()
        {
            response.Add(":" + Directory.GetCurrentDirectory());
        }

        public void f_randomseed()
        {
            int seed = getIntegerArgument("seed");
            if (seed < 0)
            {
                response.Add("!Error: Seed value 'seed' not set.");
                return;
            }
            Blockmodeling.SetRandomSeed(seed);
            response.Add("New random seed set: " + seed);

        }

        /// <summary>
        /// Command 'setwd(..)' to set the current working directory
        /// </summary>
        public void f_setwd()
        {
            string dir = getStringArgument("dir");
            try
            {
                if (dir.Equals("user"))
                {
                    if (userDirectoryPath.Length > 0)
                        Directory.SetCurrentDirectory(userDirectoryPath);
                }
                else
                {
                    Directory.SetCurrentDirectory(dir);
                }
                response.Add("Setting working directory: " + Directory.GetCurrentDirectory());
            }
            catch (Exception e)
            {
                response.Add("!Error: " + e.Message);
            }
        }

        /// <summary>
        /// Command 'dir()' to view the content of the current working directory
        /// </summary>
        public void f_dir()
        {
            try
            {
                string[] dirs = Directory.GetDirectories(Directory.GetCurrentDirectory());
                string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
                foreach (string dir in dirs)
                    response.Add(":/" + Path.GetFileName(dir) + "/");
                foreach (string file in files)
                {
                    response.Add(":" + Path.GetFileName(file));
                }
            }
            catch (Exception e)
            {
                response.Add("!Error: Could not list content of directory '" + Directory.GetCurrentDirectory() + "'. Make sure that it is not a symbolic link!");
                response.Add(e.Message);
            }
        }

        /// <summary>
        /// Command 'load(..)' to load a Socnet.se data structure from file
        /// </summary>
        public void f_load()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), getStringArgument("type"), getStringArgument("name"), getStringArgument("sep")));
        }

        /// <summary>
        /// Command 'loadactorset(..)' to load an Actorset structure from file
        /// </summary>
        public void f_loadactorset()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), "actorset", getStringArgument("name"), getStringArgument("sep")));
        }

        /// <summary>
        /// Command 'loadmatrix(..)' to load a Matrix structure from file
        /// </summary>
        public void f_loadmatrix()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), "matrix", getStringArgument("name"), getStringArgument("sep")));
        }

        /// <summary>
        /// Command 'loadblockimage(..)' to load a Blockimage structure from file
        /// </summary>
        public void f_loadblockimage()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), "blockimage", getStringArgument("name"), getStringArgument("sep")));
        }

        /// <summary>
        /// Command 'loadpartition(..)' to load a Partition structure from file
        /// </summary>
        public void f_loadpartition()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), "partition", getStringArgument("name"), getStringArgument("sep")));
        }

        /// <summary>
        /// Command 'save(..)' to save a Socnet.se data structure to file
        /// </summary>
        public void f_save()
        {
            response.Add(SocnetIO.SaveDataStructure(response, dataset, getStringArgument("name"), getStringArgument("file")));
        }

        /// <summary>
        /// Command 'loadscript(..)' to load a text file with Socnet.se commands and execute these
        /// </summary>
        public void f_loadscript()
        {
            string file = getStringArgument("file");
            string[]? commands = SocnetIO.readAllLines(file, response);
            if (commands != null)
            {
                response.Add("Loading and executing '" + file + "'...");
                foreach (string command in commands)
                {
                    if (command.Length > 0 && command[0] != '#')
                    {
                        response.Add("> " + command);
                        executeCommand(command);
                    }
                }
            }
        }

        /// <summary>
        /// Command 'loadedgelist(..)' to load an edgelist from file and store it as a Matrix object
        /// </summary>
        public void f_loadedgelist()
        {
            response.Add(SocnetIO.LoadEdgelist(response,
                dataset,
                getStringArgument("file"),
                getIntegerArgument("col1"),
                getIntegerArgument("col2"),
                getStringArgument("symmetric"),
                getStringArgument("actorset"),
                getIntegerArgument("colval"),
                getStringArgument("headers"),
                getStringArgument("sep")
            ));
        }

        /// <summary>
        /// Command 'rename(..)' to rename a data structure
        /// </summary>
        public void f_rename()
        {
            string oldName = getStringArgument("name"), newName = getStringArgument("newname");
            DataStructure? structure = dataset.GetStructureByName(oldName);

            if (structure == null)
                response.Add("!Error: Structure '" + oldName + "' not found");
            else
            {
                if (dataset.StructureExists(newName))
                    response.Add("!Error: Structure named '" + newName + "' already exists");
                else
                {
                    dataset.structures.Remove(structure.Name);
                    structure.Name = newName;
                    dataset.structures.Add(newName, structure);
                    response.Add("Renamed structure '" + oldName + "' (" + structure.DataType + ") to '" + newName + "'");
                }
            }
        }

        /// <summary>
        /// Command 'delete(..)' to delete a data structure from memory
        /// </summary>
        public void f_delete()
        {
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("name"));
            if (structure != null)
                response.Add(dataset.DeleteStructure(structure));
            else
                response.Add("!Error: Structure '" + getStringArgument("name") + "' not found");
        }

        /// <summary>
        /// Command 'deleteall()' to delete all data structures from memory
        /// </summary>
        public void f_deleteall()
        {
            response.Add(dataset.DeleteAllStructures());
        }

        /// <summary>
        /// Command 'structures(..)' to display stored data structures
        /// </summary>
        public void f_structures()
        {
            response.Add("Name" + "\t" + "Type" + "\t" + "Size");
            response.Add("========" + "\t" + "====" + "\t" + "====");
            string type = getStringArgument("type");
            if (type == "")
                foreach (KeyValuePair<string, DataStructure> obj in dataset.structures)
                    response.Add(":" + obj.Value.Name + "\t" + obj.Value.DataType + "\t" + obj.Value.Size);
            else
                foreach (KeyValuePair<string, DataStructure> obj in dataset.structures)
                    if (obj.Value.GetType().Name.Equals(type, StringComparison.CurrentCultureIgnoreCase))
                        response.Add(":" + obj.Value.Name + "\t" + obj.Value.DataType + "\t" + obj.Value.Size);
        }

        /// <summary>
        /// Command 'view(..)' to view the content of a data structure
        /// </summary>
        public void f_view()
        {
            string name = getStringArgument("name");
            DataStructure? structure = dataset.GetStructureByName(name);
            if (structure == null)
            {
                response.Add("!Error: Structure '" + name + "' not found");
                return;
            }
            response.AddRange(structure.View);
        }

        /// <summary>
        /// Command 'set(..)' to modify a value in a data structure
        /// </summary>
        public void f_set()
        {
            string name = getStringArgument("name");
            DataStructure? structure = dataset.GetStructureByName(name);
            if (structure == null)
            {
                response.Add("!Error: Structure '" + name + "' not found");
                return;
            }
            string rowName = getStringArgument("row"), colName = getStringArgument("col");
            if (structure is Actorset)
            {
                Actorset actorset=(Actorset)structure;
                Actor? actor = actorset.GetActorByLabel(rowName);
                if (actor == null)
                {
                    response.Add("!Error: Actor '" + rowName + "' not found in actorset '" + actorset.Name + "'");
                    return;
                }
                string newLabel = getStringArgument("value");
                if (newLabel.Length < 1 || actorset.labelToActor.ContainsKey(newLabel))
                {
                    response.Add("!Error: Actor label '" + newLabel+ "' either too short or already exists in actorset '" + actorset.Name + "'");
                    return;
                }
                actorset.RenameActor(actor, newLabel);
                return;
            }
            else if (structure is Matrix)
            {
                Matrix matrix = (Matrix)structure;
                Actor? from = matrix.actorset.GetActorByLabel(rowName), to = matrix.actorset.GetActorByLabel(colName);
                if (from == null || to == null)
                {
                    response.Add("!Error: Actor(s) not found in actorset '" + matrix.actorset.Name + "'");
                    return;
                }
                double val = getDoubleArgument("value");
                if (double.IsNaN(val))
                {
                    response.Add("!Error: 'value' not a number");
                    return;
                }
                matrix.Set(from, to, val);
                return;
            }
            else if (structure is Partition)
            {
                Partition partition = (Partition)structure;
                Actor? from = partition.actorset.GetActorByLabel(rowName);
                if (from == null)
                {
                    response.Add("!Error: Actor not found in actorset '" + partition.actorset.Name + "'");
                    return;
                }
                int val = getIntegerArgument("value");
                if (val == -1 || val >= partition.nbrClusters)
                {
                    response.Add("!Error: 'value' not a valid cluster index");
                    return;
                }
                partition.moveActor(from, val);
                return;
            }
            else if (structure is BlockImage)
            {
                BlockImage blockimage = (BlockImage)structure;
                string val = getStringArgument("value");
                if (!blockimage.setBlockByPattern(rowName, colName, val))
                {
                    response.Add("!Error: Could not find position name(s) in BlockImage");
                    return;
                }
                blockimage.checkMultiblocked();
            }
            else
            {
                response.Add("!Error: Not implemented for this structure");
            }
        }

        /// <summary>
        /// Command 'actorset(..)' to create an Actorset structure
        /// </summary>
        /// <returns></returns>
        public Actorset? f_actorset()
        {
            int nbrActors = getIntegerArgument("size");
            if (nbrActors < 1)
            {
                response.Add("!Error: Actorset must contain at least 1 actor");
                return null;
            }
            Actorset? actorset = null;
            string labelstring = getStringArgument("labelarray");
            if (labelstring.Length > 0)
            {
                string[] cells = labelstring.Split(";");
                if (cells.Length != nbrActors)
                {
                    response.Add("!Error: Length of provided labelarray (" + cells.Length + ") not same length as specified Actorset size(" + nbrActors + ")");
                    return null;
                }
                foreach (string cell in cells)
                {
                    if (cell.Length == 0)
                    {
                        response.Add("!Error: Actor labels must consist of at least one character");
                        return null;
                    }
                }
                actorset = dataset.CreateActorsetByLabels(cells);
                if (actorset == null)
                {
                    response.Add("!Error: At least two actors seem to have the same name in the 'labelarray'");
                    return null;
                }
            }
            else
            {
                string[] cells = new string[nbrActors];
                for (int i = 0; i < nbrActors; i++)
                    cells[i] = "actor" + i;
                actorset = dataset.CreateActorsetByLabels(cells);

            }
            return actorset;
        }

        /// <summary>
        /// Command 'matrix()' to create a Matrix structure
        /// </summary>
        /// <returns></returns>
        public Matrix? f_matrix()
        {
            string actorsetName = getStringArgument("actorset");
            if (actorsetName.Length == 0)
            {
                response.Add("!Error: No 'actorset' specified");
                return null;
            }
            DataStructure? structure = dataset.GetStructureByName(actorsetName, typeof(Actorset));
            if (structure == null || !(structure is Actorset))
            {
                response.Add("!Error: Actorset '" + actorsetName + "' not found");
                return null;
            }
            Actorset actorset = (Actorset)structure;

            Matrix matrix = new Matrix(actorset, "", "F2");

            string dataarray = getStringArgument("data");
            if (dataarray.Length > 0)
            {
                string[] cells = dataarray.Split(";");
                if (cells.Length != actorset.Count* actorset.Count)
                {
                    response.Add("!Error: Size of data array (" + cells.Length + ") differs from size of Matrix (" + (actorset.Count * actorset.Count) + ")");
                    return null;
                }
                int index = 0;
                double value = 0;
                foreach (Actor rowActor in actorset.actors)
                    foreach (Actor colActor in actorset.actors)
                    {
                        if (double.TryParse(cells[index], out value))
                            matrix.Set(rowActor, colActor, value);
                        index++;                        
                    }
            }
            return matrix;
        }

        /// <summary>
        /// Command 'blockimage(..)' to create a Blockimage structure
        /// </summary>
        /// <returns></returns>
        public BlockImage? f_blockimage()
        {
            int nbrPositions = getIntegerArgument("size");
            if (nbrPositions < 2)
            {
                response.Add("!Error: Blockimage size must be at least 2");
                return null;
            }
            BlockImage bi = new BlockImage("", nbrPositions);
            string type = getStringArgument("type"), pattern = getStringArgument("pattern"), content = getStringArgument("content");
            if (type != "")
            {
                if (type.Equals("structural"))
                    bi.setBlocksByPattern("nul;com");
                else if (type.Equals("regular"))
                    bi.setBlocksByPattern("nul;reg");
                else
                {
                    response.Add("!Error: Type must be 'structural' or 'regular'");
                    return null;
                }
            }
            else if (pattern != "")
            {
                bi.setBlocksByPattern(pattern);
            }
            else if (content != "")
            {
                string[] contentParts = content.Split('|');
                if (contentParts.Length != nbrPositions * nbrPositions)
                {
                    response.Add("!Error: Size mismatch between content and blockimage size");
                    return null;
                }
                bi.setBlocksByContentString(contentParts);
            }
            return bi;
        }

        /// <summary>
        /// Command 'partition(..)' to create a Partition structure
        /// </summary>
        /// <returns></returns>
        public Partition? f_partition()
        {
            string actorsetName = getStringArgument("actorset");
            if (actorsetName.Length == 0)
            {
                response.Add("!Error: No 'actorset' specified");
                return null;
            }
            DataStructure? structure = dataset.GetStructureByName(actorsetName, typeof(Actorset));
            if (structure == null || !(structure is Actorset))
            {
                response.Add("!Error: Actorset '" + actorsetName + "' not found");
                return null;
            }
            Actorset actorset = (Actorset)structure;
            int nbrClusters = getIntegerArgument("nbrclusters");
            if (nbrClusters < 1)
            {
                response.Add("!Error: Number of clusters must be at least 1");
                return null;
            }
            Partition partition = new Partition(actorset, "");
            partition.createClusters(nbrClusters);

            string partstring = getStringArgument("partarray");
            if (partstring.Length > 0)
            {
                // Ok - also provided a partarray thingie, so initialize the partition
                string[] cells = partstring.Split(";");
                if (cells.Length != actorset.Count)
                {
                    response.Add("!Error: Length of provided partarray (" + cells.Length + ") not same length as Actorset (" + actorset.Count + ")");
                    return null;
                }
                int[] partarray = new int[cells.Length];
                foreach (Actor actor in actorset.actors)
                {
                    if (!Int32.TryParse(cells[actor.index], out partarray[actor.index]))
                    {
                        response.Add("!Error: Couldn't convert '" + cells[actor.index] + "' to an integer");
                        return null;
                    }
                    if (partarray[actor.index] < 0 || partarray[actor.index] >= partition.clusters.Length)
                    {
                        response.Add("!Error: Partition index '" + cells[actor.index] + "' out of bounds");
                        return null;
                    }
                }
                partition.setPartitionByPartArray(partarray);
            }
            else
                partition.setZeroPartition();
            return partition;
        }

        /// <summary>
        /// Function 'coreperi(..)' to initialize and run a Borgatti-Everett-style search for a core-periphery structure
        /// </summary>
        public void f_coreperi()
        {
            Dictionary<string, object?> searchParams = new Dictionary<string, object?>();

            DataStructure? network = dataset.GetStructureByName(getStringArgument("network"), typeof(Matrix));
            if (network == null)
            {
                response.Add("!Error: Network not found (parameter: network)");
                return;
            }

            string searchType = getStringArgument("searchtype");
            if (searchType == "" || !BlockmodelingConstants.SearchTypes.Contains(searchType))
            {
                response.Add("!Error: Search type '" + searchType + "' not recognized (check 'searchtype' parameter)");
                return;
            }

            BlockImage cpbi = new BlockImage("cp", 2);
            cpbi.setPositionName(0, "C");
            cpbi.setPositionName(1, "P");
            cpbi.setBlockByPattern(1, 1, "nul");
            string core = getStringArgument("core");
            cpbi.setBlockByPattern(0, 0, (core.Length > 2 && core.Substring(0, 3).Equals("pco")) ? core : "com");

            string intercat = getStringArgument("intercat");
            string powerrelational = getStringArgument("powerrelational");

            if (powerrelational != "")
            {
                if (powerrelational.Equals("dep"))
                {
                    cpbi.setBlockByPattern(1, 0, "rfn");
                    cpbi.setBlockByPattern(0, 1, "cfn");
                }
                else if (powerrelational.Equals("dom"))
                {
                    cpbi.setBlockByPattern(1, 0, "cre");
                    cpbi.setBlockByPattern(0, 1, "rre");
                }
                else if (powerrelational.Equals("depdom"))
                {
                    cpbi.setBlockByPattern(1, 0, "pcdd");
                    cpbi.setBlockByPattern(0, 1, "cpdd");
                }
                else
                {
                    response.Add("!Error: Power-relational pattern '" + powerrelational + "' not recognized; use 'dep', 'dom' or 'depdom'");
                    return;
                }
                cpbi.Name = "cp" + powerrelational;
            }
            else if (intercat != "")
            {
                cpbi.setBlockByPattern(1, 0, intercat);
                cpbi.setBlockByPattern(0, 1, intercat);
            }
            else
            {
                string ptoc = getStringArgument("ptoc");
                if (ptoc != "")
                    cpbi.setBlockByPattern(1, 0, ptoc);
                else
                    cpbi.setBlockByPattern(1, 0, "dnc");
                string ctop = getStringArgument("ctop");
                if (ctop != "")
                    cpbi.setBlockByPattern(0, 1, ctop);
                else
                    cpbi.setBlockByPattern(0, 1, "dnc");
            }

            if (!cpbi.hasBlocks())
            {
                response.Add("!Error: Something wrong with inter-categorical blocks");
                return;
            }

            searchParams["network"] = network;
            searchParams["blockimage"] = cpbi;
            searchParams["searchtype"] = searchType;
            searchParams["method"] = "nordlund";
            searchParams["minclustersize"] = getIntegerArgument("minclustersize");
            searchParams["nbrrestarts"] = getIntegerArgument("nbrrestarts");
            searchParams["maxiterations"] = getIntegerArgument("maxiterations");
            searchParams["maxtime"] = getIntegerArgument("maxtime");
            searchParams["nbrrandomstart"] = getIntegerArgument("nbrrandomstart");
            searchParams["doswitching"] = getStringArgument("doswitching");
            searchParams["minnbrbetter"] = getIntegerArgument("minnbrbetter");

            string statusInitMsg = Blockmodeling.InitializeSearch(searchParams);
            if (statusInitMsg.Equals("ok"))
            {
                response.AddRange(Blockmodeling.logLines);
                f_bmstart();
            }
            else if (statusInitMsg[0] == '!')
                response.Add(statusInitMsg);
            Blockmodeling.logLines.Clear();
        }

        /// <summary>
        /// Command 'bmtest(..)' to get the blockmodel for a specific partition, network and blockimage
        /// </summary>
        /// <returns></returns>
        public BlockModel? f_bmtest()
        {
            DataStructure? network = dataset.GetStructureByName(getStringArgument("network"), typeof(Matrix));
            if (network == null)
            {
                response.Add("!Error: Network '" + getStringArgument("network") + "' not found (parameter: network)");
                return null;
            }
            DataStructure? blockimage = dataset.GetStructureByName(getStringArgument("blockimage"), typeof(BlockImage));
            if (blockimage == null)
            {
                response.Add("!Error: Blockimage '" + getStringArgument("blockimage") + "'not found (parameter: blockimage)");
                return null;
            }
            DataStructure? partition = dataset.GetStructureByName(getStringArgument("partition"), typeof(Partition));
            if (partition == null)
            {
                response.Add("!Error: Partition '" + getStringArgument("partition") + "' not found (parameter: blockimage)");
                return null;
            }

            string gofMethod = getStringArgument("method");
            if (gofMethod == "" || !BlockmodelingConstants.GofMethods.Contains(gofMethod))
            {
                response.Add("!Error: Method '" + gofMethod + "' not recognized/set (parameter: method)");
                return null;
            }

            if (gofMethod.Equals("nordlund") && ((BlockImage)blockimage).multiBlocked)
            {
                response.Add("!Error: Can't do 'bmtest()' with method 'nordlund' and multiblocked 'blockimage'");
                return null;
            }

            BlockModel? blockmodel = Blockmodeling.GetHypotheticalBlockmodel((Matrix)network, (BlockImage)blockimage, (Partition)partition, gofMethod);
            return blockmodel;
        }

        /// <summary>
        /// Command 'bminit(..)' to initialize a blockmodeling search
        /// </summary>
        public void f_bminit()
        {
            Dictionary<string, object?> searchParams = new Dictionary<string, object?>();

            DataStructure? network = dataset.GetStructureByName(getStringArgument("network"), typeof(Matrix));
            if (network == null)
            {
                response.Add("!Error: Network not found (parameter: network)");
                return;
            }
            DataStructure? blockimage = dataset.GetStructureByName(getStringArgument("blockimage"), typeof(BlockImage));
            if (blockimage == null)
            {
                response.Add("!Error: Blockimage not found (parameter: blockimage)");
                return;
            }
            if (!((BlockImage)blockimage).hasBlocks())
            {
                response.Add("!Error: Blockimage '" + blockimage.Name + "' has unspecified blocks");
                return;
            }
            string searchType = getStringArgument("searchtype");
            if (searchType == "" || !BlockmodelingConstants.SearchTypes.Contains(searchType))
            {
                response.Add("!Error: Search type not recognized/set (parameter: searchtype");
                return;
            }
            string gofMethod = getStringArgument("method");
            if (gofMethod == "" || !BlockmodelingConstants.GofMethods.Contains(gofMethod))
            {
                response.Add("!Error: Method not recognized/set (parameter: method)");
                return;
            }

            searchParams["network"] = network;
            searchParams["blockimage"] = blockimage;
            searchParams["searchtype"] = searchType;
            searchParams["method"] = gofMethod;
            searchParams["minclustersize"] = getIntegerArgument("minclustersize");
            searchParams["nbrrestarts"] = getIntegerArgument("nbrrestarts");
            searchParams["maxiterations"] = getIntegerArgument("maxiterations");
            searchParams["maxtime"] = getIntegerArgument("maxtime");
            searchParams["nbrrandomstart"] = getIntegerArgument("nbrrandomstart");
            searchParams["doswitching"] = getStringArgument("doswitching");
            searchParams["minnbrbetter"] = getIntegerArgument("minnbrbetter");

            string statusInitMsg = Blockmodeling.InitializeSearch(searchParams);
            if (statusInitMsg.Equals("ok"))
                response.AddRange(Blockmodeling.logLines);
            else if (statusInitMsg[0] == '!')
                response.Add(statusInitMsg);
            Blockmodeling.logLines.Clear();
        }

        /// <summary>
        /// Command 'bmstart(..)' to start a blockmodeling search
        /// </summary>
        public void f_bmstart()
        {
            string status = Blockmodeling.StartSearch();
            if (status.Equals("ok"))
            {
                long executionTime = Blockmodeling.stopwatch.ElapsedMilliseconds;
                response.Add("Execution time (ms):" + executionTime);
                response.Add("Nbr tests done:" + Blockmodeling.nbrTested);
                List<BlockModel> blockmodels = Blockmodeling.generateBlockmodelStructuresFromBMSolutions(getStringArgument("outname"));

                foreach (BlockModel bm in blockmodels)
                    response.Add(dataset.StoreStructure(bm));
                response.Add("Goodness-of-fit (1st BlockModel): " + blockmodels[0].gof + " (" + blockmodels[0].gofMethod + ")");
            }
            else if (status.Equals("timeout"))
            {
                response.AddRange(Blockmodeling.logLines);
            }
            else
            {
                response.Add("!Error: Indirect blockmodeling not properly initialized!");
            }
        }

        /// <summary>
        /// Command 'bmview(..)' to display a Blockmodel structure
        /// </summary>
        public void f_bmview()
        {
            BlockModel bm;
            bool showIdealMatrix = (getStringArgument("ideal").Length > 0 && getStringArgument("ideal")[0] == 'y');
            string name = getStringArgument("blockmodel");
            if (name.Length == 0)
            {
                List<DataStructure> structures = dataset.GetStructuresByType(typeof(BlockModel));
                if (structures.Count == 1)
                    bm = (BlockModel)structures[0];
                else
                {
                    if (structures.Count == 0)
                        response.Add("!Error: No BlockModel objects found");
                    else
                        response.Add("!Error: Found several BlockModel objects - specify which with 'blockmodel' parameter");
                    return;
                }
            }
            else
            {
                DataStructure? structure = dataset.GetStructureByName(name, typeof(BlockModel));
                if (structure == null)
                {
                    response.Add("!Error: Blockmodel '" + name + "' not found (parameter: blockmodel)");
                    return;
                }
                bm = (BlockModel)structure;
            }
            response.Add(":Blockmodel:");
            response.AddRange(bm.DisplayBlockmodelMatrix());
            if (showIdealMatrix)
            {
                response.Add(":Ideal blockmodel:");
                response.AddRange(bm.DisplayIdealMatrix());
            }
            response.Add(":Blockimage:");
            response.AddRange(bm.DisplayBlockimage());
            response.Add(":Goodness-of-fit: " + bm.gof + " (" + bm.gofMethod + ")");
        }

        /// <summary>
        /// Command 'bmextract(..)' to extract internal structures from a Blockmodel structure
        /// </summary>
        public void f_bmextract()
        {
            string outname = getStringArgument("outname");
            bool autoname = (outname.Length == 0);

            DataStructure? structure = dataset.GetStructureByName(getStringArgument("blockmodel"), typeof(BlockModel));
            if (structure == null)
            {
                response.Add("!Error: Blockmodel not found");
                return;
            }
            BlockModel blockmodel = (BlockModel)structure;
            string type = getStringArgument("type");
            if (type.Equals("blockimage"))
            {
                BlockImage bi = blockmodel.blockimage;
                bi.Name = (autoname) ? dataset.GetAutoName(bi.Name) : outname;
                response.Add(dataset.StoreStructure(bi));
            }
            else if (type.Equals("matrix"))
            {
                Matrix bmMatrix = blockmodel.bmMatrix;
                Matrix bmIdealMatrix = blockmodel.bmIdealMatrix;
                if (!autoname)
                {
                    bmMatrix.Name = outname;
                    bmMatrix.actorset.Name = outname+"_actors";
                    bmIdealMatrix.Name = outname + "_ideal";
                }
                response.Add(dataset.StoreStructure(bmMatrix.actorset));
                response.Add(dataset.StoreStructure(bmMatrix));
                response.Add(dataset.StoreStructure(bmIdealMatrix));
            }
            else if (type.Equals("partition"))
            {
                Partition partition = blockmodel.partition;
                if (!autoname)
                    partition.Name = outname;
                response.Add(dataset.StoreStructure(partition));
            }
            else if (type.Equals("gof"))
            {
                response.Add(":" + blockmodel.gof + " (" + blockmodel.gofMethod + ")");
            }
        }

        /// <summary>
        /// Command 'densities(..)' to create a density matrix from a network and a partition
        /// </summary>
        public void f_densities()
        {
            DataStructure? net = dataset.GetStructureByName(getStringArgument("network"), typeof(Matrix));
            if (net == null)
            {
                response.Add("!Error: Network not found");
                return;
            }
            Matrix matrix = (Matrix)net;
            DataStructure? part = dataset.GetStructureByName(getStringArgument("partition"), typeof(Partition));
            if (part == null)
            {
                response.Add("!Error: Partition not found");
                return;
            }
            Partition partition = (Partition)part;
            if (partition.actorset != matrix.actorset)
            {
                response.Add("!Error: Partition and Matrix have different actorsets");
                return;
            }

            string[] clusterNames = new string[partition.nbrClusters];
            for (int i = 0; i < partition.nbrClusters; i++)
                clusterNames[i] = partition.clusters[i].Name;

            Actorset? densitiesActorset = dataset.GetActorsetByLabels(clusterNames);
            if (densitiesActorset == null)
            {
                densitiesActorset = dataset.CreateActorsetByLabels(clusterNames);
                if (densitiesActorset == null)
                {
                    response.Add("!Error: Could not create actorset for density matrix");
                    return;
                }
                densitiesActorset.Name = matrix.Name + "_densities_actorset";
                response.Add(dataset.StoreStructure(densitiesActorset));
            }
            Matrix densities = new Matrix(densitiesActorset, matrix.Name + "_densities", "N4");
            for (int r = 0; r < partition.nbrClusters; r++)
                for (int c = 0; c < partition.nbrClusters; c++)
                {
                    double denom = 0;
                    foreach (Actor rowActor in partition.clusters[r].actors)
                        foreach (Actor colActor in partition.clusters[c].actors)
                            if (rowActor != colActor)
                            {
                                densities.data[r, c] += matrix.Get(rowActor, colActor);
                                denom++;
                            }
                    densities.data[r, c] = Math.Round(densities.data[r, c] / denom, 4);

                }
            response.Add(dataset.StoreStructure(densities));
        }

        /// <summary>
        /// Command 'biextend(..)' to make an extention of an existing Blockimage
        /// </summary>
        /// <returns></returns>
        public BlockImage? f_biextend()
        {
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("blockimage"), typeof(BlockImage));
            if (structure == null)
            {
                response.Add("!Error: 'blockimage' not set/recognized");
                return null;
            }

            string pattern = getStringArgument("pattern");


            BlockImage bi_extended = Functions.GetBlockImageExtended((BlockImage)structure, pattern);
            return bi_extended;
            //response.Add(dataset.StoreStructure(bi_extended));
        }

        /// <summary>
        /// Command 'bivarieties(..)' to create all non-isomorphic and non-trivial singleblocked blockimages
        /// </summary>
        public void f_bivarieties()
        {
            response.Add("*** TEST FUNCTION ***");
            response.Add("Create varieties from multiblocked blockimage");
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("blockimage"), typeof(BlockImage));
            if (structure == null || !(structure is BlockImage))
            {
                response.Add("!Error: 'blockimage' not set/recognized");
                return;
            }
            BlockImage bi = (BlockImage)structure;
            if (!bi.multiBlocked)
            {
                response.Add("!Error: Blockimage not multiblocked");
                return;
            }
            List<BlockImage> blockimages = Functions.GetBlockImageVarieties(bi);
            foreach (BlockImage blim in blockimages)
                dataset.StoreStructure(blim);
        }

        /// <summary>
        /// Command 'symmetrize(..)' to symmetrize a Matrix structure
        /// </summary>
        /// <returns></returns>
        public Matrix? f_symmetrize()
        {
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("name"));
            if (!(structure is Matrix))
            {
                response.Add("!Error: Can only symmetrize Matrix structures");
                return null;
            }
            Matrix matrix = (Matrix)structure;
            string method = getStringArgument("method").ToLower();
            if (!Functions.symmMethods.Contains(method))
            {
                response.Add("!Error: Symmetrization method '" + method + "' not known");
                return null;
            }
            Matrix? symmMatrix = Functions.Symmetrize(matrix, method);
            return symmMatrix;
        }

        /// <summary>
        /// Command 'dichotomize(..)' to dichotomize a Matrix structure
        /// </summary>
        /// <returns></returns>
        public DataStructure? f_dichotomize()
        {
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("name"));
            if (structure == null)
            {
                response.Add("!Error: Structure not found");
                return null;
            }
            if (!(structure is Matrix || structure is Table || structure is Vector))
            {
                response.Add("!Error: Can only dichotomize Matrix, Table and Vector structures");
                return null;
            }
            double threshold = getDoubleArgument("threshold");
            if (double.IsNaN(threshold))
            {
                response.Add("!Error: Couldn't parse 'threshold' argument");
                return null;
            }
            string condition = getStringArgument("condition").ToLower();
            if (!Functions.conditionAbbrs.Contains(condition))
            {
                response.Add("!Error: Condition '" + condition + "' not available. Options: ge,gt,le,lt,eq,ne");
                return null;
            }
            string truevalstr = getStringArgument("truevalue"), falsevalstr = getStringArgument("falsevalue");
            double truevalue = truevalstr.Equals("") ? 1 : truevalstr.Equals("keep") ? double.NaN : getDoubleArgument("truevalue");
            double falsevalue = falsevalstr.Equals("") ? 0 : falsevalstr.Equals("keep") ? double.NaN : getDoubleArgument("falsevalue");

            return Functions.Dichotomize(structure, condition, threshold, truevalue, falsevalue);
        }

        /// <summary>
        /// Command 'rescale(..)' to rescale the values in a Matrix structure
        /// </summary>
        /// <returns></returns>
        public DataStructure? f_rescale()
        {
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("name"));
            if (structure == null)
            {
                response.Add("!Error: Structure not found");
                return null;
            }
            if (!(structure is Matrix))
            {
                response.Add("!Error: Can only rescale Matrix objects at the moment");
                return null;
            }
            double min = getDoubleArgument("min"), max = getDoubleArgument("max");
            response.Add("Min value: " + min + ", max:" + max);
            min = (double.IsNaN(min)) ? 0 : min;
            max = (double.IsNaN(max)) ? 1 : max;
            if (min >= max)
            {
                response.Add("!Error: 'max' (" + max + ") must be larger than 'min (" + min + ")");
                return null;
            }
            bool incldiag = (getStringArgument("incldiag").Length > 0 && getStringArgument("incldiag").ToLower()[0] == 'y');
            return Functions.Rescale(structure, min, max, incldiag);
        }
    }
}
