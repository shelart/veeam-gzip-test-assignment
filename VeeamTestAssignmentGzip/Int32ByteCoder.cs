using System;
using System.Runtime.InteropServices;

namespace VeeamTestAssignmentGzip
{
    // It's like C++ union.
    [StructLayout(LayoutKind.Explicit)]
    public struct Int32ByteCoder
    {
        [FieldOffset(0)] private Int32 value;
        [FieldOffset(0)] private byte byte1;
        [FieldOffset(1)] private byte byte2;
        [FieldOffset(2)] private byte byte3;
        [FieldOffset(3)] private byte byte4;

        public Int32 Value => this.value;
        public byte Byte1 => this.byte1;
        public byte Byte2 => this.byte2;
        public byte Byte3 => this.byte3;
        public byte Byte4 => this.byte4;

        public Int32ByteCoder(int value)
        {
            byte1 = byte2 = byte3 = byte4 = 0;
            this.value = value;
        }

        public Int32ByteCoder(byte byte1, byte byte2, byte byte3, byte byte4)
        {
            value = 0;
            this.byte1 = byte1;
            this.byte2 = byte2;
            this.byte3 = byte3;
            this.byte4 = byte4;
        }

        public static implicit operator Int32(Int32ByteCoder value)
        {
            return value.value;
        }

        public static implicit operator Int32ByteCoder(Int32 value)
        {
            return new Int32ByteCoder(value);
        }
    }
}
