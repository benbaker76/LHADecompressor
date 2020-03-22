using System.IO;

namespace LHADecompressor
{
    /**
     * LH3 format data decoder.
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class Lh3Decoder : LhDecoder
    {
        private const int BUFBITS = 16;
        private const int NP = 8 * 1024 / 64;
        private const int CBIT = 9;
        private const int CODE_TABLE_SIZE = 4096;
        private const int N1 = 286;
        private const int EXTRA_BITS = 8;
        private const int LENGTH_FIELD = 4;

        private static int[] FIXED = { 2, 0x01, 0x01, 0x03, 0x06, 0x0D, 0x1F, 0x4E, 0 };

        private int[] positionCode;

        private int[] codeLength;
        private int[] codeTable;

        private int np;
        private int blockSize;

        public Lh3Decoder(BinaryReader @in, long originalSize)
    : base(@in, originalSize, 13, OFFSET)
        {
            this.positionCode = new int[NPT];

            this.codeLength = new int[NC];
            this.codeTable = new int[CODE_TABLE_SIZE];

            this.blockSize = 0;

            this.np = 1 << (13 - 6);
        }

        private void ReadyMade()
        {
            int index = 0;
            int j = FIXED[index++];
            int weight = 1 << (16 - j);
            int code = 0;
            for (int i = 0; i < np; ++i)
            {
                while (FIXED[index] == i)
                {
                    ++j;
                    ++index;
                    weight = (int)((uint)weight >> 1);
                }
                positionLength[i] = j;
                positionCode[i] = code;
                code += weight;
            }
        }

        private void ReadTreeCode()
        {

            int i = 0;
            while (i < N1)
            {
                if (GetBits(1) != 0)
                {
                    codeLength[i] = GetBits(LENGTH_FIELD) + 1;
                }
                else
                {
                    codeLength[i] = 0;
                }
                ++i;
                if ((i == 3) && (codeLength[0] == 1) && (codeLength[1] == 1) && (codeLength[2] == 1))
                {
                    int c = GetBits(CBIT);
                    for (int j = 0; j < N1; ++j)
                    {
                        codeLength[j] = 0;
                    }
                    for (int j = 0; j < 4096; ++j)
                    {
                        codeTable[j] = c;
                    }
                    return;
                }
            }
            MakeTable(N1, codeLength, 12, codeTable);
        }

        private void ReadTreePosition()
        {
            int i = 0;
            while (i < NP)
            {
                positionLength[i] = GetBits(LENGTH_FIELD);
                ++i;
                if ((i == 3) && (positionLength[0] == 1) && (positionLength[1] == 1) && (positionLength[2] == 1))
                {
                    int c = GetBits(13 - 6);
                    for (int j = 0; j < NP; ++j)
                    {
                        positionLength[j] = 0;
                    }
                    for (int j = 0; j < 256; ++j)
                    {
                        positionTable[j] = c;
                    }
                    return;
                }
            }
        }

        protected internal override void InitRead()
        {
            FillBitBuffer(2 * CHAR_BIT);
        }

        protected internal override int DecodeCode()
        {
            if (blockSize == 0)
            {
                blockSize = GetBits(BUFBITS);
                ReadTreeCode();
                if (GetBits(1) != 0)
                {
                    ReadTreePosition();
                }
                else
                {
                    ReadyMade();
                }
                MakeTable(NP, positionLength, 8, positionTable);
            }
            --blockSize;
            int j = codeTable[(int)((uint)bitBuffer >> (16 - 12))];
            if (j < N1)
            {
                FillBitBuffer(codeLength[j]);
            }
            else
            {
                FillBitBuffer(12);
                int b = bitBuffer;
                do
                {
                    if ((b & 0x8000) != 0)
                    {
                        j = treeRight[j];
                    }
                    else
                    {
                        j = treeLeft[j];
                    }
                    b <<= 1;
                } while (j >= N1);
                FillBitBuffer(codeLength[j] - 12);
            }

            if (j == (N1 - 1))
            {
                j += GetBits(EXTRA_BITS);
            }
            return (j);
        }

        protected internal override int DecodePosition()
        {
            int j = positionTable[(int)((uint)bitBuffer >> (16 - 8))];
            if (j < np)
            {
                FillBitBuffer(positionLength[j]);
            }
            else
            {
                FillBitBuffer(8);
                int b = bitBuffer;
                do
                {
                    if ((b & 0x80) != 0)
                    {
                        j = treeRight[j];
                    }
                    else
                    {
                        j = treeLeft[j];
                    }
                    b <<= 1;
                } while (j >= np);
                FillBitBuffer(positionLength[j] - 8);
            }

            return ((j << 6) + GetBits(6));
        }
    }
}
