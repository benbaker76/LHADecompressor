using System.IO;

namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class Lz5Decoder : SlidingDicDecoder
    {
        private const int MAGIC = 19;

        private int flag;
        private int flagCount;
        private int matchPosition;
        public Lz5Decoder(BinaryReader @in, long originalSize)
            : base(@in, originalSize, 12, OFFSET)
        {
        }

        protected internal override int DecodeCode()
        {
            if (flagCount == 0)
            {
                flagCount = 8;
                flag = @in.ReadByte();
            }
            --flagCount;
            int c = @in.ReadByte();
            if ((flag & 0x0001) == 0)
            {
                matchPosition = c;
                c = @in.ReadByte();
                matchPosition += (c & 0x00F0) << 4;
                c &= 0x000F;
                c |= 0x0100;
            }
            flag = (int)((uint)flag >> 1);
            return (c);
        }

        protected internal override int DecodePosition()
        {
            return ((bufferPointerEnd - matchPosition - MAGIC) & dictionaryMask);
        }

        protected internal override void InitRead()
        {
            flagCount = 0;

            for (int i = 0; i < 256; ++i)
            {
                for (int j = 0; j < 13; ++j)
                {
                    dictionaryBuffer[i * 13 + 18 + j] = (byte)i;
                }
                dictionaryBuffer[256 * 13 + 18 + i] = (byte)i;
                dictionaryBuffer[256 * 13 + 256 + 18 + i] = (byte)(255 - i);
            }
            for (int i = 0; i < 128; ++i)
            {
                dictionaryBuffer[256 * 13 + 512 + 18] = 0;
            }
            for (int i = 0; i < (128 - 18); ++i)
            {
                dictionaryBuffer[256 * 13 + 512 + 128 + 18] = 0x20;
            }
        }
    }
}
