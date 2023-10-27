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

        public void setPositionName(int positionIndex, string positionName)
        {
            if (positionIndex >= 0 && positionIndex < nbrPositions)
                positionNames[positionIndex] = positionName;
        }

        public BlockImage(BlockImage template, int[,] blockindices)
        {
            this.Name = template.Name;
            this.nbrPositions = template.nbrPositions;
            this.blocks = new List<_Block>[nbrPositions, nbrPositions];
            positionNames = new string[nbrPositions];
            for (int r = 0; r < nbrPositions; r++)
            {
                positionNames[r] = template.positionNames[r];
                for (int c = 0; c < nbrPositions; c++)
                {
                    blocks[r, c] = new List<_Block>
                    {
                        template.blocks![r, c][blockindices[r, c]].cloneBlock()
                    };
                }
            }
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
            if (blocks != null)
            {
                for (int c = 0; c < nbrPositions; c++)
                    line += "\t" + positionNames[c];
                content.Add(line);
                for (int r = 0; r < nbrPositions; r++)
                {
                    line = positionNames[r];
                    for (int c = 0; c < nbrPositions; c++)
                    {
                        line += "\t[";
                        if (blocks[r, c] != null)
                            line += string.Join(";", blocks[r, c]);
                        line += "]";
                    }
                    content.Add(line);
                }
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

        internal void setBlockByPattern(int r, int c, string pattern)
        {
            blocks![r, c] = Functions.GetBlockInstances(pattern.Split(';'));
        }


        // Check if this is multiblocked: if so, update multiblocked flag
        // This is run when the blockimage is created/updated by pattern/content
        public void checkMultiblocked()
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
            for (int r = 0; r < nbrPositions; r++)
            {
                positionNames[r] = "P" + r;
                for (int c = 0; c < nbrPositions; c++)
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
