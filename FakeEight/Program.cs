using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FakeEight
{
    class Program
    {
        public static VirtualMachine VirtualMachine;

        static void Main(string[] args)
        {
            // Init
            Application.EnableVisualStyles();

            // Form
            var form = new RenderForm();
            form.Show();
            
            // Emulator
            var t = new Thread(new ThreadStart(() =>
            {
                VirtualMachine = new VirtualMachine();
                VirtualMachine.Frequency = 1;
                VirtualMachine.Run("roms/pong.ch8");
            }));
            t.Priority = ThreadPriority.Normal;
            t.Start();

            // Forms loop
            Application.Run(form);
        }
    }
}
