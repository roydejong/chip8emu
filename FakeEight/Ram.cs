using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FakeEight
{
    /// <summary>
    /// Virtual RAM.
    /// </summary>
    public class Ram
    {
        public const int DEFAULT_CAPACITY = 4096;

        protected byte[] memory;
        protected object syncLock;

        public int TotalCapacityInBytes
        {
            get
            {
                return memory.Length;
            }
        }

        public Ram(int bytesCapacity = DEFAULT_CAPACITY)
        {
            memory = new byte[bytesCapacity];
            syncLock = new object();
        }

        public void Reset()
        {
            lock (syncLock)
            {
                memory = new byte[TotalCapacityInBytes];
            }
        }
        
        public byte ReadByte(int index)
        {
            lock (syncLock)
            {
                return memory[index];
            }
        }

        public byte[] ReadBytes(int startIndex, int length)
        {
            lock (syncLock)
            {
                byte[] portion = new byte[length];

                for (var i = 0; i < length; i++)
                {
                    portion[i] = memory[startIndex + i];
                }

                return portion;
            }
        }

        public ushort ReadShort(int startIndex)
        {
            lock (syncLock)
            {
                byte a = memory[startIndex];
                byte b = memory[startIndex + 1];

                // As one opcode is 2 bytes long, we will need to fetch two successive bytes and merge them to get the actual opcode.
                // In order to merge both bytes and store them in an unsigned short (2 bytes datatype) we will use the bitwise OR operation.

                // Takeaway: Opcodes are stored as two bytes in big-endian order.

                return (ushort)(a << 8 | b);
            }
        }

        public void WriteByte(int index, byte value)
        {
            lock (syncLock)
            {
                memory[index] = value;
            }
        }

        public void WriteBytes(int startIndex, byte[] value)
        {
            lock (syncLock)
            {
                for (var i = 0; i < value.Length; i++)
                {
                    memory[startIndex + i] = value[i];
                }
            }
        }
    }
}
