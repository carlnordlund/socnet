using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Socnet.DataLibrary.Blocks;

namespace Socnet.DataLibrary
{
    public class BlockImage : DataStructure
    {
        public List<_Block>[,]? blocks;
        public string[] positionNames;
        public int nbrPositions;
        public bool multiBlocked = false;


        public BlockImage(string name, int nbrPositions)
        {
            this.Name = name;
            this.nbrPositions = nbrPositions;
            blocks = new List<_Block>[nbrPositions, nbrPositions];
            positionNames = new string[nbrPositions];
            for (int r = 0; r < nbrPositions; r++)
            {
                positionNames[r] = "P" + r;
                for (int c = 0; c < nbrPositions; c++)
                    blocks[r, c] = new List<_Block>();
            }
        }

        public _Block[,]? getPickedBlocks(int[,] blockindices)
        {
            if (blockindices.GetLength(0)!=nbrPositions || blockindices.GetLength(1)!=nbrPositions)
                return null;
            _Block[,] retBlocks = new _Block[nbrPositions, nbrPositions];
            for (int r = 0; r < nbrPositions; r++)
                for (int c = 0; c < nbrPositions; c++)
                    if (blockindices[r, c] < blocks![r, c].Count)
                        retBlocks[r, c] = blocks[r, c][blockindices[r, c]];
            return retBlocks;
        }

        public bool hasBlocks()
        {
            if (blocks == null)
                return false;
            for (int r = 0; r < nbrPositions; r++)
                for (int c = 0; c < nbrPositions; c++)
                    if (blocks[r, c].Count == 0)
                        return false;
            return true;
        }

        internal override void GetContent(List<string> content)
        {
            string line = "";
            if (blocks !=null)
                for (int r = 0; r < nbrPositions; r++)
                {
                    line = "";
                    for (int c = 0; c < nbrPositions; c++)
                    {
                        line += "[";
                        if (blocks[r, c] != null)
                            line += string.Join(";", blocks[r, c]);
                        line += "],";
                    }
                    line = line.TrimEnd(',');
                    content.Add(line);
                }
            content.Add("Multiblocked: " + multiBlocked);
        }

        internal List<string> GetAllUniqueBlockNames()
        {
            List<string> blockNames = new List<string>();
            for (int r = 0; r < nbrPositions; r++)
                for (int c = 0; c < nbrPositions; c++)
                    foreach (_Block block in blocks![r, c])
                        if (!blockNames.Contains(block.Name))
                            blockNames.Add(block.Name);
            return blockNames;
        }

        internal override string GetSize()
        {
            return nbrPositions + "x" + nbrPositions;
        }

        internal _Block GetBlock(int r, int c, int i = 0)
        {
            return blocks![r, c][i];
        }

        internal void setBlocksByPattern(string pattern)
        {
            blocks = new List<_Block>[nbrPositions, nbrPositions];
            string[] patternBlocks = pattern.Split(';');
            for (int r = 0; r < nbrPositions; r++)
                for (int c = 0; c < nbrPositions; c++)
                {
                    blocks[r, c] = Functions.GetBlockInstances(patternBlocks);
                }
            checkMultiblocked();
        }

        internal void setBlocksByContentString(string[] blockCellContent)
        {
            blocks = new List<_Block>[nbrPositions, nbrPositions];
            // Extra check that content here will fit blockimage
            if (blockCellContent.Length != nbrPositions * nbrPositions)
                return;
            int index = 0;
            for (int r = 0; r < nbrPositions; r++)
                for (int c = 0; c < nbrPositions; c++)
                {
                    string[] blockNames = blockCellContent[index].Split(';');
                    blocks[r, c] = Functions.GetBlockInstances(blockNames);
                    index++;
                }
            checkMultiblocked();
        }

        // Check if this is multiblocked: if so, update multiblocked flag
        // This is run when the blockimage is created/updated by pattern/content
        private void checkMultiblocked()
        {
            if (blocks == null)
                return;
            multiBlocked = true;
            for (int r = 0; r < nbrPositions; r++)
                for (int c = 0; c < nbrPositions; c++)
                    if (blocks[r, c].Count > 1)
                        return;
            multiBlocked = false;
        }

        internal void setBlocksByPrimeIndices(int[,] blockIndices)
        {
            if (blockIndices == null)
                return;
            this.nbrPositions = blockIndices.GetLength(0);
            blocks = new List<_Block>[nbrPositions, nbrPositions];
            positionNames = new string[nbrPositions];
            for (int r=0;r<nbrPositions;r++)
            {
                positionNames[r] = "P" + r;
                for (int c=0;c<nbrPositions;c++)
                {
                    this.blocks[r, c] = new List<_Block>();
                    if (Functions.indexToIdealBlockName.ContainsKey(blockIndices[r, c]))
                    {
                        _Block? block = Functions.GetBlockInstance(Functions.indexToIdealBlockName[blockIndices[r, c]]);
                        if (block != null)
                            this.blocks[r, c].Add(block);
                    }
                }
            }
            multiBlocked = false;

        }

    }
}
