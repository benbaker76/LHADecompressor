
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LHADecompressor
{
    /**
     * This class is used to read a lha file.<br>
     * <p>
     * <strong>SAMPLE CODE:</strong> read all data from a lha file.<br>
     * 
     * <pre>
     * LzhFile file = new LzhFile(&quot;sample.lzh&quot;);
     * for (Iterator iter = file.entryIterator(); e.hasNext();) {
     * 	InputStream in = file.getInputStream((LzhEntry) e.next());
     * 
     * }
     * </pre>
     * 
     * </p>
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class LhaFile
    {
        private BinaryReader binaryReader;
        private Encoding encoding;
        private string name;
        private List<LhaEntry> entryList;
        private Dictionary<string, LhaEntry> entryMap;
        private int size;
        private long pos;

        /**
         * Opens a lha file for reading.
         * 
         * @param name
         *            the name of lha file
         * @param encoding
         *            character encoding name
         * @throws LhaException
         *             if a lha format error has occurred
         * @throws IOException
         *             if an I/O error has occurred
         */
        public LhaFile(string name, Encoding encoding)
        {
            this.binaryReader = new BinaryReader(File.Open(name, FileMode.Open), encoding);
            this.encoding = encoding;
            this.name = name;
            this.entryList = new List<LhaEntry>();
            this.entryMap = new Dictionary<string, LhaEntry>();
            MakeEntryMap();
        }

        /**
         * Returns the lha file entry for the specified name, or null if not found.
         * 
         * @param name
         *            the name of the entry
         * @return the lha file entry, or null if not found
         */
        public virtual LhaEntry GetEntry(string name)
        {
            return entryMap[name];
        }

        /**
         * Returns the lha file entry for the specified index, or null if not found.
         * 
         * @param index
         *            the index of the entry
         * @return the lha file entry, or null if not found
         */
        public virtual LhaEntry GetEntry(int index)
        {
            return entryList[index];
        }

        /**
         * Returns an input stream for reading the contents of the specified lha
         * file entry.
         * 
         * @param entry
         *            the lha file entry
         * @return the input stream for reading the contents of the specified lha
         *         file entry
         * @throws IOException
         *             if an I/O error has occurred
         */
        public virtual LhaDecoderInputStream GetInputStream(LhaEntry entry)
        {
            return new LhaDecoderInputStream(new LhaFileInputStream(this, entry), entry);
        }

        /**
         * Returns the path name of the lha file.
         * 
         * @return the path name of the lha file
         */
        public virtual string GetName()
        {
            return name;
        }

        /**
         * Returns an iterator of the lha file entries
         * 
         * @return an iterator of the lha file entries
         */
        public IEnumerator<LhaEntry> GetEnumerator()
        {
            return entryList.GetEnumerator();
        }

        /**
         * Reads from the current lha entry into an array of bytes.
         */
        public int Read(long pos, byte[] b, int off, int len)
        {
            if (pos != this.pos)
                binaryReader.BaseStream.Position = pos;
            int n = binaryReader.Read(b, off, len);
            if (n > 0)
                this.pos = pos + n;
            return n;
        }

        /**
         * Reads from the current lha entry.
         */
        public int Read(long pos)
        {
            if (pos != this.pos)
                binaryReader.BaseStream.Position = pos;
            int n = binaryReader.ReadByte();
            if (n > 0)
                this.pos = pos + 1;
            return n;
        }

        /**
         * Returns the number of entries in the lha file.
         * 
         * @return the number of entries in the lha file
         */
        public virtual int Size()
        {
            return this.size;
        }

        /**
         * Closes the lha file
         * 
         * @throws IOException
         *             if an I/O error has occured
         */
        public virtual void Close()
        {
            if (binaryReader != null)
            {
                binaryReader.Close();
                binaryReader = null;
            }
        }

        /**
         * Make entry map in lha file.
         * 
         * @throws LhaException
         *             if a lha format error has occurred
         * @throws IOException
         *             if an I/O error has occured
         */
        private void MakeEntryMap()
        {
            LhaEntryReader lhaEntryReader = new LhaEntryReader(binaryReader, encoding);
            this.size = 0;

            while (true)
            {
                LhaEntry e = lhaEntryReader.ReadHeader();

                if (e == null)
                    break;

                e.SetOffset(binaryReader.BaseStream.Position);
                entryList.Add(e);
                entryMap.Add(e.GetPath(), e);
                ++size;

                int skipSize = (int)e.GetCompressedSize();
                binaryReader.BaseStream.Position += skipSize;
            }
        }

        public byte[] GetEntryBytes(LhaEntry entry)
        {
            byte[] buffer = new byte[entry.GetOriginalSize()];
            LhaDecoderInputStream inputStream = GetInputStream(entry);
            inputStream.Read(buffer, 0, buffer.Length);
            return buffer;
        }

        /*
         * Inner class implementing the input stream used to read a lha file entry.
         */
        internal sealed class LhaFileInputStream : BinaryReader
        {
            private LhaFile file;
            private long pos;
            private long count;

            public LhaFileInputStream(LhaFile file, LhaEntry entry)
                : base(file.binaryReader.BaseStream, file.encoding)
            {
                if (file == null || entry == null)
                {
                    throw new NullReferenceException();
                }
                this.file = file;
                this.pos = entry.GetOffset();
                this.count = entry.GetCompressedSize();

                file.binaryReader.BaseStream.Position = this.pos;
            }

            public override int Read(byte[] b, int off, int len)
            {
                if (count == 0)
                    return (-1);

                if (len > count)
                {
                    if (Int32.MaxValue < count)
                    {
                        len = Int32.MaxValue;
                    }
                    else
                    {
                        len = (int)count;
                    }
                }

                len = file.Read(pos, b, off, len);
                if (len == -1)
                    throw (new LhaException("premature EOF"));

                pos += len;
                count -= len;

                return (len);
            }

            public override int Read()
            {
                if (count == 0)
                {
                    return -1;
                }
                int n = file.Read(pos);
                if (n == -1)
                {
                    throw new LhaException("premature EOF");
                }
                ++pos;
                --count;

                return n;
            }
        }
    }
}
