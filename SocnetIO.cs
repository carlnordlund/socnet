using Socnet.DataLibrary;

namespace Socnet
{
    /// <summary>
    /// Static class for doing IO operations for Socnet.se
    /// </summary>
    public static class SocnetIO
    {
        // Characters for removing quotes
        public static char[] quotechars = new char[] { '"', '\'' };

        /// <summary>
        /// Method for loading an individual DataStructure object as a text file
        /// </summary>
        /// <param name="response">List of strings where details can be added</param>
        /// <param name="dataset">Dataset object from where to get DataStructure</param>
        /// <param name="name">Name of DataStructure</param>
        /// <param name="filepath">Filepath where to save it</param>
        /// <param name="sep">Character to separate data fields</param>
        /// <returns>Status text</returns>
        internal static string SaveDataStructure(List<string> response, Dataset dataset, string name, string filepath, string sep = "\t")
        {
            DataStructure? structure = dataset.GetStructureByName(name);
            if (structure == null)
                return "!Error: Structure '" + name + "' not found";
            if (structure is Matrix)
                return SaveMatrix((Matrix)structure, filepath, sep);
            if (structure is BlockImage)
                return SaveBlockImage((BlockImage)structure, filepath, sep);
            if (structure is Partition)
                return SavePartition((Partition)structure, filepath, sep);

            return "error - structure type not implemented";
        }

        /// <summary>
        /// Method for saving BlockImage object
        /// </summary>
        /// <param name="blockimage">BlockImage object</param>
        /// <param name="filepath">Filepath where to save to</param>
        /// <param name="sep">Character to separate data fields</param>
        /// <returns>Status text</returns>
        private static string SaveBlockImage(BlockImage blockimage, string filepath, string sep)
        {
            if (blockimage == null || blockimage.blocks == null)
                return "!Error: Blockimage (or its blocks) is null";
            int nbrPositions = blockimage.nbrPositions;
            string[,] filecells = new string[nbrPositions + 1, nbrPositions + 1];
            filecells[0, 0] = "";
            for (int r = 0; r < nbrPositions; r++)
            {
                filecells[0, r + 1] = blockimage.positionNames[r];
                filecells[r + 1, 0] = blockimage.positionNames[r];
                for (int c = 0; c < nbrPositions; c++)
                {
                    if (blockimage.blocks[r, c] != null)
                        filecells[r + 1, c + 1] = String.Join(";", blockimage.blocks[r, c]);
                    else
                        filecells[r + 1, c + 1] = "";
                }
            }
            if (WriteFileCells(filecells, filepath, sep))
                return "Blockimage '" + blockimage.Name + "' saved: " + filepath;
            else
                return "!Error: Could not save Blockimage file";
        }

        /// <summary>
        /// Method for saving Partition object
        /// </summary>
        /// <param name="partition">Partition object</param>
        /// <param name="filepath">Filepath where to save to</param>
        /// <param name="sep">Character to separate data fields</param>
        /// <returns></returns>
        private static string SavePartition(Partition partition, string filepath, string sep)
        {
            if (partition == null)
                return "!Error: Partition is null";
            int size = partition.actorset.Count;
            string[,] filecells = new string[size + 1, 2];
            filecells[0, 0] = "";
            filecells[0, 1] = "partindex";
            for (int c = 0; c < partition.clusters.Length; c++)
            {
                foreach (Actor actor in partition.clusters[c].actors)
                {
                    filecells[actor.index + 1, 0] = actor.Name;
                    filecells[actor.index + 1, 1] = c.ToString();
                }
            }
            if (WriteFileCells(filecells, filepath, sep))
                return "Partition '" + partition.Name + "' saved: " + filepath;
            else
                return "!Error: Could not save partition '" + partition.Name + "' to file";

        }

        /// <summary>
        /// Method for saving BlockModel object in the form of a JSON string
        /// This can subsequently be loaded into R as a data frame
        /// </summary>
        /// <param name="blockmodel">BlockModel object to save</param>
        /// <param name="file">File path to JSON text file</param>
        /// <returns>Status text</returns>
        //internal static string SaveBlockModel(BlockModel blockmodel, string file)
        //{
        //    Matrix bmMatrix = blockmodel.bmMatrix;
        //    Actorset bmActorset = bmMatrix.actorset;
        //    string actorLabels = "[" + string.Join(',', bmActorset.GetActorLabelArray("\"")) + "]";
        //    int nbrActors = bmActorset.Count;
        //    string partstring = blockmodel.partition.GetPartString(",");
        //    BlockImage bi = blockmodel.blockimage;
        //    int nbrPosBi = bi.nbrPositions;
        //    double gof = blockmodel.gof;
        //    string gofmethod = blockmodel.gofMethod;

