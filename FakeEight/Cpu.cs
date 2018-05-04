using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FakeEight
{
    /// <summary>
    /// Virtual CHIP-8 CPU component.
    /// </summary>
    public class Cpu
    {
        /// <summary>
        /// Virtual system board that allows components to communicate.
        /// </summary>
        protected IoHub Io;
        
        /// <summary>
        /// Index Register (I)
        /// </summary>
        protected int ir;

        /// <summary>
        /// CPU Registers
        /// </summary>
        /// <remarks>
        /// The Chip 8 has 15 8-bit general purpose registers named V0,V1 up to VE.
        /// The 16th register is used for the carry flag.
        /// </remarks>
        protected byte[] rv;

        /// <summary>
        /// Program Counter (pc)
        /// </summary>
        protected int pc;

        /// <summary>
        /// Program counter stack, each entry is a pc value
        /// Used for calling to, and returning from, subroutines
        /// </summary>
        protected List<int> stack;

        /// <summary>
        /// The next value for the pc, once the cycle completes.
        /// </summary>
        protected int cycleNextPc;

        public Cpu(IoHub io)
        {
            this.Io = io;

            Reset();
        }

        public void Reset()
        {
            ir = 0x000;
            rv = new byte[16];
            pc = 0x200;
            stack = new List<int>();
        }

        /// <summary>
        /// Performs one CPU cycle based on the current program counter.
        /// </summary>
        public void Cycle()
        {
            cycleNextPc = pc + 2;

            // Read the next opcode from memory and execute it
            Execute(Io.Ram.ReadShort(pc));

            // Update program counter
            pc = cycleNextPc;
        }

        /// <summary>
        /// Parses an opcode
        /// </summary>
        public void Execute(ushort opcode)
        {
            if (opcode == 0x000)
            {
                // No-op
                return;
            }

            Console.WriteLine("Exec " + (opcode).ToString("X4"));

            // The first nibble (four bytes) in the opcode is used as a prefix
            // We'll switch based on the appropriate hex char (0 - F) to determine which opcode to run
            ushort shiftedopc = (ushort)(opcode & (0xF000));

            switch (shiftedopc)
            {
                case (0x0000): // *** Call RCA 1802 (0NNN), Clear screen (00E0) or Return sub (00EE)
                    
                    if (opcode == 0x00E0)
                    {
                        x00E0_DisplayClear();
                    }
                    else if (opcode == 0x00EE)
                    {
                        x00EE_FlowReturn();
                    }
                    else
                    {
                        x0NNN_CallRca1802(opcode);
                    }

                    break;

                case 0x1000: // Jump to NNN

                    x1NNN_FlowGoto(opcode);
                    break;

                case 0x2000: // Call sub

                    x2NNN_FlowCall(opcode);
                    break;

                case 0x3000: // Skip, VX eq NN?

                    x3XNN_CondIfVxEqNn(opcode);
                    break;

                case 0x4000: // Skip, VX neq NN?

                    x4XNN_CondIfVxNeqNn(opcode);
                    break;

                case 0x5000: // Skip, VX eq VY?

                    x5XY0_CondIfVxEqVy(opcode);
                    break;

                case 0x6000: // Set VX to NN

                    x6XNN_ConstSetVxToNn(opcode);
                    break;

                case 0x7000: // Add NN to VX (No carry)

                    x7XNN_ConstAddNnToVx(opcode);
                    break;

                case 0x8000: // Bit and math ops ***

                    break;

                case 0x9000: // Skip, VX neq VY?

                    break;

                case 0xA000: // Set I to NN

                    xANNN_MemSetIndexRegister(opcode);
                    break;

                case 0xB000: // Jump to NNN plus V0

                    break;

                case 0xC000: // Set VX to bitwise RAND and NN

                    break;

                case 0xD000: // Draw sprite

                    xDXYN_DispDraw(opcode);
                    break;

                case 0xE000: // Skip, key state checks ***

                    break;

                case 0xF000: // Memory, timer and sound operations ***

                    if ((opcode & 0x0FF) == 0x01E) // FX1E: Add VX to I
                    {
                        xFX1E_MemAddVxToI(opcode);
                    }

                    break;

                default:

                    Console.WriteLine("Execution error: Opcode is out of expected range: {0}", opcode.ToString("X4"));
                    break;
            }
        }

        public void x0NNN_CallRca1802(ushort opcode)
        {
            Console.WriteLine("Execution error: RCA 1802 is not implemented, no-op");
        }

        public void x00E0_DisplayClear()
        {
            Io.Display.Clear();

            Console.WriteLine(" * Screen clear.");
        }

        public void x00EE_FlowReturn()
        {
            // Go back to the previous stack location
            var stackPrevIdx = stack.Count - 1;

            if (stackPrevIdx >= 0)
            {
                cycleNextPc = stack.ElementAt(stackPrevIdx) + 2;
                stack.RemoveAt(stackPrevIdx);

                Console.WriteLine("* Return, goto " + cycleNextPc);
            }
            else
            {
                Console.WriteLine("Execution error: Return called from root level (stack zero)");
            }
        }

        public void x1NNN_FlowGoto(ushort opcode)
        {
            cycleNextPc = (opcode & 0x0FFF); // jump to new pointer

            Console.WriteLine("* Goto " + cycleNextPc);
        }

        public void x2NNN_FlowCall(ushort opcode)
        {
            cycleNextPc = (opcode & 0x0FFF); // jump to new pointer
            stack.Add(pc); // this is a subroutine, so retain previous pointer in stack

            Console.WriteLine("* CallGoto " + cycleNextPc);
        }

        public void x3XNN_CondIfVxEqNn(ushort opcode)
        {
            ushort vNum = (ushort)((opcode & 0x0F00) >> 8);
            byte eqVal = (byte)(opcode & 0x00FF);

            Console.Write(" * IF: Register V{0} == {1}?", vNum, eqVal);
            
            if (rv[vNum] == eqVal)
            {
                // Register value X equals NN, skip next instruction
                cycleNextPc += 2;
                Console.WriteLine("TRUE, SKIP");
            }
            else
            {
                Console.WriteLine("FALSE");
            }
        }

        public void x4XNN_CondIfVxNeqNn(ushort opcode)
        {
            ushort vNum = (ushort)((opcode & 0x0F00) >> 8);
            byte eqVal = (byte)(opcode & 0x00FF);

            Console.Write(" * IF: Register V{0} != {1}?", vNum, eqVal);

            if (rv[vNum] != eqVal)
            {
                // Register value X does not equal NN, skip next instruction
                cycleNextPc += 2;
                Console.WriteLine("TRUE, SKIP");
            }
            else
            {
                Console.WriteLine("FALSE");
            }
        }

        public void x5XY0_CondIfVxEqVy(ushort opcode)
        {
            ushort vNumX = (ushort)((opcode & 0x0F00) >> 8);
            ushort vNumY = (ushort)((opcode & 0x00F0) >> 4);

            Console.Write(" * IF: Register V{0} == Register V{1}?", vNumX, vNumY);

            if (rv[vNumX] == rv[vNumY])
            {
                // Register value X equals register value Y, skip next instruction
                cycleNextPc += 2;
                Console.WriteLine("TRUE, SKIP");
            }
            else
            {
                Console.WriteLine("FALSE");
            }
        }

        public void x6XNN_ConstSetVxToNn(ushort opcode)
        {
            ushort vNum = (ushort)((opcode & 0x0F00) >> 8);
            byte newVal = (byte)(opcode & 0x00FF);

            rv[vNum] = newVal;

            Console.WriteLine(" * Set V{0} to {1}", vNum, newVal);
        }

        public void x7XNN_ConstAddNnToVx(ushort opcode)
        {
            ushort vNum = (ushort)((opcode & 0x0F00) >> 8);
            byte addVal = (byte)(opcode & 0x00FF);

            rv[vNum] += addVal;

            Console.WriteLine(" * Add {1} to V{0}", vNum, addVal);
        }

        public void xANNN_MemSetIndexRegister(ushort opcode)
        {
            var nextIrVal = (ushort)(opcode & 0x0FFF);

            Console.WriteLine(" * Set IR to {0}", nextIrVal);

            ir = nextIrVal;
        }

        /// <summary>
        /// Draws a sprite at coordinate (VX, VY) that has a width of 8 pixels and a height of N pixels.
        /// 
        /// Each row of 8 pixels is read as bit-coded (with the most significant bit of each byte displayed on the left)
        /// starting from memory location I; I value doesn't change after the execution of this instruction. As described above,
        /// VF is set to 1 if any screen pixels are flipped from set to unset when the sprite is drawn, and to 0 if that doesn't happen.
        /// </summary>
        public void xDXYN_DispDraw(ushort opcode)
        {
            // Shift X and Y register numbers + sprite height from the opcode
            ushort xNum = (ushort)((opcode & 0x0F00) >> 8);
            ushort yNum = (ushort)((opcode & 0x00F0) >> 4);
            byte nHeight = (byte)(opcode & 0x000F);

            // Read X + Y values from their registers
            byte xVal = rv[xNum];
            byte yVal = rv[yNum];

            // Set VF flag to 0 by default, to indicate only new pixels were drawn
            rv[15] = 0;

            // Loop through target coordinates, read sprite data from memory address I
            var pxChangeNum = 0;

            for (var highNum = 0; highNum < nHeight; highNum++)
            {
                byte hMemVal = Io.Ram.ReadByte(ir + highNum);

                // Every vertical line has one byte (in hMemVal).
                // Each byte has 8 bits. (a fixed sprite width of 8 pixels applies to all draws)

                for (int wideNum = 0; wideNum < 8; wideNum++) 
                {
                    int drawX = xVal + wideNum;
                    int drawY = yVal + highNum;

                    if ((hMemVal & (0x80 >> wideNum)) != 0)
                    {
                        var pixelIsOn = (Io.Display.GetPixel(drawX, drawY));

                        if (pixelIsOn)
                        {
                            // Turn pixel off, and set VF flag to 1 to report this has happened
                            rv[15] = 1;
                        }

                        Io.Display.SetPixel(drawX, drawY, !pixelIsOn);
                        pxChangeNum++;
                    }
                }
            }

            Console.WriteLine(" * Draw operation, {0} pixels changed", pxChangeNum);
        }

        /// <summary>
        /// Adds VX to I.
        /// </summary>
        /// <remarks>
        /// VF is set to 1 when there is a range overflow (I+VX>0xFFF), and to 0 when there isn't.
        /// This is an undocumented feature of the CHIP-8 and used by the Spacefight 2091! game.
        /// </remarks>
        public void xFX1E_MemAddVxToI(ushort opcode)
        {
            var xNum = (ushort)((opcode & 0x0F00) >> 8);
            var xVal = rv[xNum];

            var nextValue = ir + xVal;

            if (nextValue >= 0xFFF)
            {
                // Overflow
                rv[15] = 0;
            }
            else
            {
                // Not overflow
                rv[15] = 0;
            }

            ir = nextValue;
        }

    }
}