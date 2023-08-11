﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Socnet.DataLibrary;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Net;

namespace Socnet
{
    public class SocnetEngine
    {
        Dataset dataset;
        char[] trimChars = new Char[] { ' ', '"', '\'' };

        public List<string> response = new List<string>();

        Dictionary<string, string> args_input = new Dictionary<string, string>();

        Dictionary<string, string[]> args_required = new Dictionary<string, string[]>()
        {
            {"load", new string[] {"file","type" } },
            {"loadscript", new string[] {"file" } },
            {"loadmatrix", new string[] {"file" } },
            {"setwd", new string[] {"dir" } },
            {"view", new string[] {"name" } },
            {"delete", new string[] {"name" } },
            {"rename", new string[] {"name", "newname" } },
            {"blockimage", new string[] {"size"} },
            {"initdirectbm", new string[] {"network", "blockimage", "searchtype", "method" } },
            {"bivarieties", new string[] {"blockimage"} },
            {"viewbm", new string[] {"blockmodel"} },
            {"save", new string[] {"name","file"} },
            {"bmextract", new string[] {"blockmodel", "type"} },
            {"coreperi", new string[] {"network", "searchtype" } },
            {"dichotomize", new string[] {"name", "condition", "threshold" } },
            {"symmetrize", new string[] {"name", "method" } },
            {"loadedgelist", new string[] {"file", "col1","col2" } }

        };


        public SocnetEngine()
        {
            // Initiate stuff, particularly initiate new NetworkDataset object
            dataset = new Dataset();
            dataset.Name = "Untitled";
        }

        public List<string> executeCommand(string command, bool clearResponse=false)
        {
            if (clearResponse)
                response.Clear();
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
                        response.Add("!Error: Use 'rename()' function to rename, 'duplicate() function to duplicate");
                        return response;
                    }
                }

