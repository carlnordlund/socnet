using Socnet.Core.Model;
using Socnet.Core.Utilities;

namespace Socnet.Core.IO
{
    /// <summary>
    /// Loading and saving of data structures as text files. Methods return a status message, starting
    /// with '!' if something went wrong; additional messages are added to the response list.
    /// </summary>
    public static class SocnetIO
    {
        private static readonly char[] QuoteChars = ['"', '\''];

        #region Saving
        /// <summary>
        /// Saves a Matrix, BlockImage, Partition or Actorset to a text file.
        /// </summary>
        public static string SaveDataStructure(Dataset dataset, string name, string filepath, string sep = "\t")
        {
            DataStructure? structure = dataset.GetStructureByName(name);
            return structure switch
            {
                null => $"!Error: Structure '{name}' not found",
                Matrix matrix => SaveMatrix(matrix, filepath, sep),
                BlockImage blockimage => SaveBlockImage(blockimage, filepath, sep),
                Partition partition => SavePartition(partition, filepath, sep),
                Actorset actorset => SaveActorset(actorset, filepath, sep),
                _ => "error - structure type not implemented"
            };
        }

        private static string SaveBlockImage(BlockImage blockimage, string filepath, string sep)
        {
            int k = blockimage.NbrPositions;
            string[,] cells = new string[k + 1, k + 1];
            cells[0, 0] = "";
            for (int r = 0; r < k; r++)
            {
                cells[0, r + 1] = blockimage.PositionNames[r];
                cells[r + 1, 0] = blockimage.PositionNames[r];
                for (int c = 0; c < k; c++)
                    cells[r + 1, c + 1] = string.Join(";", blockimage.Blocks(r, c));
            }
            return WriteFileCells(cells, filepath, sep)
                ? $"Blockimage '{blockimage.Name}' saved: {filepath}"
                : "!Error: Could not save Blockimage file";
        }

        private static string SaveActorset(Actorset actorset, string filepath, string sep)
        {
            string[,] cells = new string[actorset.Count, 1];
            for (int i = 0; i < actorset.Count; i++)
                cells[i, 0] = actorset.Label(i);
            return WriteFileCells(cells, filepath, sep)
                ? $"Actorset '{actorset.Name}' saved: {filepath}"
                : $"!Error: Could not save actorset '{actorset.Name}' to file";
        }

        private static string SavePartition(Partition partition, string filepath, string sep)
        {
            int n = partition.Actorset.Count;
            string[,] cells = new string[n + 1, 2];
            cells[0, 0] = "";
            cells[0, 1] = "partindex";
            for (int i = 0; i < n; i++)
            {
                cells[i + 1, 0] = partition.Actorset.Label(i);
                cells[i + 1, 1] = partition.ClusterOf[i].ToString();
            }
            return WriteFileCells(cells, filepath, sep)
                ? $"Partition '{partition.Name}' saved: {filepath}"
                : $"!Error: Could not save partition '{partition.Name}' to file";
        }

        private static string SaveMatrix(Matrix matrix, string filepath, string sep)
        {
            int n = matrix.N;
            string[,] cells = new string[n + 1, n + 1];
            cells[0, 0] = "";
            for (int r = 0; r < n; r++)
            {
                cells[0, r + 1] = matrix.Actorset.Label(r);
                cells[r + 1, 0] = matrix.Actorset.Label(r);
                for (int c = 0; c < n; c++)
                    cells[r + 1, c + 1] = Fmt.D(matrix[r, c]);
            }
            return WriteFileCells(cells, filepath, sep)
                ? $"Matrix '{matrix.Name}' saved: {filepath}"
                : $"!Error: Could not save matrix '{matrix.Name}' file";
        }

        private static bool WriteFileCells(string[,] cells, string filepath, string sep)
        {
            int nbrRows = cells.GetLength(0), nbrCols = cells.GetLength(1);
            List<string> lines = new(nbrRows);
            for (int r = 0; r < nbrRows; r++)
            {
                string[] row = new string[nbrCols];
                for (int c = 0; c < nbrCols; c++)
                    row[c] = cells[r, c];
                lines.Add(string.Join(sep, row));
            }
            try
            {
                File.WriteAllLines(filepath, lines);
                return true;
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                return false;
            }
        }
        #endregion

