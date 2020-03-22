using System.IO;

namespace LHADecompressor
{
    /**
     * LH4 format data decoder.
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class Lh4Decoder : LhDecoder
    {
        private const int UNSIGNED_SHORT_BIT = 16;
        private const int NT = UNSIGNED_SHORT_BIT + 3;
        private const int TBIT = 5;
        private const int CBIT = 9;
        private const int CODE_TABLE_SIZE = 4096;

        private int[] codeLength;
        private int[] codeTable;

        private int np;
        private int positionBit;
        private int blockSize;

        public Lh4Decoder(BinaryReader @in, long originalSize, int dictionaryBit, int np, int positionBit)
    : base(@in, originalSize, dictionaryBit, OFFSET)
        {
            this.codeLength = new int[NC];
            this.codeTable = new int[CODE_TABLE_SIZE];

            this.np = np;
            this.positionBit = positionBit;
            this.blockSize = 0;
        }

        protected internal override void InitRead()
        {
            FillBitBuffer(2 * CHAR_BIT);
        }

        protected internal override int DecodeCode()
        {
            if (blockSize == 0)
            {
                blockSize = GetBits(16);
                ReadPositionLength(NT, TBIT, 3);
                ReadCodeLength();
                ReadPositionLength(np, positionBit, -1);
            }

            --blockSize;
            int j = codeTable[(int)((uint)bitBuffer >> (16 - 12))];

            if (j < NC)
            {
                FillBitBuffer(codeLength[j]);
            }
            else
            {
                FillBitBuffer(12);
                int mask = 1 << (16 - 1);
                do
                {
                    if ((bitBuffer & mask) != 0)
                    {
                        j = treeRight[j];
                    }
                    else
                    {
                        j = treeLeft[j];
                    }

                    mask = (int)((uint)mask >> 1);
                } while ((j >= NC) && ((mask != 0) || (j != treeLeft[j])));

                FillBitBuffer(codeLength[j] - 12);
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

                int mask = 1 << (16 - 1);
                do
                {
                    if ((bitBuffer & mask) != 0)
                    {
                        j = treeRight[j];
                    }
                    else
                    {
                        j = treeLeft[j];
                    }

                    mask = (int)((uint)mask >> 1);
                } while ((j >= np) && ((mask != 0) || (j != treeLeft[j])));

                FillBitBuffer(positionLength[j] - 8);
            }

            if (j != 0)
            {
                j = ((1 << (j - 1)) + GetBits(j - 1));
            }

            return (j);
        }

        private void ReadPositionLength(int nn, int nbit, int i_special)
        {
            int n = GetBits(nbit);
            if (n == 0)
            {
                int c = GetBits(nbit);

                for (int i = 0; i < nn; ++i)
                {
                    positionLength[i] = 0;
                }

                for (int i = 0; i < POSITION_TABLE_SIZE; ++i)
                {
                    positionTable[i] = c;
                }
            }
            else
            {
                int i = 0;
                int max = n < NPT ? n : NPT;
                while (i < max)
                {
                    int c = bitBuffer >> (16 - 3);
                    if (c != 7)
                    {
                        FillBitBuffer(3);
                    }
                    else
                    {
                        int mask = 1 << (16 - 4);
                        while ((mask & bitBuffer) != 0)
                        {
                            mask = (int)((uint)mask >> 1);
                            ++c;
                        }
                        FillBitBuffer(c - 3);
                    }

                    positionLength[i++] = c;

                    if (i == i_special)
                    {
                        c = GetBits(2);
                        while ((--c >= 0) && (i < NPT))
                        {
                            positionLength[i++] = 0;
                        }
                    }
                }

                while (i < nn)
                {
                    positionLength[i++] = 0;
                }

                MakeTable(nn, positionLength, 8, positionTable);
            }
        }

        private void ReadCodeLength()
        {
            int n = GetBits(CBIT);
            if (n == 0)
            {
                int c = GetBits(CBIT);
                for (int i = 0; i < NC; ++i)
                {
                    codeLength[i] = 0;
                }

                for (int i = 0; i < CODE_TABLE_SIZE; ++i)
                {
                    codeTable[i] = c;
                }
            }
            else
            {
                int i = 0;
                int max = n < NC ? n : NC;
                while (i < max)
                {
                    int c = positionTable[(int)((uint)bitBuffer >> (16 - 8))];
                    if (c >= NT)
                    {
                        int mask = 1 << (16 - 9);
                        do
                        {
                            if ((bitBuffer & mask) != 0)
                            {
                                c = treeRight[c];
                            }
                            else
                            {
                                c = treeLeft[c];
                            }

                            mask = (int)((uint)mask >> 1);
                        } while ((c >= NT) && ((mask != 0) || (c != treeLeft[c])));
                    }

                    FillBitBuffer(positionLength[c]);

                    if (c <= 2)
                    {
                        if (c == 0)
                        {
                            c = 1;
                        }
                        else if (c == 1)
                        {
                            c = GetBits(4) + 3;
                        }
                        else
                        {
                            c = GetBits(CBIT) + 20;
                        }

                        while (--c >= 0)
                        {
                            codeLength[i++] = 0;
                        }
                    }
                    else
                    {
                        codeLength[i++] = c - 2;
                    }
                }

                while (i < NC)
                {
                    codeLength[i++] = 0;
                }

                MakeTable(NC, codeLength, 12, codeTable);
            }
        }
    }
}