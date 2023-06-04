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
        public string[]? positionNames;
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

        //public BlockImage(string name, string[,] blocklists)
        //{
        //    this.Name = name;
        //    positionNames = new string[nbrPositions];
        //    SetBlocks(blocklists);
        //}

        //private void SetBlocks(string[,] blocklists)
        //{
        //    if (blocklists == null)
        //        return;
        //    multiBlocked = false;
        //    this.nbrPositions = blocklists.GetLength(0);
        //    blocks = new List<_Block>[nbrPositions, nbrPositions];
        //    positionNames = new string[nbrPositions];
        //    for (int r=0; r< nbrPositions;r++)
        //    {
        //        positionNames[r] = "P" + r;
        //        for (int c=0; c<nbrPositions;c++)
        //        {
        //            this.blocks[r, c] = new List<_Block>();
        //            string[] blocks = blocklists[r, c].Split(',');
        //            if (blocks.Length > 1)
        //                multiBlocked = true;
        //            for (int i=0;i<blocks.Length;i++)
        //            {
        //                _Block? block = Functions.GetBlockInstance(blocks[i]);
        //                if (block != null)
        //                    this.blocks[r, c].Add(block);
        //            }
        //        }
        //    }

        //}

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
                        if (blocks[r,c] != null)
                        foreach (_Block block in blocks[r,c])
                        {
                                line += block.name + ",";
                        }
                        line = line.TrimEnd(',');
                        line += "],";
                    }
                    line = line.TrimEnd(',');
                    content.Add(line);
                }
            content.Add("Multiblocked: " + multiBlocked);
        }

        internal override string GetSize()
        {
            return nbrPositions + "x" + nbrPositions;
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

    }
}
