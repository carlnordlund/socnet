using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary
{
    public class BlockModel : DataStructure
    {
        public Matrix matrix;
        public BlockImage blockimage;
        public Partition partition;

        public Matrix idealMatrix;

        public int[,] blockIndices;
        public string partString;
        public double gof;
        public string gofMethod;
        public int[,] blockPenalties;

        public BlockModel(string name, Matrix matrix, BlockImage blockimage, Partition partition, int[,] blockIndices, string partString, string gofMethod, Matrix idealMatrix, int[,] blockPenalties)
        {
            this.Name = name;
            this.matrix = matrix;
            this.blockimage = blockimage;
            this.partition = partition;
            this.blockIndices = blockIndices;
            this.partString = partString;
            this.gofMethod = gofMethod;
            this.idealMatrix = idealMatrix;
            this.blockPenalties = blockPenalties;
        }



        internal override void GetContent(List<string> content)
        {
            content.Add("Blockmodel " + this.Name);
        }

        internal override string GetSize()
        {
            return "[Blockmodel size]";
        }
    }
}
