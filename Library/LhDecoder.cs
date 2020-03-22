using System.IO;

namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public abstract class LhDecoder : SlidingDicDecoder
    {
        protected internal const int NPT = 0x80;
        protected internal const int NC = UNSIGNED_CHAR_MAX + MAX_MATCH + 2 - THRESHOLD;
        protected internal const int POSITION_TABLE_SIZE = 256;

        protected internal int[] treeLeft;
        protected internal int[] treeRight;
        protected internal int[] positionTable;
        protected internal int[] positionLength;

        public LhDecoder(BinaryReader @in, long originalSize, int dictionaryBit, int positionAdjust)
            : base(@in, originalSize, dictionaryBit, positionAdjust)
        {
            this.treeLeft = new int[2 * NC - 1];
            this.treeRight = new int[2 * NC - 1];
            this.positionTable = new int[POSITION_TABLE_SIZE];
            this.positionLength = new int[NPT];
        }

        protected internal void MakeTable(int nchar, int[] bitLength, int tableBits, int[] table)
        {
            int[] countTable = new int[17];
            int[] weightTable = new int[17];
            int[] startTable = new int[17];
            int total;
            int j, k, m;

            for (int i = 1; i <= 16; ++i)
            {
                countTable[i] = 0;
                weightTable[i] = 1 << (16 - i);
            }

            for (int i = 0; i < nchar; ++i)
            {
                if (bitLength[i] > 16)
                {
                    throw (new LhaException("Bad table."));
                }

                ++countTable[bitLength[i]];
            }

            total = 0;
            for (int i = 1; i <= 16; ++i)
            {
                startTable[i] = total;
                total += weightTable[i] * countTable[i];
            }

            if (((total & 0xffff) != 0) || (tableBits > 16))
            {
                throw (new LhaException("Bad table."));
            }

            m = 16 - tableBits;
            for (int i = 1; i <= tableBits; ++i)
            {
                startTable[i] = (int)((uint)startTable[i] >> m);
                weightTable[i] = (int)((uint)weightTable[i] >> m);
            }

            j = (int)((uint)startTable[tableBits + 1] >> m);
            k = 1 << tableBits;
            if (k > 4096)
            {
                k = 4096;
            }
            if (j != 0)
            {
                for (int i = j; i < k; ++i)
                {
                    table[i] = 0;
                }
            }

            int avail = nchar;
            for (j = 0; j < nchar; ++j)
            {
                k = bitLength[j];
                if (k == 0)
                {
                    continue;
                }

                int l = startTable[k] + weightTable[k];
                if (k <= tableBits)
                {
                    if (l > 4096)
                    {
                        l = 4096;
                    }
                    for (int i = startTable[k]; i < l; ++i)
                    {
                        table[i] = j;
                    }
                }
                else
                {
                    int[] t = table;
                    int start = startTable[k];
                    int p = (int)((uint)start >> m);
                    if (p > 4096)
                    {
                        throw (new LhaException("Bad table."));
                    }
                    start <<= tableBits;
                    for (int n = k - tableBits; n > 0; --n)
                    {
                        if (t[p] == 0)
                        {
                            treeRight[avail] = treeLeft[avail] = 0;
                            t[p] = avail++;
                        }

                        p = t[p];
                        if ((start & 0x8000) != 0)
                        {
                            t = treeRight;
                        }
                        else
                        {
                            t = treeLeft;
                        }

                        start <<= 1;
                    }

                    t[p] = j;
                }

                startTable[k] = l;
            }
        }
    }
}
