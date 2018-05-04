using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeEight
{
    public class VirtualMachine
    {
        protected IoHub io;
        protected int frequency = 60;
        protected bool running;
        protected string lastRomLoaded;
        
        public IoHub IoHub
        {
            get
            {
                return io;
            }
        }

        public string RomPath
        {
            get
            {
                return lastRomLoaded;
            }
        }

        public bool Running
        {
            get
            {
                return running;
            }
        }

        public int Frequency
        {
            get
            {
                return frequency;
            }

            set
            {
                if (running)
                {
                    throw new InvalidOperationException("Cannot modify VM frequency while running!");
                }

                frequency = value;
            }
        }

        public void Run(string romPath)
        {
            if (running)
            {
                throw new InvalidOperationException("VM is already running.");
            }

            running = true;
            Thread.CurrentThread.Name = "Virtual CHIP-8";

            Initialize();
            LoadRom(romPath);

            var tickTimer = Stopwatch.StartNew();
            var msPerCycle = (1000 / frequency);

            while (running)
            {
                if (tickTimer.ElapsedMilliseconds >= msPerCycle)
                {
                    tickTimer.Restart();

                    io.Cpu.Cycle();
                }

                Thread.Sleep(1);
            }
        }

        public void Stop()
        {
            running = false;
        }

        protected void Initialize()
        {
            io = new IoHub();

            // TODO Load fontset ROM
        }

        protected void LoadRom(string romPath, int memoryOffset = 0x200)
        {
            var bytes = File.ReadAllBytes(romPath);
            LoadBytes(bytes, memoryOffset);

            lastRomLoaded = romPath;
        }

        protected void LoadBytes(byte[] bytes, int memoryOffset = 0x200)
        {
            io.Ram.WriteBytes(memoryOffset, bytes);
        }

        protected void LoadFontsetRom()
        {
            byte[] fontsetBytes =
            {
                0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
                0x20, 0x60, 0x20, 0x20, 0x70, // 1
                0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
                0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
                0x90, 0x90, 0xF0, 0x10, 0x10, // 4
                0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
                0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
                0xF0, 0x10, 0x20, 0x40, 0x40, // 7
                0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
                0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
                0xF0, 0x90, 0xF0, 0x90, 0x90, // A
                0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
                0xF0, 0x80, 0x80, 0x80, 0xF0, // C
                0xE0, 0x90, 0x90, 0x90, 0xE0, // D
                0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
                0xF0, 0x80, 0xF0, 0x80, 0x80  // F
            };

            LoadBytes(fontsetBytes, 0x50);
        }
    }
}