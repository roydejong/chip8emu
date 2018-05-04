using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeEight
{
    /// <summary>
    /// Virtual display buffer device.
    /// </summary>
    public class Display
    {
        protected int width;
        protected int height;
        protected bool[,] buffer;

        public Display(int widthPixels = 64, int heightPixels = 32)
        {
            this.width = widthPixels;
            this.height = heightPixels;

            Clear();
        }

        public void Clear()
        {
            buffer = new bool[width, height];
        }

        public void SetPixel(int x, int y, bool on)
        {
            buffer[x, y] = on;
        }

        public bool GetPixel(int x, int y)
        {
            return buffer[x, y];
        }
    }
}
