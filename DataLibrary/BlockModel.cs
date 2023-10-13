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

        public Matrix bmIdealMatrix, bmMatrix;


        public int[,] blockIndices;
        public double gof;
        public string gofMethod;
        public int[,] blockPenalties;

        public BlockModel(string name, Matrix matrix, BlockImage blockimage, Partition partition, int[,] blockIndices, double gof, string gofMethod, Matrix idealMatrix)
        {
            this.Name = name;
            this.matrix = matrix;
            if (!blockimage.multiBlocked)
            {
                this.blockimage = blockimage;
                this.blockIndices = blockIndices;
            }
            else
            {
                this.blockimage = new BlockImage(blockimage, blockIndices);
                this.blockIndices = new int[blockIndices.Length, blockIndices.Length];
            }
            this.partition = partition;
            
            this.gof = Math.Round(gof, 4);
            this.gofMethod = gofMethod;
            this.idealMatrix = idealMatrix;

            createBlockmodelMatrices();
        }

        private void createBlockmodelMatrices()
        {
            Actorset bmActorset = new Actorset(this.Name + "_actors");
            int nbrPositions = partition.clusters.Length;
            Dictionary<Actor, Actor> actorIndexMap = new Dictionary<Actor, Actor>();
            int index = 0;
            for (int i = 0; i < nbrPositions; i++)
            {
                foreach (Actor actor in partition.clusters[i].actors)
                {
                    Actor bmActor = new Actor(i + "_" + actor.Name, index);
                    bmActorset.actors.Add(bmActor);
                    actorIndexMap.Add(actor, bmActor);
                    index++;
                }
            }
            bmMatrix = new Matrix(bmActorset, this.Name + "_matrix", matrix.Cellformat);
            bmIdealMatrix = new Matrix(bmActorset, this.Name + "_idealmatrix", "N0");
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                {
                    bmMatrix.Set(actorIndexMap[rowActor], actorIndexMap[colActor], matrix.Get(rowActor, colActor));
                    bmIdealMatrix.Set(actorIndexMap[rowActor], actorIndexMap[colActor], idealMatrix.Get(rowActor, colActor));
                }
        }

        internal override void GetContent(List<string> content)
        {
            content.Add("Matrix:" + matrix.Name);
            content.Add("Blockimage:" + blockimage.Name);
            content.Add("Partition:" + partition.Name + " (" + partition.GetPartString() + ")");
            content.Add("GoF:" + gof + " (" + gofMethod + ")");
        }

        internal override string GetSize()
        {
            return "[Blockmodel size]";
        }

        internal List<string> DisplayBlockimage()
        {
            List<string> lines = new List<string>();
            int nbrClusters = blockimage.nbrPositions;
            string line = "";
            for (int c = 0; c < nbrClusters; c++)
                line += "\t" + blockimage.positionNames[c];
            lines.Add(line);

            for (int r=0;r<nbrClusters;r++)
            {
                line = blockimage.positionNames[r];
                for (int c = 0; c < nbrClusters; c++)
                    line += "\t" + blockimage.GetBlock(r, c, blockIndices[r, c]).ToString();
                lines.Add(line);
            }
            return lines;
        }

        internal List<string> DisplayBlockmodelMatrix()
        {
            return DisplayBlockmodel(matrix,'X',' ');
        }

        internal List<string> DisplayIdealMatrix()
        {
            return DisplayBlockmodel(idealMatrix, '1', '0');
        }

        internal List<string> DisplayBlockmodel(Matrix displayMatrix, char tieChar = 'X', char noTieChar=' ')
        {
            List<string> lines = new List<string>();
            if (displayMatrix == null)
                return lines;

            double max = Functions.GetMaxValue(displayMatrix, false);
            double median = (max == 1) ? 0.5 : Functions.GetMedianValue(displayMatrix);

            int nbrClusters = partition.clusters.Length;
            lines.Add("+" + new string('-', partition.actorset.Count + nbrClusters - 1) + "+");
            string line = "";
            double val = 0;
            for (int r = 0; r < nbrClusters; r++)
            {

                foreach (Actor rowActor in partition.clusters[r].actors)
                {
                    line = "|";
                    for (int c = 0; c < nbrClusters; c++)
                    {
                        foreach (Actor colActor in partition.clusters[c].actors)
                        {
                            val = displayMatrix.Get(rowActor, colActor);
                            if (rowActor != colActor)
                            {
                                line += (val > median) ? tieChar : (double.IsNaN(val)) ? "." : noTieChar;
                            }
                            else
                                line += @"\";
                        }
                        line += "|";
                    }
                    line += "\t" + r + "_" + rowActor.Name;
                    lines.Add(line);
                }
                if (r < nbrClusters)
                    lines.Add("+" + new string('-', partition.actorset.Count + nbrClusters - 1) + "+");
            }
            return lines;
        }
    }
}
