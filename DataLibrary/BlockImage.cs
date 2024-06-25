using Socnet.DataLibrary.Blocks;

namespace Socnet.DataLibrary
{
    /// <summary>
    /// Class for BlockImage object
    /// </summary>
    public class BlockImage : DataStructure
    {
        public List<_Block>[,]? blocks;
        public string[] positionNames;
        public int nbrPositions;
        public bool multiBlocked = false;

        /// <summary>
        /// Constructor for BlockImage object, specifying its name and size
        /// </summary>
        /// <param name="name">Name of blockimage</param>
        /// <param name="nbrPositions">Size of blockimage (i.e. number of positions/clusters)</param>
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

        /// <summary>
        /// Method for setting the name of each position in the BlockImage
        /// </summary>
        /// <param name="positionIndex">Index of position</param>
        /// <param name="positionName">Name of position</param>
        public void setPositionName(int positionIndex, string positionName)
        {
            if (positionIndex >= 0 && positionIndex < nbrPositions)
                positionNames[positionIndex] = positionName;
        }

        /// <summary>
        /// Constructor for single-blocked BlockImage object, using existing BlockImage as template and the specific multi-blocked indices as input.
        /// This is used when converting a given multiblocked BlockImage (such as used in freesearching) to a specific single-blocked BlockImage to store a result.
        /// </summary>
        /// <param name="template">The (typically multi-blocked) BlockImage that is used as a template</param>
        /// <param name="blockindices">2d array indicating which of the blocks in the multiblocked template that should be used</param>
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

        /// <summary>
        /// Method to check if the BlockImage has all blocks (i.e. at least one ideal Block object at each block position
        /// </summary>
        /// <returns>Returns true if there are at least one block in each block position</returns>
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

        /// <summary>
        /// Method to retrieve a list containing all unique block names in the block image
        /// </summary>
        /// <returns>List of all unique block names (string) in the BlockImage</returns>
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

        /// <summary>
        /// Get the ideal Block at block position r,c, index i
        /// </summary>
        /// <param name="r">Block row in the block image</param>
        /// <param name="c">Block column in the block image</param>
        /// <param name="i">Index of the block (by default 0 for single-blocked BlockImage objects)</param>
        /// <returns>Returns the ideal block</returns>
        internal _Block GetBlock(int r, int c, int i = 0)
        {
            return blocks![r, c][i];
        }

        /// <summary>
        /// Set the ideal block positions given a pattern string, where ideal block names are separated by semicolon
        /// E.g. for a standard multiblocked blockimage for structural equivalence: nul;com
        /// </summary>
        /// <param name="pattern">The pattern string of ideal blocks to populate the BlockImage</param>
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

        /// <summary>
        /// Set the ideal block positions given an array of block strings
        /// Size of array should be size^2.
        /// </summary>
        /// <param name="blockCellContent">Array of block strings</param>
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

        /// <summary>
        /// Set the ideal blocks at specific block row and block column to the provided block pattern
        /// </summary>
        /// <param name="r">Block row in the block image</param>
        /// <param name="c">Block column in the block image</param>
        /// <param name="pattern">String with semicolon-separated block names</param>
        internal void setBlockByPattern(int r, int c, string pattern)
        {
            blocks![r, c] = Functions.GetBlockInstances(pattern.Split(';'));
        }

        internal bool setBlockByPattern(string rowName, string colName, string pattern)
        {
            int r = -1, c = -1;
            for (int i=0;i<positionNames.Length;i++)
            {
                if (positionNames[i].Equals(rowName))
                    r = i;
                if (positionNames[i].Equals(colName))
                    c = i;
            }
            if (r == -1 || c == -1)
                return false;
            setBlockByPattern(r, c, pattern);
            return true;
        }


        /// <summary>
        /// Check if this is multiblocked: if so, update multiblocked flag
        /// This is run when the blockimage is created/updated by pattern/content
        /// </summary>
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

        /// <summary>
        /// Set blocks in single-blocked BlockImage by the provided prime indices of blocks
        /// </summary>
        /// <param name="blockIndices">2s array containing prime indices of ideal blocks</param>
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