        //    string json = @"{""type"":""list"",""attributes"":{""names"":{""type"":""character"",""attributes"":{},""value"":[""matrix"",""partition"",""blockimage"",""gof"",""gofmethod""]}},""value"":[{""type"":""double"",""attributes"":{""dim"":{""type"":""integer"",""attributes"":{},""value"":";

        //    json += "[" + nbrActors + "," + nbrActors + "]";

        //    json += @"},""dimnames"":{""type"":""list"",""attributes"":{},""value"":[{""type"":""character"",""attributes"":{},""value"":";

        //    json += actorLabels;

        //    json += @"},{""type"":""character"",""attributes"":{},""value"":";

        //    json += actorLabels;

        //    json += @"}]}},""value"":";

        //    double[,] matrix2dArray = new double[nbrActors, nbrActors];
        //    foreach (Actor rowActor in bmActorset.actors)
        //        foreach (Actor colActor in bmActorset.actors)
        //            matrix2dArray[rowActor.index, colActor.index] = bmMatrix.Get(rowActor, colActor);
        //    double[] matrix1dArray = new double[nbrActors * nbrActors];
        //    int index = 0;
        //    for (int c = 0; c < nbrActors; c++)
        //        for (int r = 0; r < nbrActors; r++)
        //        {
        //            matrix1dArray[index] = matrix2dArray[r, c];
        //            index++;
        //        }
        //    json += "[" + string.Join(',', matrix1dArray) + "]";

        //    json += @"},{""type"":""integer"",""attributes"":{},""value"":";

        //    json += "[" + partstring + "]";

        //    json += @"},{""type"":""character"",""attributes"":{""dim"":{""type"":""integer"",""attributes"":{},""value"":";

        //    json += "[" + nbrPosBi + "," + nbrPosBi + "]";

        //    json += @"}},""value"":";

        //    List<string> biContent = new List<string>();
        //    List<string> cellStuff = new List<string>();
        //    for (int c = 0; c < nbrPosBi; c++)
        //        for (int r = 0; r < nbrPosBi; r++)
        //        {
        //            cellStuff.Clear();
        //            for (int i = 0; i < bi.blocks![r, c].Count; i++)
        //            {
        //                cellStuff.Add(bi.blocks![r, c][i].Name);
        //            }
        //            string stuff = string.Join(';', cellStuff);
        //            biContent.Add("\"" + stuff + "\"");
        //        }
        //    json += "[" + string.Join(',', biContent) + "]";
        //    json += @"},{""type"":""double"",""attributes"":{},""value"":";
        //    json += "[" + gof + "]";
        //    json += @"},{""type"":""character"",""attributes"":{},""value"":";
        //    json += "[\"" + gofmethod + "\"]";
        //    json += @"}]}";

        //    try
        //    {
        //        File.WriteAllText(file, json);
        //    }
        //    catch (Exception e)
        //    {
        //        return "!Error: Could not save file '" + file + "'; " + e.Message;
        //    }
        //    return "Saved BlockModel to '" + file + "' as JSON for R (use library 'jsonlite' and 'unserializeJSON()' to get this into a useful R object)";
        //}

        /// <summary>
        /// Method to save Matrix object
        /// </summary>
        /// <param name="matrix">Matrix object to save</param>
        /// <param name="filepath">Filepath where to save to</param>
        /// <param name="sep">Character to separate data fields</param>
        /// <returns>Status text</returns>
        private static string SaveMatrix(Matrix matrix, string filepath, string sep)
        {
            if (matrix == null)
                return "!Error: Matrix is null";
            int size = matrix.actorset.Count;
            string[,] filecells = new string[size + 1, size + 1];
            filecells[0, 0] = "";
            foreach (Actor rowActor in matrix.actorset.actors)
            {
                filecells[0, rowActor.index + 1] = rowActor.Name;
                filecells[rowActor.index + 1, 0] = rowActor.Name;
                foreach (Actor colActor in matrix.actorset.actors)
                    filecells[rowActor.index + 1, colActor.index + 1] = matrix.Get(rowActor, colActor).ToString();
            }
            if (WriteFileCells(filecells, filepath, sep))
                return "Matrix '" + matrix.Name + "' saved: " + filepath;
            else
                return "!Error: Could not save matrix '" + matrix.Name + "' file";
        }

