using System;
using System.IO;

namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public abstract class SlidingDicDecoder : LhaDecoder
    {
        protected internal const int OFFSET = 0x100 - 3;
        protected internal const int UNSIGNED_CHAR_MAX = 255;
        protected internal const int CHAR_BIT = 8;
        protected internal const int THRESHOLD = 3;
        protected internal const int MAX_MATCH = 256;

        protected internal BinaryReader @in;
        protected internal long originalSize;
        protected internal long decodeCount;

        protected internal int positionAdjust;
        protected internal int dictionarySize;
        protected internal int dictionaryMask;
        protected internal byte[] dictionaryBuffer;
        protected internal int bufferPointerBegin;
        protected internal int bufferPointerEnd;

        protected internal int bitBuffer;
        protected internal int subBitBuffer;
        protected internal int bitCount;

        protected internal bool needInitialRead;

        protected internal abstract void InitRead();
        protected internal abstract int DecodeCode();
        protected internal abstract int DecodePosition();

        public SlidingDicDecoder(BinaryReader @in, long originalSize, int dictionaryBit, int positionAdjust)
        {
            this.@in = @in;
            this.originalSize = originalSize;
            this.decodeCount = 0L;

            this.positionAdjust = positionAdjust;
            this.dictionarySize = 1 << dictionaryBit;
            this.dictionaryMask = dictionarySize - 1;
            this.dictionaryBuffer = new byte[dictionarySize];
            this.bufferPointerBegin = 0;
            this.bufferPointerEnd = 0;

            this.bitBuffer = 0;
            this.subBitBuffer = 0;
            this.bitCount = 0;

            this.needInitialRead = true;
        }

        public virtual void Close()
        {
        }

        public virtual int Read(byte[] b, int off, int len)
        {
            if (needInitialRead)
            {
                InitRead();
                needInitialRead = false;
            }

            int sl = len;
            int rs = bufferPointerEnd - bufferPointerBegin;

            if (rs < 0)
            {
                int bl = dictionarySize - bufferPointerBegin;

                if (bl >= len)
                {
                    Array.Copy(dictionaryBuffer, bufferPointerBegin, b, off, len);
                    bufferPointerBegin += len;
                    if (bufferPointerBegin == dictionarySize)
                    {
                        bufferPointerBegin = 0;
                    }

                    return (sl);
                }
                else
                {
                    Array.Copy(dictionaryBuffer, bufferPointerBegin, b, off, bl);
                    off += bl;
                    len -= bl;
                    bufferPointerBegin = 0;

                    if (bufferPointerEnd >= len)
                    {
                        Array.Copy(dictionaryBuffer, 0, b, off, len);
                        bufferPointerBegin = len;
                        return (sl);
                    }
                    else if (bufferPointerEnd != 0)
                    {
                        Array.Copy(dictionaryBuffer, 0, b, off, bufferPointerEnd);
                        off += bufferPointerEnd;
                        len -= bufferPointerEnd;
                        bufferPointerBegin = bufferPointerEnd;
                    }
                }
            }
            else if (rs >= len)
            {
                Array.Copy(dictionaryBuffer, bufferPointerBegin, b, off, len);
                bufferPointerBegin += len;
                return (sl);
            }
            else if (rs != 0)
            {
                Array.Copy(dictionaryBuffer, bufferPointerBegin, b, off, rs);
                off += rs;
                len -= rs;
                bufferPointerBegin = bufferPointerEnd;
            }

            if (originalSize <= decodeCount)
            {
                int l = sl - len;
                return l > 0 ? l : -1;
            }

            while ((decodeCount < originalSize) && (len > 0))
            {
                int c = DecodeCode();

                if (c <= UNSIGNED_CHAR_MAX)
                {
                    ++decodeCount;
                    --len;
                    ++bufferPointerBegin;
                    dictionaryBuffer[bufferPointerEnd++] = b[off++] = (byte)c;
                    if (bufferPointerEnd == dictionarySize)
                    {
                        bufferPointerBegin = bufferPointerEnd = 0;
                    }
                }
                else
                {
                    int matchLength = c - positionAdjust;
                    int matchOffset = DecodePosition();
                    int matchPosition = (bufferPointerEnd - matchOffset - 1) & dictionaryMask;
                    decodeCount += matchLength;

                    for (int k = 0; k < matchLength; ++k)
                    {
                        byte t = dictionaryBuffer[(matchPosition + k) & dictionaryMask];
                        dictionaryBuffer[bufferPointerEnd++] = t;
                        if (len > 0)
                        {
                            --len;
                            ++bufferPointerBegin;
                            if (bufferPointerBegin == dictionarySize)
                            {
                                bufferPointerBegin = 0;
                            }
                            b[off++] = t;
                        }

                        if (bufferPointerEnd == dictionarySize)
                        {
                            bufferPointerEnd = 0;
                        }
                    }
                }
            }

            return (sl - len);
        }

        protected internal void FillBitBuffer(int n)
        {
            while (n > bitCount)
            {
                n -= bitCount;
                bitBuffer = (bitBuffer << bitCount) + (int)((uint)subBitBuffer >> (CHAR_BIT - bitCount));

                int c = @in.Read();
                subBitBuffer = (c > 0) ? c : 0;
                bitCount = CHAR_BIT;
            }

            bitCount -= n;
            bitBuffer = ((bitBuffer << n) + (int)((uint)(subBitBuffer >> (CHAR_BIT - n)))) & 0xFFFF;
            subBitBuffer = (subBitBuffer << n) & 0x00FF;
        }

        protected internal int GetBits(int n)
        {
            int x = (int)((uint)bitBuffer >> (2 * CHAR_BIT - n));
            FillBitBuffer(n);

            return (x);
        }
    }
}
