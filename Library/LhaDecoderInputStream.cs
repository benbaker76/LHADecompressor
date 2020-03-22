using System;
using System.IO;

namespace LHADecompressor
{
    /**
     * This class implements an input stream for reading datas with lha
     * decoder.<br>
     * 
     * Supports decompression methods are lhd, lh0, lh1, lh4, lh5, lh6, lh7, lz4, lz5, lzs.<br>
     * 
     * This class is NOT THREAD SAFE.<br>
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class LhaDecoderInputStream : BinaryReader
    {
        private const int SIZE_SKIP_BUFFER = 512;

        protected internal LhaDecoder decoder;
        protected internal LhaEntry entry;
        protected internal Checksum crc;
        protected internal byte[] skipBuffer;
        protected internal long entryCount;

        /**
         * Creates a new lha input stream.
         * 
         * @param in
         *            the actual input stream
         * @param entry
         *            the lha entry of current input stream
         */
        public LhaDecoderInputStream(BinaryReader @in, LhaEntry entry)
    : base(@in.BaseStream)
        {
            this.entry = entry;
            this.crc = new CRC16();
            this.skipBuffer = new byte[512];
            this.entryCount = entry.GetOriginalSize();
            this.decoder = CreateDecoder(@in, entryCount, entry.GetMethod());
            crc.Reset();
        }

        /**
         * Gets the checksum of input stream.
         * 
         * @return the checksum of input stream
         */
        public virtual Checksum GetChecksum()
        {
            return crc;
        }

        /**
         * Returns 0 after EOF has reached for the current entry data, otherwise
         * always return 1. Programs should not count on this method to return the
         * actual number of bytes that could be read without blocking.
         * 
         * @return 1 before EOF and 0 after EOF has reached for input stream.
         * @throws IOException
         *             if an I/O error has occurred
         */
        public int Available()
        {
            if (decoder == null)
                return 0;

            return (entryCount > 0 ? 1 : 0);
        }

        /**
         * Reads from the input stream.
         * 
         * @param pos
         *            the offset in lha file
         * @return the next byte of data, or <code>-1</code> if the end of the
         *         stream is reached
         * @throws IOException
         *             if an I/O error has occurred
         */
        public override int Read()
        {
            if (decoder == null)
            {
                return (-1);
            }

            byte[] b = new byte[1];
            int n = decoder.Read(b, 0, 1);
            if (n > 0)
            {
                crc.Update(b[0]);
                --entryCount;
            }

            return (b[0]);
        }

        /**
         * Reads from the input stream into an array of bytes.
         * 
         * @param pos
         *            the offset in lha file
         * @param b
         *            the buffer into which the data is read
         * @param off
         *            the start offset in array <code>b</code> at which the data
         *            is written
         * @param len
         *            the maximum number of bytes to read
         * @return the total number of bytes read into the buffer, or
         *         <code>-1</code> if there is no more data because the end of the
         *         stream has been reached
         * @throws IOException
         *             if an I/O error has occurred
         */
        public override int Read(byte[] b, int off, int len)
        {
            if (decoder == null)
            {
                return (-1);
            }

            int n = decoder.Read(b, off, len);
            if (n > 0)
            {
                crc.Update(b, off, n);
                entryCount -= n;
            }

            return (n);
        }

        /**
         * Returns 0 after EOF has reached for the input stream, otherwise always
         * return 1. Programs should not count on this method to return the actual
         * number of bytes that could be read without blocking.
         * 
         * @param n
         *            the number of bytes to skip
         * @return the actual number of bytes skipped
         * @throws IOException
         *             if an I/O error has occurred
         */
        public long Skip(long n)
        {
            if (n <= 0)
            {
                return (0);
            }

            if (n > Int32.MaxValue)
            {
                n = Int32.MaxValue;
            }
            int total = 0;
            while (total < n)
            {
                int len = (int)n - total;
                if (len > skipBuffer.Length)
                {
                    len = skipBuffer.Length;
                }

                len = Read(skipBuffer, 0, len);
                if (len == -1)
                {
                    break;
                }
                else
                {
                    crc.Update(skipBuffer, 0, len);
                }

                total += len;
            }

            entryCount -= total;
            return (total);
        }

        /**
         * Closes the input stream.
         * 
         * @throws IOException
         *             if an I/O error has occured
         */
        public override void Close()
        {
            if (decoder != null)
            {
                decoder.Close();
                decoder = null;
                entry = null;
                crc = null;
            }
        }

        /**
         * Closes the input stream.
         * 
         * @throws IOException
         *             if an I/O error has occured
         */
        public virtual void CloseEntry()
        {
            long skipCount = Skip(entryCount);
            if (entryCount != skipCount)
            {
                throw new LhaException("Data length not matched");
            }
            if (entry.HasCRC() && entry.GetCRC() != crc.GetValue())
            {
                throw new LhaException("Data crc is not matched");
            }
            Close();
        }

        /**
         * Creates a new decoder for input stream.
         */
        private static LhaDecoder CreateDecoder(BinaryReader @in, long originalSize, string method)
        {
            if (method.Equals(LhaEntry.METHOD_SIG_LHD))
                return new LhdDecoder();
            else if (method.Equals(LhaEntry.METHOD_SIG_LH0))
                return new NocompressDecoder(@in, originalSize);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH1))
                return new Lh1Decoder(@in, originalSize);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH2))
                throw new LhaException("Unsupported method: " + method);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH3))
                throw new LhaException("Unsupported method: " + method);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH4))
                return new Lh4Decoder(@in, originalSize, 12, 14, 4);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH5))
                return new Lh4Decoder(@in, originalSize, 13, 14, 4);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH6))
                return new Lh4Decoder(@in, originalSize, 15, 16, 5);
            else if (method.Equals(LhaEntry.METHOD_SIG_LH7))
                return new Lh4Decoder(@in, originalSize, 16, 17, 5);
            else if (method.Equals(LhaEntry.METHOD_SIG_LZS))
                return new LzsDecoder(@in, originalSize);
            else if (method.Equals(LhaEntry.METHOD_SIG_LZ4))
                return new NocompressDecoder(@in, originalSize);
            else if (method.Equals(LhaEntry.METHOD_SIG_LZ5))
                return new Lz5Decoder(@in, originalSize);

            throw new LhaException("Unknown method: " + method);
        }
    }
}
