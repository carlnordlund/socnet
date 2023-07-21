﻿using System;
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

        //public Matrix idealMatrix;

        public int[,] blockIndices;
        //public string partString;
        public double gof;
        public string gofMethod;
        public int[,] blockPenalties;

        public BlockModel(string name, Matrix matrix, BlockImage blockimage, Partition partition, int[,] blockIndices, double gof, string gofMethod)
        {
            this.Name = name;
            this.matrix = matrix;
            this.blockimage = blockimage;
            this.partition = partition;
            this.blockIndices = blockIndices;
            //this.partString = partString;
            this.gof = Math.Round(gof, 4);
            this.gofMethod = gofMethod;
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

        internal List<string> DisplayBlockmodel()
        {
            List<string> lines = new List<string>();
            int nbrClusters = partition.clusters.Length;
            string line = "";
            for (int r = 0; r < nbrClusters; r++)
            {

                foreach (Actor rowActor in partition.clusters[r].actors)
                {
                    line = r + "_" + rowActor.Name + "\t";
                    for (int c = 0; c < nbrClusters; c++)
                    {
                        foreach (Actor colActor in partition.clusters[c].actors)
                        {
                            if (rowActor != colActor)
                                line += (matrix.Get(rowActor, colActor) > 0) ? "X" : " ";
                            else
                                line += @"\";

                        }
                        line += "|";

                    }
                    line = line.TrimEnd('|');
                    lines.Add(line);
                }
                if (r < nbrClusters - 1)
                    lines.Add("\t" + new string('-', partition.actorset.Count + nbrClusters - 1));
            }
            return lines;
        }

        internal BlockImage ExtractBlockimage()
        {
            if (!blockimage.multiBlocked)
                return blockimage;
            // It is multiblocked, so create new Blockimage based on this

            BlockImage bi = new BlockImage(blockimage, blockIndices);
            return bi;

        }

        internal Actorset GetBlockModelActorset()
        {
            Actorset actorset = new Actorset("actorset_" + this.Name);
            int nbrClusters = partition.clusters.Length;
            int index = 0;
            for (int i = 0; i < nbrClusters; i++)
            {
                foreach (Actor actor in partition.clusters[i].actors)
                {
                    actorset.actors.Add(new Actor(i + "_" + actor.Name, index));
                    index++;
                }
            }
            return actorset;
        }


        internal Matrix GetBlockModelMatrix()
        {
            Actorset bmActorset = new Actorset(this.Name + "_actorset");
            int nbrPositions = partition.clusters.Length;
            Dictionary<Actor, Actor> actorIndexMap = new Dictionary<Actor, Actor>();
            int index = 0;
            for (int i=0;i<nbrPositions;i++)
            {
                foreach (Actor actor in partition.clusters[i].actors)
                {
                    Actor bmActor = new Actor(i + "_" + actor.Name, index);
                    bmActorset.actors.Add(bmActor);
                    actorIndexMap.Add(actor,bmActor);
                    index++;
                }
            }
            Matrix bmMatrix = new Matrix(bmActorset, this.Name + "_matrix", matrix.Cellformat);
            foreach (Actor rowActor in matrix.actorset.actors)
                foreach (Actor colActor in matrix.actorset.actors)
                    bmMatrix.Set(actorIndexMap[rowActor], actorIndexMap[colActor], matrix.Get(rowActor, colActor));
            return bmMatrix;
        }
    }
}
