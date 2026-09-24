using Socnet.Core.Blocks;
using System.Text;

namespace Socnet.Core.Model
{
    /// <summary>
    /// A blockimage: a k x k grid of positions, where each block position holds one or more ideal blocks.
    /// A blockimage with more than one ideal block in some position is 'multi-blocked'.
    /// </summary>
    public sealed class BlockImage : DataStructure
    {
        private readonly List<IdealBlock>[] _cells;

        /// <summary>
        /// Creates a blockimage of the given size with no ideal blocks. Positions are named P0, P1, ...
        /// </summary>
        public BlockImage(string name, int nbrPositions)
        {
            Name = name;
            NbrPositions = nbrPositions;
            PositionNames = Partition.DefaultClusterNames(nbrPositions);
            _cells = new List<IdealBlock>[nbrPositions * nbrPositions];
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = [];
        }

        /// <summary>
        /// Number of positions (k).
        /// </summary>
        public int NbrPositions { get; }

        /// <summary>
        /// Names of the positions.
        /// </summary>
        public string[] PositionNames { get; }

        /// <summary>
        /// True if at least one block position holds more than one ideal block.
        /// </summary>
        public bool MultiBlocked => _cells.Any(c => c.Count > 1);

        /// <summary>
        /// The ideal blocks at block position (r,c).
        /// </summary>
        public List<IdealBlock> Blocks(int r, int c) => _cells[r * NbrPositions + c];

        /// <summary>
        /// The ideal block at block position (r,c), index i.
        /// </summary>
        public IdealBlock GetBlock(int r, int c, int i = 0) => _cells[r * NbrPositions + c][i];

        /// <summary>
        /// True if all block positions hold at least one ideal block.
        /// </summary>
        public bool HasBlocks() => _cells.All(c => c.Count > 0);

        /// <summary>
        /// Returns the names of all ideal block types used in this blockimage.
        /// </summary>
        public List<string> GetAllUniqueBlockNames()
        {
            List<string> names = [];
            foreach (var cell in _cells)
                foreach (IdealBlock block in cell)
                    if (!names.Contains(block.Name))
                        names.Add(block.Name);
            return names;
        }

        /// <summary>
        /// Sets all block positions to the ideal blocks in a semicolon-separated pattern, e.g. "nul;com".
        /// </summary>
        public void SetBlocksByPattern(string pattern)
        {
            string[] blockStrings = pattern.Split(';');
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = BlockFactory.CreateList(blockStrings);
        }

        /// <summary>
        /// Sets each block position (row-major) to the semicolon-separated ideal blocks in the content array.
        /// </summary>
        public void SetBlocksByContent(string[] cellContent)
        {
            if (cellContent.Length != _cells.Length)
                return;
            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = BlockFactory.CreateList(cellContent[i].Split(';'));
        }

        /// <summary>
        /// Sets the ideal blocks at block position (r,c) to the semicolon-separated blocks in the pattern.
        /// </summary>
        public void SetBlock(int r, int c, string pattern) => _cells[r * NbrPositions + c] = BlockFactory.CreateList(pattern.Split(';'));

        /// <summary>
        /// Sets the ideal blocks at the block position given by position names. Returns false if a name is not found.
        /// </summary>
        public bool SetBlock(string rowName, string colName, string pattern)
        {
            int r = Array.IndexOf(PositionNames, rowName), c = Array.IndexOf(PositionNames, colName);
            if (r < 0 || c < 0)
                return false;
            SetBlock(r, c, pattern);
            return true;
        }

        /// <summary>
        /// Sets the ideal blocks at block position (r,c) to (clones of) the given blocks.
        /// </summary>
        public void SetBlocks(int r, int c, IEnumerable<IdealBlock> blocks) => _cells[r * NbrPositions + c] = [.. blocks.Select(b => b.Clone())];

        /// <summary>
        /// Returns a copy of this blockimage with a new name.
        /// </summary>
        public BlockImage Clone(string name)
        {
            BlockImage bi = new(name, NbrPositions);
            Array.Copy(PositionNames, bi.PositionNames, NbrPositions);
            for (int r = 0; r < NbrPositions; r++)
                for (int c = 0; c < NbrPositions; c++)
                    bi.SetBlocks(r, c, Blocks(r, c));
            return bi;
        }

        /// <summary>
        /// Returns a single-blocked copy of this blockimage, using the ideal block given by blockIndices at each position.
        /// </summary>
        public BlockImage CreateSingleBlocked(string name, int[] blockIndices)
        {
            BlockImage bi = new(name, NbrPositions);
            Array.Copy(PositionNames, bi.PositionNames, NbrPositions);
            for (int r = 0; r < NbrPositions; r++)
                for (int c = 0; c < NbrPositions; c++)
                    bi.SetBlocks(r, c, [GetBlock(r, c, blockIndices[r * NbrPositions + c])]);
            return bi;
        }

        /// <summary>
        /// Returns a new blockimage with one extra position, where the new row and column hold the given pattern.
        /// </summary>
        public BlockImage Extend(string pattern)
        {
            int k = NbrPositions;
            BlockImage bi = new(Name + "_extended", k + 1);
            for (int r = 0; r < k; r++)
            {
                bi.PositionNames[r] = PositionNames[r];
                for (int c = 0; c < k; c++)
                    bi.SetBlocks(r, c, Blocks(r, c));
                bi.SetBlock(k, r, pattern);
                bi.SetBlock(r, k, pattern);
            }
            bi.SetBlock(k, k, pattern);
            return bi;
        }

        public override string Size => $"{NbrPositions}x{NbrPositions}";

        public override void GetContent(List<string> content)
        {
            StringBuilder sb = new();
            for (int c = 0; c < NbrPositions; c++)
                sb.Append('\t').Append(PositionNames[c]);
            content.Add(":" + sb);
            for (int r = 0; r < NbrPositions; r++)
            {
                sb.Clear();
                sb.Append(PositionNames[r]);
                for (int c = 0; c < NbrPositions; c++)
                    sb.Append("\t[").Append(string.Join(";", Blocks(r, c))).Append(']');
                content.Add(":" + sb);
            }
            content.Add(":Multiblocked: " + (MultiBlocked ? "True" : "False"));
        }
    }
}