        /// <summary>
        /// Internal method for writing 2d array to file
        /// </summary>
        /// <param name="filecells">2d array of strings with content</param>
        /// <param name="filepath">File path to save to</param>
        /// <param name="sep">Character to separate data fields</param>
        private static bool WriteFileCells(string[,] filecells, string filepath, string sep)
        {
            int nbrRows = filecells.GetLength(0);
            int nbrCols = filecells.GetLength(1);
            List<string> csvLines = new List<string>();
            string line = "";
            for (int r = 0; r < nbrRows; r++)
            {
                line = "";
                for (int c = 0; c < nbrCols - 1; c++)
                    line += filecells[r, c] + sep;
                line += filecells[r, nbrCols - 1];
                csvLines.Add(line);
            }
            try
            {
                File.WriteAllLines(filepath, csvLines);
                return true;
            }
            catch (IOException)
            {
                return false;
            }
            
        }

        /// <summary>
        /// Method for loading network (Matrix object) from edgelist file
        /// </summary>
        /// <param name="response">List of strings for storing response log</param>
        /// <param name="dataset">Dataset to store to</param>
        /// <param name="filepath">File path to edgelist to load</param>
        /// <param name="col1">Index of column for Actor 1 (From actor for directional ties)</param>
        /// <param name="col2">Index of column for Actor 2 (To actor for directional ties)</param>
        /// <param name="symmetric">String to indicate whether ties are symmetric ("yes") or directional ("no")</param>
        /// <param name="actorsetName">Name of Actorset, either an existing one or name of new one to be created</param>
        /// <param name="colval">Index of column with edge values</param>
        /// <param name="headers">String to indicate whether first line contains headers ("yes" if so)</param>
        /// <param name="sep">Character that separates data fields in the raw edgelist file</param>
        /// <returns>Status text</returns>
        internal static string LoadEdgelist(List<string> response, Dataset dataset, string filepath, int col1, int col2, string symmetric, string actorsetName, int colval, string headers, string sep)
        {
            if (!File.Exists(filepath))
                return "!Error: File '" + filepath + "' not found";
            string[] lines = File.ReadAllLines(filepath);
            if (lines.Length == 0)
                return "!Error: File '" + filepath + "' is empty";
            string dsName = Path.GetFileNameWithoutExtension(filepath);
            sep = (sep.Length == 0) ? "\t" : sep;
            int startline = (headers.Length > 0 && headers.ToLower()[0] == 'n') ? 0 : 1;
            bool symm = (symmetric.Length > 0 && symmetric.ToLower()[0] == 'y');
            Actorset? actorset;
            bool storeActorset = false;
            string[] cells;
            col1--;
            col2--;
            colval--;
            try
            {
                if (actorsetName.Length > 0)
                {
                    DataStructure? actorsetPrev = dataset.GetStructureByName(actorsetName, typeof(Actorset));
                    if (actorsetPrev == null)
                        return "!Error: Specified actorset '" + actorsetName + "' not found";
                    actorset = (Actorset)actorsetPrev;
                    response.Add("Reusing previous actorset: '" + actorsetName + "'");
                }
                else
                {
                    List<string> actorLabels = new List<string>();
                    for (int i = startline; i < lines.Length; i++)
                    {
                        cells = lines[i].Split(sep);
                        if (!actorLabels.Contains(cells[col1]))
                            actorLabels.Add(cells[col1]);
                        if (!actorLabels.Contains(cells[col2]))
                            actorLabels.Add(cells[col2]);
                    }
                    actorset = dataset.CreateActorsetByLabels(actorLabels.ToArray());
                    if (actorset == null)
                        return "!Error: Something went wrong when creating actorset";
                    response.Add("Creating new actorset: " + actorset.Count);
                    storeActorset = true;
                }
                Matrix matrix = new Matrix(actorset, dsName, "F2");
                Actor? a1, a2;
                double val;
                for (int i = startline; i < lines.Length; i++)
                {
                    cells = lines[i].Split(sep);
                    a1 = actorset.GetActorByLabel(cells[col1]);
                    a2 = actorset.GetActorByLabel(cells[col2]);
                    if (a1 == null || a2 == null)
                    {
                        response.Add("!Error: Actor label not found (either '" + cells[col1] + "' or '" + cells[col2] + "')");
                        continue;
                    }

                    val = (colval > 0) ? double.Parse(cells[colval]) : 1;
                    matrix.Set(a1, a2, val);
                    if (symm)
                        matrix.Set(a2, a1, val);
                }
                if (storeActorset)
                    response.Add(dataset.StoreStructure(actorset));
                response.Add(dataset.StoreStructure(matrix));
                return "Loading edgelist: OK";
            }
            catch (Exception e)
            {
                return "!Error: " + e.Message;
            }
        }

