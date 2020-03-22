using System.IO;

namespace LHADecompressor
{
    /**
     * LH2 format data decoder.
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class Lh2Decoder : LhDecoder
    {
        private const int N_CHAR = 256 + 60 - THRESHOLD + 1;
        private const int TREESIZE_CODE = N_CHAR * 2;
        private const int TREESIZE_POSITION = 128 * 2;
        private const int TREESIZE = TREESIZE_CODE + TREESIZE_POSITION;
        private const int ROOT_CODE = 0;
        private const int ROOT_POSITION = TREESIZE_CODE;

        private int nMax;
        private int maxMatch;

        private int[] child;
        private int[] parent;
        private int[] block;
        private int[] edge;
        private int[] stock;
        private int[] sNode;
        private int[] freq;
        private int avail;
        private int n1;
        private int totalPosition;
        private int mostPosition;
        private int nn;
        private long nextCount;

        public Lh2Decoder(BinaryReader @in, long originalSize)
    : base(@in, originalSize, 13, 253)
        {
            this.nMax = 286;
            this.maxMatch = MAX_MATCH;
            this.nn = 1 << 13;
        }

        private void InitCodeDynamic()
        {
            this.child = new int[TREESIZE];
            this.parent = new int[TREESIZE];
            this.block = new int[TREESIZE];
            this.edge = new int[TREESIZE];
            this.stock = new int[TREESIZE];
            this.sNode = new int[TREESIZE / 2];
            this.freq = new int[TREESIZE];

            n1 = (nMax >= (256 + maxMatch - THRESHOLD + 1)) ? 512 : nMax - 1;
            for (int i = 0; i < TREESIZE_CODE; ++i)
            {
                stock[i] = i;
                block[i] = 0;
            }

            int j = nMax * 2 - 2;
            for (int i = 0; i < nMax; ++i, --j)
            {
                freq[j] = 1;
                child[j] = ~i;
                sNode[i] = j;
                block[j] = 1;
            }
            avail = 2;
            edge[1] = nMax - 1;
            for (int i = nMax * 2 - 2; j >= 0; i -= 2, --j)
            {
                int f = freq[j] = freq[i] + freq[i - 1];
                child[j] = i;
                parent[i] = parent[i - 1] = j;
                if (f == freq[j + 1])
                {
                    block[j] = block[j + 1];
                }
                else
                {
                    block[j] = stock[avail++];
                }
                edge[block[j]] = j;
            }
        }

        private void InitPositionDynamic()
        {
            freq[ROOT_POSITION] = 1;
            child[ROOT_POSITION] = ~(N_CHAR);
            sNode[N_CHAR] = ROOT_POSITION;
            block[ROOT_POSITION] = stock[avail++];
            edge[block[ROOT_POSITION]] = ROOT_POSITION;
            mostPosition = ROOT_POSITION;
            totalPosition = 0;
            nextCount = 64;
        }

        private void Reconstruct(int begin, int end)
        {
            int j, l, b, f;

            j = begin;
            b = block[begin];
            for (int i = begin; i < end; ++i)
            {
                int k = child[i];
                if (k < 0)
                {
                    freq[j] = (freq[i] + 1) / 2;
                    child[j] = k;
                    ++j;
                }
                b = block[i];
                if (edge[b] == i)
                {
                    stock[--avail] = b;
                }
            }
            --j;
            l = end - 2;
            for (int i = end - 1; i >= begin; --i, l -= 2)
            {
                while (i >= l)
                {
                    freq[i] = freq[j];
                    child[i] = child[j];
                    --i;
                    --j;
                }
                int k;
                f = freq[l] + freq[l + 1];
                for (k = begin; f < freq[k]; ++k)
                    ;
                while (j >= k)
                {
                    freq[i] = freq[j];
                    child[i] = child[j];
                    --i;
                    --j;
                }
                freq[i] = (int)f;
                child[i] = l + 1;
            }
            f = 0;
            for (int i = begin; i < end; ++i)
            {
                j = child[i];
                if (j < 0)
                {
                    sNode[~j] = i;
                }
                else
                {
                    parent[j] = parent[j - 1] = i;
                }
                int g = freq[i];
                if (g == f)
                {
                    block[i] = b;
                }
                else
                {
                    b = block[i] = stock[avail++];
                    edge[b] = i;
                    f = g;
                }
            }
        }

        private int SwapInc(int p)
        {
            int b = block[p];
            int q = edge[b];
            if (q != p)
            {
                int r = child[p];
                int s = child[q];
                child[p] = s;
                child[q] = r;
                if (r >= 0)
                {
                    parent[r] = parent[r - 1] = q;
                }
                else
                {
                    sNode[~r] = q;
                }
                if (s >= 0)
                {
                    parent[s] = parent[s - 1] = p;
                }
                else
                {
                    sNode[~s] = p;
                }
                p = q;

                ++edge[b];
                ++freq[p];
                if (freq[p] == freq[p - 1])
                {
                    block[p] = block[p - 1];
                }
                else
                {
                    block[p] = stock[avail++];
                    edge[block[p]] = p;
                }
            }
            else if (b == block[p + 1])
            {
                ++edge[b];
                ++freq[p];
                if (freq[p] == freq[p - 1])
                {
                    block[p] = block[p - 1];
                }
                else
                {
                    block[p] = stock[avail++];
                    edge[block[p]] = p;
                }
            }
            else if (++freq[p] == freq[p - 1])
            {
                stock[--avail] = b;
                block[p] = block[p - 1];
            }
            return (parent[p]);
        }

        private void UpdateCode(int p)
        {
            if (freq[ROOT_CODE] == 0x8000)
            {
                Reconstruct(0, nMax * 2 - 1);
            }
            ++freq[ROOT_CODE];
            int q = sNode[p];
            do
            {
                q = SwapInc(q);
            } while (q != ROOT_CODE);
        }

        private void UpdatePosition(int p)
        {
            if (totalPosition == 0x8000)
            {
                Reconstruct(ROOT_POSITION, mostPosition + 1);
                totalPosition = freq[ROOT_POSITION];
                freq[ROOT_POSITION] = 0xFFFF;
            }
            int q = sNode[p + N_CHAR];
            while (q != ROOT_POSITION)
            {
                q = SwapInc(q);
            }
            ++totalPosition;
        }

        private void MakeNewNode(int p)
        {
            int r = mostPosition + 1;
            int q = r + 1;
            child[r] = child[mostPosition];
            child[~child[r]] = r;
            child[q] = ~(p + N_CHAR);
            child[mostPosition] = q;
            freq[r] = freq[mostPosition];
            freq[q] = 0;
            block[r] = block[mostPosition];
            if (mostPosition == ROOT_POSITION)
            {
                freq[ROOT_POSITION] = 0xFFFF;
                ++edge[block[ROOT_POSITION]];
            }
            parent[r] = parent[q] = mostPosition;
            block[q] = stock[avail++];
            edge[block[q]] = sNode[p + N_CHAR] = mostPosition = q;
            UpdatePosition(p);
        }

        protected internal override void InitRead()
        {
            FillBitBuffer(2 * CHAR_BIT);
            InitCodeDynamic();
            InitPositionDynamic();
        }

        protected internal override int DecodeCode()
        {
            int c = child[ROOT_CODE];
            int b = bitBuffer;
            int count = 0;
            do
            {
                if ((b & 0x8000) != 0)
                {
                    c = child[c - 1];
                }
                else
                {
                    c = child[c];
                }
                b <<= 1;
                ++count;
                if (count == 16)
                {
                    FillBitBuffer(16);
                    b = bitBuffer;
                    count = 0;
                }
            } while (c > 0);
            FillBitBuffer(count);
            c = ~c;
            UpdateCode(c);
            if (c == n1)
            {
                c += GetBits(8);
            }

            return (c);
        }

        protected internal override int DecodePosition()
        {
            while (decodeCount > nextCount)
            {
                MakeNewNode((int)(nextCount / 64));
                nextCount += 64;
                if (nextCount >= nn)
                {
                    nextCount = 0x00000000FFFFFFFFL;
                }
            }

            int c = child[ROOT_POSITION];
            int b = bitBuffer;
            int count = 0;
            while (c > 0)
            {
                if ((b & 0x8000) != 0)
                {
                    c = child[c - 1];
                }
                else
                {
                    c = child[c];
                }
                b <<= 1;
                ++count;
                if (count == 16)
                {
                    FillBitBuffer(16);
                    b = bitBuffer;
                    count = 0;
                }
            }
            FillBitBuffer(count);
            c = (~c) - N_CHAR;
            UpdatePosition(c);

            return ((c << 6) + GetBits(6));
        }
    }
}
