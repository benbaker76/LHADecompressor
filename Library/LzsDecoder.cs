using System.IO;

namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class LzsDecoder : SlidingDicDecoder
    {
        private const int MAGIC = 18;

        private int matchPosition;

        public LzsDecoder(BinaryReader @in, long originalSize)
            : base(@in, originalSize, 11, 256 - 2)
        {
        }

        protected internal override int DecodeCode()
        {
            int b = GetBits(1);
            if (b != 0)
            {
                return (GetBits(8));
            }
            else
            {
                matchPosition = GetBits(11);
                return (GetBits(4) + 0x100);
            }
        }

        protected internal override int DecodePosition()
        {
            return ((bufferPointerEnd - matchPosition - MAGIC) & dictionaryMask);
        }

        protected internal override void InitRead()
        {
            FillBitBuffer(2 * CHAR_BIT);
        }
    }
}