        /// <summary>
        /// Method to load DataStructure object
        /// </summary>
        /// <param name="response">List of response strings</param>
        /// <param name="dataset">Dataset object to store to</param>
        /// <param name="filepath">File path to file to load</param>
        /// <param name="type">Type to load: "matrix", "table", "partition", "blockimage"</param>
        /// <param name="name">Name of DataStructure</param>
        /// <param name="sep">Character to separate data fields</param>
        /// <returns>Status text</returns>
        internal static string LoadDataStructure(List<string> response, Dataset dataset, string filepath, string type, string name, string sep = "\t")
        {
            try
            {
                if (!File.Exists(filepath))
                    return "!Error: File '" + filepath + "' not found";
                string[]? lines = readAllLines(filepath, response);
                if (lines == null)
                    return "!Error: File '" + filepath + "' seems empty";
                //string filename = Path.GetFileNameWithoutExtension(filepath);
                string dsname = (name.Length == 0) ? Path.GetFileNameWithoutExtension(filepath) : name;
                char sepchar = (sep.Length == 1) ? sep[0] : '\t';

                if (type.Equals("matrix"))
                {
                    ActorsAndData aod = parseActorsAndData(lines, sepchar);
                    if (aod.error)
                        return aod.errorMsg;
                    if (aod.rowLabels.Length != aod.colLabels.Length)
                        return "!Error: Label size mismatch for rows and columns";
                    for (int i = 0; i < aod.rowLabels.Length; i++)
                        if (!aod.rowLabels[i].Equals(aod.colLabels[i]))
                            return "!Error: Matrix label mismatch: '" + aod.rowLabels[i] + "' vs '" + aod.colLabels[i] + "'";
                    Actorset? actorset = dataset.GetActorsetByLabels(aod.rowLabels);
                    if (actorset == null)
                    {
                        actorset = dataset.CreateActorsetByLabels(aod.rowLabels);
                        if (actorset == null)
                            return "!Error: Couldn't create Actorset from labels";
                        actorset.Name = dsname + "_actors";
                        response.Add(dataset.StoreStructure(actorset));
                    }
                    Matrix matrix = new Matrix(actorset, dsname, "F2");
                    matrix.installData(aod.rowLabels, aod.data);
                    response.Add(dataset.StoreStructure(matrix));
                }
                else if (type.Equals("table"))
                {
                    ActorsAndData aod = parseActorsAndData(lines, sepchar);
                    if (aod.error)
                        return aod.errorMsg;
                    Actorset? rowActorset = dataset.GetActorsetByLabels(aod.rowLabels);
                    Actorset? colActorset = dataset.GetActorsetByLabels(aod.colLabels);
                    //bool addRowActorset = false, addColActorset = false;
                    if (rowActorset == null)
                    {
                        // Doesn't exist, so try creating
                        rowActorset = dataset.CreateActorsetByLabels(aod.rowLabels);
                        if (rowActorset == null)
                            // Nope, didn't work - give error and abort
                            return "!Error: Couldn't create Actorset from row labels";
                        // If I am here, this means I got a rowActorset, which might or might not already be stored
                        response.Add(dataset.StoreStructure(rowActorset));
                        //addRowActorset = true;
                    }
                    if (colActorset == null)
                    {
                        // Doesn't exist, so try creating
                        colActorset = dataset.CreateActorsetByLabels(aod.colLabels);
                        if (colActorset == null)
                            // Nope, didn't work - give error and abort
                            return "!Error: Couldn't create Actorset from column labels";
                        // If I am here, this means I got a colActorset, which might or might not already be stored
                        response.Add(dataset.StoreStructure(colActorset));
                    }
                    Table table = new Table(rowActorset, colActorset, dsname, "F2");
                    table.installData(aod.rowLabels, aod.colLabels, aod.data);
                    response.Add(dataset.StoreStructure(table));
                }
                else if (type.Equals("partition"))
                {
                    ActorsAndData aod = parseActorsAndData(lines, sepchar);
                    if (aod.error)
                        return aod.errorMsg;
                    Actorset? actorset = dataset.GetActorsetByLabels(aod.rowLabels);
                    if (actorset == null)
                    {
                        actorset = dataset.CreateActorsetByLabels(aod.rowLabels);
                        if (actorset == null)
                            return "!Error: Couldn't create Actorset from first row labels";
                        response.Add(dataset.StoreStructure(actorset));
                    }
                    Partition partition = new Partition(actorset, dsname);
                    int[] partArray = new int[actorset.Count];
                    int maxIndex = -1;
                    for (int r = 0; r < actorset.Count; r++)
                    {
                        partArray[r] = (int)aod.data[r, 0];
                        if (partArray[r] < 0)
                            return "!Error: Cluster index can't be negative";
                        maxIndex = (partArray[r] > maxIndex) ? partArray[r] : maxIndex;
                    }
                    partition.createClusters(maxIndex + 1);
                    partition.setPartitionByPartArray(partArray);
                    response.Add(dataset.StoreStructure(partition));
                }
                else if (type.Equals("blockimage"))
                {
                    string[] positionNames = lines[0].TrimStart(sepchar).Split(sepchar);
                    int nbrPositions = positionNames.Length;
                    if (lines.Length - 1 != nbrPositions)
                    {
                        return "!Error: Row/col mismatch for blockimage file";
                    }
                    BlockImage blockimage = new BlockImage(dsname, nbrPositions);
                    for (int r = 0; r < nbrPositions; r++)
                    {
                        blockimage.positionNames[r] = positionNames[r];
                        string[] cells = lines[r + 1].Split('\t');
                        for (int c = 0; c < nbrPositions; c++)
                            blockimage.setBlockByPattern(r, c, cells[c + 1]);
                        // Do rest here...
                    }
                    blockimage.checkMultiblocked();
                    response.Add(dataset.StoreStructure(blockimage));
                }
                else
                    return "!Error: Type '" + type + "' not recognized";
                return "Loading data structure: OK";
            }
            catch (Exception e)
            {
                return "!Error: " + e.Message;
            }
        }

