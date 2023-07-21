﻿using System;
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

        internal static string SaveDataStructure(List<string> response, Dataset dataset, string name, string filepath, string sep = "\t")
        {
            DataStructure? structure = dataset.GetStructureByName(name);
            if (structure == null)
                return "!Error: Structure '" + name + "' not found";
            if (structure is Matrix)
                return SaveMatrix((Matrix)structure, filepath, sep);
            if (structure is BlockImage)
                return SaveBlockImage((BlockImage)structure, filepath, sep);

            return "ok";
        }

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
            WriteFileCells(filecells, filepath, sep);
            return "Blockimage '" + blockimage.Name + "' saved: " + filepath;
        }

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
            WriteFileCells(filecells, filepath, sep);
            return "Matrix '" + matrix.Name + "' saved: " + filepath;
        }

        private static void WriteFileCells(string[,] filecells, string filepath, string sep)
        {
            int nbrRows = filecells.GetLength(0);
            int nbrCols = filecells.GetLength(1);
            List<string> csvLines = new List<string>();
            string line = "";
            for (int r=0;r<nbrRows;r++)
            {
                line = "";
                for (int c = 0; c < nbrCols-1; c++)
                    line += filecells[r, c] + sep;
                line += filecells[r, nbrCols-1];
                csvLines.Add(line);
            }
            File.WriteAllLines(filepath, csvLines);
        }

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
                ActorsAndData aod = parseActorsAndData(lines);
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
                ActorsAndData aod = parseActorsAndData(lines);
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
            else if (type.Equals("blockimage"))
            {
                string[] positionNames = lines[0].TrimStart('\t').Split('\t');
                int nbrPositions = positionNames.Length;
                if (lines.Length - 1 != nbrPositions)
                {
                    return "!Error: Row/col mismatch for blockimage file";
                }
                BlockImage blockimage = new BlockImage(dsname, nbrPositions);
                for (int r=0;r<nbrPositions;r++)
                {
                    blockimage.positionNames[r] = positionNames[r];
                    string[] cells = lines[r + 1].Split('\t');
                    for (int c=0;c<nbrPositions;c++)
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

        private static ActorsAndData parseActorsAndData(string[] lines, char separator = '\t')
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
