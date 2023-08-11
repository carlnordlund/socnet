using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Socnet.DataLibrary.Blocks
{
    public class pcoBlock : _Block
    {
        public double p;

        public pcoBlock()
        {
            Name = "pco";
            this.p = 0.5;
            primeIndex = 11;
        }

        public pcoBlock(double p=0.5)
        {
            Name = "pco";
            this.p = p;
            primeIndex = 11;
        }

        public override void initArgValue(double v)
        {
            this.p = v;
        }
        public override _Block cloneBlock()
        {
            return new pcoBlock(this.p);
        }





        public override string ToString()
        {
            return Name + "_" + p;
        }


    }
}