        /// <summary>
        /// Internal method for parsing Actors and Data from array of strings
        /// </summary>
        /// <param name="lines">Array of strings from loading text file</param>
        /// <param name="separator">Character to separate data fields</param>
        /// <returns>Returns ActorsAndData struct</returns>
        private static ActorsAndData parseActorsAndData(string[] lines, char separator = '\t')
        {
            ActorsAndData actorsAndData = new ActorsAndData();
            try
            {
                string[] colLabels = lines[0].TrimStart(separator).Split(separator);
                stripQuotes(colLabels);
                int nbrCols = colLabels.Length;
                int nbrRows = lines.Length - 1;
                string[] rowLabels = new string[nbrRows];
                double[,] data = new double[nbrRows, nbrCols];
                for (int r = 0; r < nbrRows; r++)
                {
                    string[] cells = lines[r + 1].Split(separator);
                    rowLabels[r] = cells[0];
                    for (int c = 0; c < nbrCols; c++)
                        Double.TryParse(cells[c + 1], out data[r, c]);
                }
                stripQuotes(rowLabels);
                actorsAndData.rowLabels = rowLabels;
                actorsAndData.colLabels = colLabels;
                actorsAndData.data = data;
            }
            catch (Exception e)
            {
                actorsAndData.error = true;
                actorsAndData.errorMsg = "!Error: " + e.Message;
            }
            return actorsAndData;
        }

        private static void stripQuotes(string[] colLabels)
        {
            for (int i = 0; i < colLabels.Length; i++)
                colLabels[i] = colLabels[i].Trim(quotechars);
        }

        public static string[]? readAllLines(string filename, List<string> response)
        {
            try
            {
                if (File.Exists(filename))
                    return File.ReadAllLines(filename);
                response.Add("!Error: File not found");
            }
            catch (FileNotFoundException e)
            {
                response.Add("!Error: " + e.Message);
            }
            return null;
        }
    }

    /// <summary>
    /// Struct for storing 'rectangular' data with vectors for row- and column labels.
    /// Used when loading/saving data structures
    /// </summary>
    public struct ActorsAndData
    {
        public double[,] data;
        public string[] rowLabels, colLabels;
        public bool error = false;
        public string errorMsg = "";

        public ActorsAndData(double[,] data, string[] rowLabels, string[] colLabels)
        {
            this.data = data;
            this.rowLabels = rowLabels;
            this.colLabels = colLabels;
        }
    }
}
