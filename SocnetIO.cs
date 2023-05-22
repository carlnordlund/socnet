using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Socnet.DataLibrary;

namespace Socnet
{
    public static class SocnetIO
    {

        internal static string LoadDataStructure(List<string> response, Dataset dataset, string filepath, string type, string name)
        {
            if (!File.Exists(filepath))
                return "!Error: File '" + filepath + "' not found";
            string[]? lines = readAllLines(filepath, response);
            if (lines == null)
                return "!Error: File '" + filepath + "' seems empty";
            //string filename = Path.GetFileNameWithoutExtension(filepath);
            string dsname = (name.Length == 0) ? Path.GetFileNameWithoutExtension(filepath) : name;
            if (type.Equals("matrix"))
            {
                ActorsAndData aod = parseActorsAndData(lines, response);
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
                    response.Add(dataset.StoreStructure(actorset));
                }
                Matrix matrix = new Matrix(actorset, dsname, "F2");
                matrix.installData(aod.rowLabels, aod.data);
                response.Add(dataset.StoreStructure(matrix));
            }
            else if (type.Equals("table"))
            {
                ActorsAndData aod = parseActorsAndData(lines, response);
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
                    //addColActorset = true;
                }
                //if (addRowActorset)
                //    response.Add(dataset.StoreStructure(rowActorset));
                //if (addColActorset)
                //    response.Add(dataset.StoreStructure(colActorset));
                




                //Actorset? rowActorset = dataset.GetActorsetByLabels(aod.rowLabels);
                //if (rowActorset == null)
                //    rowActorset = dataset.CreateActorsetByLabels(aod.rowLabels);
                //if (rowActorset == null)
                //    return "!Error: Couldn't create Actorset from row labels";
                //response.Add(dataset.StoreStructure(rowActorset));
                //Actorset? colActorset = dataset.GetActorsetByLabels(aod.colLabels);
                //if (colActorset == null)
                //    colActorset = dataset.CreateActorsetByLabels(aod.colLabels);
                //if (colActorset == null)
                //    return "!Error: Couldn't create Actorset from column labels";
                //response.Add(dataset.StoreStructure(colActorset));
                Table table = new Table(rowActorset, colActorset, dsname, "F2");
                table.installData(aod.rowLabels, aod.colLabels, aod.data);
                response.Add(dataset.StoreStructure(table));
            }
            else
                return "!Error: Type '" + type + "' not recognized";
            return "Loading data structure: OK";
        }

        private static ActorsAndData parseActorsAndData(string[] lines, List<string> response, char separator = '\t')
        {
            ActorsAndData actorsAndData = new ActorsAndData();
            try
            {
                string[] colLabels = lines[0].TrimStart(separator).Split(separator);
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
