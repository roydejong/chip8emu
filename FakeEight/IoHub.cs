using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeEight
{
    /// <summary>
    /// Virtual IO board that interconnects components.
    /// </summary>
    public class IoHub
    {
        protected Cpu cpu;
        protected Ram ram;
        protected Display display;

        public Cpu Cpu
        {
            get
            {
                return cpu;
            }
        }

        public Ram Ram
        {
            get
            {
                return ram;
            }
        }

        public Display Display
        {
            get
            {
                return display;
            }
        }

        public IoHub()
        {
            cpu = new Cpu(this);
            ram = new Ram();
            display = new Display();
        }
    }
}
