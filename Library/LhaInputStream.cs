
using System.IO;
using System.Text;

namespace LHADecompressor
{
    /**
     * This class implements an input stream filter for reading data in the lzh
     * file format stream.<br>
     * <p>
     * <strong>SAMPLE CODE:</strong> reads datas from lha file format stream.<br>
     * 
     * <pre>
     * InputStream in;
     * 
     * LzhInputStream lin = new LzhInputStream(in);
     * for (LzhEntry entry = lin.getNextEntry();
     *   entry != null;
     *   entry = lin.getNextEntry()) {
     * 
     * }
     * </pre>
     * 
     * </p>
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class LhaInputStream : BinaryReader
    {
        private LhaDecoderInputStream dis;
        private Encoding encoding;
        private LhaEntry entry;
        private bool closed;

        /**
         * Creates a new lha input stream.
         * 
         * @param in
         *            the actual input stream
         */
        public LhaInputStream(BinaryReader @is)
    : this(@is, Encoding.ASCII)
        {
        }

        /**
         * Creates a new lha input stream.
         * 
         * @param in
         *            the actual input stream
         * @param encoding
         *            character encoding name
         */
        public LhaInputStream(BinaryReader @in, Encoding encoding)
    : base(@in.BaseStream, encoding)
        {
            this.encoding = encoding;
            this.dis = null;
            this.entry = null;
            this.closed = true;
        }

        /**
         * Reads the next lha entry and positions stream at the beginning of the
         * entry data.
         * 
         * @param name
         *            the name of the entry
         * @return the LzhEntry just read
         * @throws LhaException
         *             if a lha format error has occurred
         * @throws IOException
         *             if an I/O error has occured
         */
        public virtual LhaEntry GetNextEntry()
        {
            if (entry != null)
            {
                dis.CloseEntry();
            }
            LhaEntryReader lhaEntryReader = new LhaEntryReader(this, encoding);
            entry = lhaEntryReader.ReadHeader();

            if (entry != null)
            {
                dis = new LhaDecoderInputStream(this, entry);
                closed = false;
            }
            else
            {
                dis = null;
                closed = true;
            }
            return entry;
        }

        /**
         * Closes the current input stream.
         * 
         * @throws IOException
         *             if an I/O error has occured
         */
        private void EnsureOpen()
        {
            if (closed)
            {
                throw new IOException("Stream closed");
            }
        }

        /**
         * Returns 0 after EOF has reached for the current input stream, otherwise
         * always return 1. Programs should not count on this method to return the
         * actual number of bytes that could be read without blocking.
         * 
         * @return 1 before EOF and 0 after EOF has reached for input stream.
         * @throws IOException
         *             if an I/O error has occurred
         */
        public int Available()
        {
            EnsureOpen();
            int result = dis.Available();
            return result;
        }

        /**
         * Reads from the current input stream.
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
            EnsureOpen();
            return dis.Read();
        }

        /**
         * Reads from the current input stream into an array of bytes.
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
            EnsureOpen();
            int result = dis.Read(b, off, len);
            return result;
        }

        /**
         * Returns 0 after EOF has reached for the current input stream, otherwise
         * always return 1. Programs should not count on this method to return the
         * actual number of bytes that could be read without blocking.
         * 
         * @param n
         *            the number of bytes to skip
         * @return the actual number of bytes skipped
         * @throws IOException
         *             if an I/O error has occurred
         */
        public long Skip(long n)
        {
            EnsureOpen();
            return dis.Skip(n);
        }

        /**
         * Closes the current input stream.
         * 
         * @throws IOException
         *             if an I/O error has occured
         */
        public override void Close()
        {
            if (dis != null)
            {
                closed = true;
                dis.Close();
                dis = null;
            }
            base.Close();
        }

        internal static BinaryReader Access(LhaInputStream input)
        {
            return input;
        }
    }
}