        #region Loading
        /// <summary>
        /// Loads a network from an edgelist file and stores it as a Matrix (and a new Actorset if needed).
        /// </summary>
        /// <param name="response">List to which status messages are added.</param>
        /// <param name="dataset">The dataset to store in.</param>
        /// <param name="filepath">Path to the edgelist file.</param>
        /// <param name="col1">Column (1-based) with the first (from) actor.</param>
        /// <param name="col2">Column (1-based) with the second (to) actor.</param>
        /// <param name="symmetric">'yes' if edges are symmetric.</param>
        /// <param name="actorsetName">Name of an existing Actorset to use (optional).</param>
        /// <param name="colval">Column (1-based) with edge values (optional; otherwise 1).</param>
        /// <param name="headers">'no' if the first line is not a header line.</param>
        /// <param name="sep">Field separator (default tab).</param>
        public static string LoadEdgelist(List<string> response, Dataset dataset, string filepath, int col1, int col2, string symmetric, string actorsetName, int colval, string headers, string sep)
        {
            if (!File.Exists(filepath))
                return $"!Error: File '{filepath}' not found";
            string[] lines = File.ReadAllLines(filepath);
            if (lines.Length == 0)
                return $"!Error: File '{filepath}' is empty";
            string dsName = Path.GetFileNameWithoutExtension(filepath);
            sep = sep.Length == 0 ? "\t" : sep;
            int startline = (headers.Length > 0 && char.ToLower(headers[0]) == 'n') ? 0 : 1;
            bool symm = symmetric.Length > 0 && char.ToLower(symmetric[0]) == 'y';
            col1--;
            col2--;
            colval--;
            try
            {
                Actorset? actorset;
                bool storeActorset = false;
                if (actorsetName.Length > 0)
                {
                    actorset = dataset.Get<Actorset>(actorsetName);
                    if (actorset == null)
                        return $"!Error: Specified actorset '{actorsetName}' not found";
                    response.Add($"Reusing previous actorset: '{actorsetName}'");
                }
                else
                {
                    List<string> labels = [];
                    HashSet<string> seen = [];
                    for (int i = startline; i < lines.Length; i++)
                    {
                        string[] cells = lines[i].Split(sep);
                        if (seen.Add(cells[col1]))
                            labels.Add(cells[col1]);
                        if (seen.Add(cells[col2]))
                            labels.Add(cells[col2]);
                    }
                    actorset = dataset.CreateActorsetByLabels(labels);
                    if (actorset == null)
                        return "!Error: Something went wrong when creating actorset";
                    response.Add("Creating new actorset: " + actorset.Count);
                    storeActorset = true;
                }
                Matrix matrix = new(actorset, dsName);
                for (int i = startline; i < lines.Length; i++)
                {
                    string[] cells = lines[i].Split(sep);
                    if (!actorset.TryGetIndex(cells[col1], out int a1) || !actorset.TryGetIndex(cells[col2], out int a2))
                    {
                        response.Add($"!Error: Actor label not found (either '{cells[col1]}' or '{cells[col2]}')");
                        continue;
                    }
                    double val = 1;
                    if (colval >= 0 && !Fmt.TryParseDouble(cells[colval], out val))
                        throw new FormatException($"The input string '{cells[colval]}' was not in a correct format.");
                    matrix[a1, a2] = val;
                    if (symm)
                        matrix[a2, a1] = val;
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
        /// Loads a data structure from a text file.
        /// </summary>
        /// <param name="response">List to which status messages are added.</param>
        /// <param name="dataset">The dataset to store in.</param>
        /// <param name="filepath">Path to the file.</param>
        /// <param name="type">Type to load: 'actorset', 'matrix', 'table', 'partition' or 'blockimage'.</param>
        /// <param name="name">Name of the loaded structure (optional; otherwise the file name).</param>
        /// <param name="sep">Field separator (default tab).</param>
        public static string LoadDataStructure(List<string> response, Dataset dataset, string filepath, string type, string name, string sep = "\t")
        {
            try
            {
                if (!File.Exists(filepath))
                    return $"!Error: File '{filepath}' not found";
                string[]? lines = ReadAllLines(filepath, response);
                if (lines == null)
                    return $"!Error: File '{filepath}' seems empty";
                string dsname = name.Length == 0 ? Path.GetFileNameWithoutExtension(filepath) : name;
                char sepchar = sep.Length == 1 ? sep[0] : '\t';

                switch (type)
                {
                    case "actorset":
                        {
                            Actorset? actorset = dataset.CreateActorsetByLabels(lines);
                            if (actorset == null)
                                return $"!Error: Couldn't create actorset from file '{filepath}'";
                            actorset.Name = dsname;
                            response.Add(dataset.StoreStructure(actorset));
                            break;
                        }
                    case "matrix":
                        {
                            ActorsAndData aod = ParseActorsAndData(lines, sepchar);
                            if (aod.ErrorMsg != null)
                                return aod.ErrorMsg;
                            if (aod.RowLabels.Length != aod.ColLabels.Length)
                                return "!Error: Label size mismatch for rows and columns";
                            for (int i = 0; i < aod.RowLabels.Length; i++)
                                if (!aod.RowLabels[i].Equals(aod.ColLabels[i]))
                                    return $"!Error: Matrix label mismatch: '{aod.RowLabels[i]}' vs '{aod.ColLabels[i]}'";
                            Actorset? actorset = dataset.GetActorsetByLabels(aod.RowLabels);
                            if (actorset == null)
                            {
                                actorset = dataset.CreateActorsetByLabels(aod.RowLabels);
                                if (actorset == null)
                                    return "!Error: Couldn't create Actorset from labels";
                                actorset.Name = dsname + "_actors";
                                response.Add(dataset.StoreStructure(actorset));
                            }
                            Matrix matrix = new(actorset, dsname);
                            int[] idx = MapLabels(actorset, aod.RowLabels);
                            for (int r = 0; r < idx.Length; r++)
                                for (int c = 0; c < idx.Length; c++)
                                    matrix[idx[r], idx[c]] = aod.Data[r, c];
                            response.Add(dataset.StoreStructure(matrix));
                            break;
                        }
                    case "table":
                        {
                            ActorsAndData aod = ParseActorsAndData(lines, sepchar);
                            if (aod.ErrorMsg != null)
                                return aod.ErrorMsg;
                            Actorset? rowActorset = dataset.GetActorsetByLabels(aod.RowLabels);
                            Actorset? colActorset = dataset.GetActorsetByLabels(aod.ColLabels);
                            if (rowActorset == null)
                            {
                                rowActorset = dataset.CreateActorsetByLabels(aod.RowLabels);
                                if (rowActorset == null)
                                    return "!Error: Couldn't create Actorset from row labels";
                                response.Add(dataset.StoreStructure(rowActorset));
                            }
                            if (colActorset == null)
                            {
                                colActorset = dataset.CreateActorsetByLabels(aod.ColLabels);
                                if (colActorset == null)
                                    return "!Error: Couldn't create Actorset from column labels";
                                response.Add(dataset.StoreStructure(colActorset));
                            }
                            Table table = new(rowActorset, colActorset, dsname);
                            int[] rowIdx = MapLabels(rowActorset, aod.RowLabels), colIdx = MapLabels(colActorset, aod.ColLabels);
                            for (int r = 0; r < rowIdx.Length; r++)
                                for (int c = 0; c < colIdx.Length; c++)
                                    table.Data[rowIdx[r], colIdx[c]] = aod.Data[r, c];
                            response.Add(dataset.StoreStructure(table));
                            break;
                        }
                    case "partition":
                        {
                            ActorsAndData aod = ParseActorsAndData(lines, sepchar);
                            if (aod.ErrorMsg != null)
                                return aod.ErrorMsg;
                            Actorset? actorset = dataset.GetActorsetByLabels(aod.RowLabels);
                            if (actorset == null)
                            {
                                actorset = dataset.CreateActorsetByLabels(aod.RowLabels);
                                if (actorset == null)
                                    return "!Error: Couldn't create Actorset from first row labels";
                                response.Add(dataset.StoreStructure(actorset));
                            }
                            int[] partArray = new int[actorset.Count];
                            int[] idx = MapLabels(actorset, aod.RowLabels);
                            int maxIndex = -1;
                            for (int r = 0; r < idx.Length; r++)
                            {
                                int cluster = (int)aod.Data[r, 0];
                                if (cluster < 0)
                                    return "!Error: Cluster index can't be negative";
                                partArray[idx[r]] = cluster;
                                maxIndex = Math.Max(maxIndex, cluster);
                            }
                            Partition partition = new(actorset, dsname, Partition.DefaultClusterNames(maxIndex + 1), partArray);
                            response.Add(dataset.StoreStructure(partition));
                            break;
                        }
                    case "blockimage":
                        {
                            string[] positionNames = lines[0].TrimStart(sepchar).Split(sepchar);
                            int k = positionNames.Length;
                            if (lines.Length - 1 != k)
                                return "!Error: Row/col mismatch for blockimage file";
                            BlockImage blockimage = new(dsname, k);
                            for (int r = 0; r < k; r++)
                            {
                                blockimage.PositionNames[r] = positionNames[r];
                                string[] cells = lines[r + 1].Split(sepchar);
                                for (int c = 0; c < k; c++)
                                    blockimage.SetBlock(r, c, cells[c + 1]);
                            }
                            response.Add(dataset.StoreStructure(blockimage));
                            break;
                        }
                    default:
                        return $"!Error: Type '{type}' not recognized";
                }
                return "Loading data structure: OK";
            }
            catch (Exception e)
            {
                return "!Error: " + e.Message;
            }
        }

        /// <summary>
        /// Reads all lines of a text file. Adds an error to the response and returns null if not possible.
        /// </summary>
        public static string[]? ReadAllLines(string filename, List<string> response)
        {
            try
            {
                if (File.Exists(filename))
                    return File.ReadAllLines(filename);
                response.Add("!Error: File not found");
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                response.Add("!Error: " + e.Message);
            }
            return null;
        }

        private static int[] MapLabels(Actorset actorset, string[] labels)
        {
            int[] idx = new int[labels.Length];
            for (int i = 0; i < labels.Length; i++)
                if (!actorset.TryGetIndex(labels[i], out idx[i]))
                    throw new KeyNotFoundException($"The given key '{labels[i]}' was not present in the dictionary.");
            return idx;
        }

        private sealed class ActorsAndData
        {
            public string[] RowLabels = [], ColLabels = [];
            public double[,] Data = new double[0, 0];
            public string? ErrorMsg;
        }

        private static ActorsAndData ParseActorsAndData(string[] lines, char separator)
        {
            ActorsAndData aod = new();
            try
            {
                string[] colLabels = lines[0].TrimStart(separator).Split(separator);
                StripQuotes(colLabels);
                int nbrCols = colLabels.Length, nbrRows = lines.Length - 1;
                string[] rowLabels = new string[nbrRows];
                double[,] data = new double[nbrRows, nbrCols];
                for (int r = 0; r < nbrRows; r++)
                {
                    string[] cells = lines[r + 1].Split(separator);
                    rowLabels[r] = cells[0];
                    for (int c = 0; c < nbrCols; c++)
                    {
                        Fmt.TryParseDouble(cells[c + 1], out double v);
                        data[r, c] = v;
                    }
                }
                StripQuotes(rowLabels);
                aod.RowLabels = rowLabels;
                aod.ColLabels = colLabels;
                aod.Data = data;
            }
            catch (Exception e)
            {
                aod.ErrorMsg = "!Error: " + e.Message;
            }
            return aod;
        }

        private static void StripQuotes(string[] labels)
        {
            for (int i = 0; i < labels.Length; i++)
                labels[i] = labels[i].Trim(QuoteChars);
        }
        #endregion
    }
}
