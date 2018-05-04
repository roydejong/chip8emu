using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FakeEight
{
    public partial class RenderForm : Form
    {
        public RenderForm()
        {
            InitializeComponent();

            this.Shown += RenderForm_Shown;
        }

        private Timer repaintTimer;
        private Bitmap paintBm;

        private void RenderForm_Shown(object sender, EventArgs e)
        {
            this.DoubleBuffered = true;

            repaintTimer = new Timer();
            repaintTimer.Interval = 10;
            repaintTimer.Tick += RepaintTimer_Tick;
            repaintTimer.Start();

            this.AutoSize = true;
            this.AutoSizeMode = AutoSizeMode.GrowOnly;
            this.ClientSize = new Size(640, 320);
            this.MaximizeBox = false;
            this.ResizeRedraw = true;
            this.Activate();
            this.CenterToScreen();
        }

        private void RepaintTimer_Tick(object sender, EventArgs e)
        {
            if (paintBm == null)
            {
                paintBm = new Bitmap(64, 32);
            }

            for (var x = 0; x < paintBm.Width; x++)
            {
                for (var y = 0; y < paintBm.Height; y++)
                {
                    var pixelOn = Program.VirtualMachine.IoHub.Display.GetPixel(x, y);
                    paintBm.SetPixel(x, y, pixelOn ? Color.White : Color.Black);
                }
            }

            pictureBox1.Image = paintBm;
            pictureBox1.Invalidate();

            var nextText = "FakeEight";

            if (Program.VirtualMachine.Running)
            {
                if (!String.IsNullOrWhiteSpace(Program.VirtualMachine.RomPath))
                {
                    nextText += " [" + Program.VirtualMachine.RomPath + "]";
                }
                else
                {
                    nextText += " [Running]";
                }
            }

            if (this.Text != nextText)
            {
                this.Text = nextText;
            }

            this.Refresh();

            Application.DoEvents();
            System.Threading.Thread.Sleep(0);
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {

        }
    }
}
