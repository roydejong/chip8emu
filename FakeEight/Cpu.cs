﻿using System;
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

                    var opcodeLastByte = (opcode & 0x0FF);

                    switch (opcodeLastByte)
                    {
                        case 0x07: // FX07: Timer, Set VX to value of delay timer

                            // TODO
                            break;

                        case 0x0A: // FX0A: KeyOp, Await key press, then store in VX (Blocking Op)

                            // TODO
                            break;

                        case 0x015: // FX15: Timer, Set delay timer to VX

                            // TODO
                            break;

                        case 0x018: // FX18: Sound, Set sound timer to VX

                            // TODO
                            break;

                        case 0x01E: // FX1E: Mem, Add VX to I
                            
                            xFX1E_MemAddVxToI(opcode);
                            break;

                        case 0x029: // FX29: Mem, Set I to location of sprite for font character in VX

                            xFX29_MemJumpToFontChar(opcode);
                            break;

                        case 0x033: // FX33: BCD, Binary magic (set BCD)

                            xFX33_BcdStoreVxAtI12(opcode);
                            break;

                        case 0x055: // FX55: Mem, Store V0 to VX in memory at I

                            xFX55_MemDump(opcode);
                            break;

                        case 0x065: // FX65: Mem, Load V0 to VX from memory at I

                            xFX65_MemRestore(opcode);
                            break;
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
        /// Sets I to the location of the sprite for the character in VX.
        /// Characters 0-F (in hexadecimal) are represented by a 4x5 font.
        /// </summary>
        public void xFX29_MemJumpToFontChar(ushort opcode)
        {
            var xNum = (ushort)((opcode & 0x0F00) >> 8);
            var xVal = rv[xNum];

            // Fonts are loaded into memory on VM startup, from address 0x50 onward
            // Each character (hex, so 0 - F) has 5 bytes
            // xVal is the number of the character, so 0 - 16

            ir = 0x50 + (xVal * 5);

            Console.WriteLine(" * MEM: Jump I to font character at position {0}, now at {1}", xVal, ir);
        }

        /// <summary>
        /// Stores the Binary-coded decimal representation of VX at the addresses I, I plus 1, and I plus 2
        /// </summary>
        public void xFX33_BcdStoreVxAtI12(ushort opcode)
        {
            // Code from TJA @ http://www.multigesture.net/wp-content/uploads/mirror/goldroad/chip8.shtml
            // I'm too dumb to understand this :-(

            Io.Ram.WriteByte(ir, (byte)(rv[(opcode & 0x0F00) >> 8] / 100));
            Io.Ram.WriteByte(ir + 1, (byte)((rv[(opcode & 0x0F00) >> 8] / 10) % 10));
            Io.Ram.WriteByte(ir + 2, (byte)((rv[(opcode & 0x0F00) >> 8] % 100) % 10));

            Console.WriteLine(" * BCD: Set binary coded decimal");
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

            Console.WriteLine(" * MEM: Add V{2} [{0}] to I, now at {1}.", xVal, nextValue, xNum);
        }

        /// <summary>
        /// Stores V0 to VX (including VX) in memory starting at address I.
        /// I is increased by 1 for each value written.
        /// </summary>
        public void xFX55_MemDump(ushort opcode)
        {
            var xRegNum = (ushort)((opcode & 0x0F00) >> 8);
            
            for (var regNum = 0; regNum <= xRegNum; regNum++)
            {
                Io.Ram.WriteByte(ir++, rv[regNum]);
            }

            Console.WriteLine(" * MEM: Dumped V0 - V{0} to RAM.", xRegNum);
        }

        /// <summary>
        /// Fills V0 to VX (including VX) with values from memory starting at address I.
        /// I is increased by 1 for each value written.
        /// </summary>
        public void xFX65_MemRestore(ushort opcode)
        {
            var xRegNum = (ushort)((opcode & 0x0F00) >> 8);

            for (var regNum = 0; regNum <= xRegNum; regNum++)
            {
                rv[regNum] = Io.Ram.ReadByte(ir++);
            }

            Console.WriteLine(" * MEM: Set V0 - V{0} from RAM.", xRegNum);
        }
    }
}