                if (returnStructure != null)
                    response.AddRange(returnStructure.View);
                return response;
            }
            if (command.Trim().Length>0)
                response.Add("!Error: Syntax error!");
            return response;
        }

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

        // This is a lookup for checking if a particular argument key exists - if so, returns the value; otherwise null
        private string getStringArgument(string key)
        {
            if (args_input.ContainsKey(key) && args_input[key].Length > 0)
                return args_input[key];
            return "";
        }

        private int getIntegerArgument(string key)
        {
            int retval = -1;
            Int32.TryParse(getStringArgument(key), out retval);
            return retval;
        }

        private double getDoubleArgument(string key)
        {
            double retval = double.NaN;
            double.TryParse(getStringArgument(key), out retval);
            return retval;
        }

        // METHODS for FUNCTIONS
        public void f_getwd()
        {
            response.Add(Directory.GetCurrentDirectory());
        }

        public void f_setwd()
        {
            string dir = getStringArgument("dir");
            try
            {
                Directory.SetCurrentDirectory(dir);
                response.Add("Setting working directory: " + dir);
            }
            catch (Exception e)
            {
                response.Add("!Error: " + e.Message);
            }
        }

        public void f_dir()
        {
            string[] dirs = Directory.GetDirectories(Directory.GetCurrentDirectory(),"*",SearchOption.TopDirectoryOnly);
            string[] files = Directory.GetFiles(Directory.GetCurrentDirectory());
            response.Add("'" + Directory.GetCurrentDirectory() + "':");
            foreach (string dir in dirs)
                response.Add(" /" + Path.GetFileName(dir) + "/");
            foreach (string file in files)
            {
                response.Add(" " + Path.GetFileName(file));
            }
        }
        public void f_loadmatrix()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), "matrix", getStringArgument("name")));
        }

        public void f_loadtable()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), "table", getStringArgument("name")));
        }

        public void f_load()
        {
            response.Add(SocnetIO.LoadDataStructure(response, dataset, getStringArgument("file"), getStringArgument("type"), getStringArgument("name")));
        }

        public void f_save()
        {
            response.Add(SocnetIO.SaveDataStructure(response, dataset, getStringArgument("name"), getStringArgument("file")));
        }

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

            // to-do

        }

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

        public void f_delete()
        {
            DataStructure? structure = dataset.GetStructureByName(getStringArgument("name"));
            if (structure != null)
                response.Add(dataset.DeleteStructure(structure));
            else
                response.Add("!Error: Structure '" + getStringArgument("name") + "' not found");
        }

        public void f_deleteall()
        {
            response.Add(dataset.DeleteAllStructures());

        }

        public void f_structures()
        {
            response.Add("DataType" + "\t" + "Name" + "\t" + "Size");
            response.Add("========" + "\t" + "====" + "\t" + "====");
            string type = getStringArgument("type");
            if (type == "")
                foreach (KeyValuePair<string, DataStructure> obj in dataset.structures)
                    response.Add(obj.Key+"\t"+ obj.Value.DataType + "\t" + obj.Value.Name + "\t" + obj.Value.Size);
            else
                foreach (KeyValuePair<string, DataStructure> obj in dataset.structures)
                    if (obj.Value.GetType().Name.Equals(type,StringComparison.CurrentCultureIgnoreCase))
                        response.Add(obj.Value.DataType + "\t" + obj.Value.Name + "\t" + obj.Value.Size);
        }

        public void f_view()
        {
            string name = getStringArgument("name");
            DataStructure? structure = dataset.GetStructureByName(name);
            if (structure == null) {
                response.Add("!Error: Structure '" + name + "' not found");
                return;
            }
            response.AddRange(structure.View);
        }


        public BlockImage? f_blockimage()
        {
            int nbrPositions = getIntegerArgument("size");
            if (nbrPositions<2)
            {
                response.Add("!Error: Blockimage size must be at least 2");
                return null;
            }
            BlockImage bi = new BlockImage("", nbrPositions);
            string pattern = getStringArgument("pattern"), content = getStringArgument("content");
            if (pattern != "")
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

        public void f_coreperi()
        {
            response.Add("Init and running corr-based core-peri");
            Dictionary<string, object?> searchParams = new Dictionary<string, object?>();

            DataStructure? network = dataset.GetStructureByName(getStringArgument("network"), typeof(Matrix));
            if (network == null)
            {
                response.Add("!Error: Network not found (parameter: network)");
                return;
            }

            string searchType = getStringArgument("searchtype");
            if (searchType == "" || !Blockmodeling.searchTypes.Contains(searchType))
            {
                response.Add("!Error: Search type not recognized/set (parameter: searchtype");
                return;
            }

            BlockImage cpbi = new BlockImage("cp", 2);
            cpbi.setPositionName(0, "C");
            cpbi.setPositionName(1, "P");
            cpbi.setBlockByPattern(1, 1, "nul");
            string core = getStringArgument("core");
            cpbi.setBlockByPattern(0, 0, (core.Length>2 && core.Substring(0, 3).Equals("pco")) ? core : "com");
            
            string intercat = getStringArgument("intercat");
            if (intercat !="")
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
                if (ctop!= "")
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

            string statusInitMsg = Blockmodeling.InitializeSearch(searchParams);
            if (statusInitMsg.Equals("ok"))
                f_startsearch();
            else if (statusInitMsg[0] == '!')
                response.Add(statusInitMsg);
            Blockmodeling.logLines.Clear();


        }

        public void f_initdirectbm()
        {
            response.Add("Doing direct blockmodeling");
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
            if (searchType =="" || !Blockmodeling.searchTypes.Contains(searchType))
            {
                response.Add("!Error: Search type not recognized/set (parameter: searchtype");
                return;
            }
            string gofMethod = getStringArgument("method");
            if (gofMethod == "" || !Blockmodeling.gofMethods.Contains(gofMethod))
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

            string statusInitMsg = Blockmodeling.InitializeSearch(searchParams);
            if (statusInitMsg.Equals("ok"))
                response.AddRange(Blockmodeling.logLines);
            else if (statusInitMsg[0] == '!')
                response.Add(statusInitMsg);
            Blockmodeling.logLines.Clear();
        }

        public void f_startsearch()
        {
            string status = Blockmodeling.StartSearch();
            if (status.Equals("ok"))
            {
                response.Add("Execution time (ms):" + Blockmodeling.stopwatch.ElapsedMilliseconds);
                List<BlockModel> blockmodels = Blockmodeling.generateBlockmodelStructuresFromBMSolutions();
                foreach (BlockModel bm in blockmodels)
                    response.Add(dataset.StoreStructure(bm));
            }
            else if (status.Equals("timeout"))
            {
                response.AddRange(Blockmodeling.logLines);
            }
        }

        public void f_viewbm()
        {
            BlockModel bm;
            string name = getStringArgument("blockmodel");
            if (name.Length==0)
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
            response.Add("Blockmodel:");
            response.AddRange(bm.DisplayBlockmodel());
            response.Add("Blockimage:");
            response.AddRange(bm.DisplayBlockimage());
            response.Add("Goodness-of-fit: " + bm.gof + " (" + bm.gofMethod + ")");
        }

        public void f_bmextract()
        {
            // "blockmodel", "type"

            DataStructure? blockmodel = dataset.GetStructureByName(getStringArgument("blockmodel"), typeof(BlockModel));
            if (blockmodel == null)
            {
                response.Add("!Error: Blockmodel not found");
                return;
            }
            string type = getStringArgument("type");
            if (type.Equals("blockimage"))
            {
                BlockImage bi = ((BlockModel)blockmodel).ExtractBlockimage();
                //bi.Name = dataset.GetAutoName(bi.Name);
                response.Add(dataset.StoreStructure(bi));
            }
            else if (type.Equals("matrix"))
            {
                Matrix bmMatrix = ((BlockModel)blockmodel).GetBlockModelMatrix();
                response.Add(dataset.StoreStructure(bmMatrix.actorset));
                response.Add(dataset.StoreStructure(bmMatrix));
            }
            else if (type.Equals("partition"))
            {

            }
                


        }



        public void f_getbmlog()
        {
            response.AddRange(Blockmodeling.logLines);
        }



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

        public DataStructure? f_dichotomize()
        {
            response.Add("Dichotomization");
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
    }
}
