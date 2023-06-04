using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Socnet.DataLibrary.Blocks;

namespace Socnet
{
    public static class Functions
    {
        public static List<string> idealBlockNames = new List<string>() { "dnc", "nul", "com" };
        

        internal static _Block? GetBlockInstance(string blockName)
        {
            var objecttype = Type.GetType("Socnet.DataLibrary.Blocks." + blockName+"Block");

            if (objecttype == null)
                return null;
            var instObj = Activator.CreateInstance(objecttype);
            if (instObj is _Block)
                return (_Block)instObj;
            return null;
        }

        internal static List<_Block> GetBlockInstances(string[] blockNames)
        {
            List<_Block> blocks = new List<_Block>();
            List<string> blocksAlready = new List<string>();
            foreach (string blockName in blockNames)
            {
                if (!blocksAlready.Contains(blockName))
                {
                    _Block? block = GetBlockInstance(blockName);
                    if (block != null)
                    {
                        blocks.Add(block);
                        blocksAlready.Add(blockName);
                    }
                }
            }
            return blocks;
        }
    }
}
