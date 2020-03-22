
using System;
using System.Globalization;

namespace LHADecompressor
{
    /**
     * This class is used to represent a lha file entry.
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class LhaEntry
    {
        // Method signatures
        /** The compression method signature for directory */
        public const string METHOD_SIG_LHD = "-lhd-";
        /** The compression method signature for lh0 */
        public const string METHOD_SIG_LH0 = "-lh0-";
        /** The compression method signature for lh1 */
        public const string METHOD_SIG_LH1 = "-lh1-";
        /** The compression method signature for lh2 */
        public const string METHOD_SIG_LH2 = "-lh2-";
        /** The compression method signature for lh3 */
        public const string METHOD_SIG_LH3 = "-lh3-";
        /** The compression method signature for lh4 */
        public const string METHOD_SIG_LH4 = "-lh4-";
        /** The compression method signature for lh5 */
        public const string METHOD_SIG_LH5 = "-lh5-";
        /** The compression method signature for lh6 */
        public const string METHOD_SIG_LH6 = "-lh6-";
        /** The compression method signature for lh7 */
        public const string METHOD_SIG_LH7 = "-lh7-";
        /** The compression method signature for lzs */
        public const string METHOD_SIG_LZS = "-lzs-";
        /** The compression method signature for lz4 */
        public const string METHOD_SIG_LZ4 = "-lz4-";
        /** The compression method signature for lz5 */
        public const string METHOD_SIG_LZ5 = "-lz5-";

        // OS id signatures
        /** The operation system signature for generic */
        public const byte OSID_SIG_GENERIC = 0x00;
        /** The operation system signature for MS-DOS */
        public const byte OSID_SIG_MSDOS = 0x4D;
        /** The operation system signature for OS/2 */
        public const byte OSID_SIG_OS2 = 0x32;
        /** The operation system signature for OS9 */
        public const byte OSID_SIG_OS9 = 0x39;
        /** The operation system signature for OS/68K */
        public const byte OSID_SIG_OS68K = 0x4B;
        /** The operation system signature for OS/386 */
        public const byte OSID_SIG_OS386 = 0x33;
        /** The operation system signature for HUMAN. */
        public const byte OSID_SIG_HUMAN = 0x48;
        /** The operation system signature for Unix */
        public const byte OSID_SIG_UNIX = 0x55;
        /** The operation system signature for CP/M */
        public const byte OSID_SIG_CPM = 0x43;
        /** The operation system signature for Flex */
        public const byte OSID_SIG_FLEX = 0x46;
        /** The operation system signature for Macintosh */
        public const byte OSID_SIG_MAC = 0x6D;
        /** The operation system signature for Runser */
        public const byte OSID_SIG_RUNSER = 0x52;
        /** The operation system signature for Java */
        public const byte OSID_SIG_JAVA = 0x4A;
        /** The operation system signature for Windows95 (from UNLHA32.DLL) */
        public const byte OSID_SIG_WIN32 = 0x77;
        /** The operation system signature for WindowsNT (from UNLHA32.DLL) */
        public const byte OSID_SIG_WINNT = 0x57;

        // Extend header signatures
        /** The extend header signature: header crc and information */
        public const byte EXHDR_SIG_COMMON = 0x00;
        /** The extend header signature: file name */
        public const byte EXHDR_SIG_FILENAME = 0x01;
        /** The extend header signature: directory name */
        public const byte EXHDR_SIG_DIRNAME = 0x02;
        /** The extend header signature: comment */
        public const byte EXHDR_SIG_COMMENT = 0x3f;
        /** The extend header signature: ms-dos attributes */
        public const byte EXHDR_SIG_DOSATTR = 0x40;
        /** The extend header signature: ms-dos time stamps (from UNLHA32.DLL) */
        public const byte EXHDR_SIG_DOSTIMES = 0x41;
        /** The extend header signature: unix permisson */
        public const byte EXHDR_SIG_UNIXPERM = 0x50;
        /** The extend header signature: unix group id,user id */
        public const byte EXHDR_SIG_UNIXID = 0x51;
        /** The extend header signature: unix group name */
        public const byte EXHDR_SIG_UNIXGROUPNAME = 0x52;
        /** The extend header signature: unix user name */
        public const byte EXHDR_SIG_UNIXUSERNAME = 0x53;
        /** The extend header signature: unix last modified time */
        public const byte EXHDR_SIG_UNIXLMTIME = 0x54;

        /** method ID */
        protected String method;
        /** compressed size ID */
        protected long compressedSize;
        /** original size */
        protected long originalSize;
        /** time stamp */
        protected DateTime timeStamp;
        /** file path and name */
        protected string path;
        /** file crc */
        protected int crc;
        /** file crc flag */
        protected bool fcrc = false;
        /** os type */
        protected byte os;
        /** offset of compressed data from beginning of lzh file */
        protected long offset = -1;

        /**
         * Creates a new lha entry.
         */
        public LhaEntry()
        {
            fcrc = false;
            offset = -1L;
        }

        /**
         * Sets the compress method id string.
         * 
         * @param method
         *            the compress method id string
         * @throws IllegalArgumentException
         *             if the compress method id is not supported
         * @see #getMethod()
         */
        protected internal virtual void SetMethod(string method)
        {
            if (!method.Equals(METHOD_SIG_LHD) &&
                !method.Equals(METHOD_SIG_LH0) &&
                !method.Equals(METHOD_SIG_LH1) &&
                !method.Equals(METHOD_SIG_LH2) &&
                !method.Equals(METHOD_SIG_LH3) &&
                !method.Equals(METHOD_SIG_LH4) &&
                !method.Equals(METHOD_SIG_LH5) &&
                !method.Equals(METHOD_SIG_LH6) &&
                !method.Equals(METHOD_SIG_LH7) &&
                !method.Equals(METHOD_SIG_LZS) &&
                !method.Equals(METHOD_SIG_LZ4) &&
                !method.Equals(METHOD_SIG_LZ5))
            {
                throw new ArgumentException("Invalid lzh entry method " + method);
            }
            this.method = method;
        }

        /**
         * Returns the compress method id string.
         * 
         * @return the compress method id string
         * @see #setMethod(String)
         */
        public virtual string GetMethod()
        {
            return method;
        }

        /**
         * Sets the compressed size.
         * 
         * @param compressedSize
         *            the compressed data size
         * @throws IllegalArgumentException
         *             if the compressed data size is less than 0 or greater than
         *             0xFFFFFFFF
         * @see #getCompressedSize()
         */
        protected internal virtual void SetCompressedSize(long compressedSize)
        {
            if (compressedSize < 0 || compressedSize > 0xFFFFFFFFL)
            {
                throw new ArgumentException("Invalid lzh entry compressed data size");
            }
            this.compressedSize = compressedSize;
        }

        /**
         * Returns the compressed data size.
         * 
         * @return the compressed data size
         * @see #setCompressedSize(long)
         */
        public virtual long GetCompressedSize()
        {
            return compressedSize;
        }

        /**
         * Sets the original data size.
         * 
         * @param originalSize
         *            the original data size
         * @throws IllegalArgumentException
         *             if the original data size is less than 0 or greater than
         *             0xFFFFFFFF
         * @see #getOriginalSize()
         */
        protected internal virtual void SetOriginalSize(long originalSize)
        {
            if (originalSize < 0 || originalSize > 0xFFFFFFFFL)
            {
                throw new ArgumentException("Invalid lha entry original data size");
            }
            this.originalSize = originalSize;
        }

        /**
         * Returns the original size.
         * 
         * @return the original size
         * @see #setOriginalSize(long)
         */
        public virtual long GetOriginalSize()
        {
            return originalSize;
        }

        /**
         * Sets the time stamp of data.
         * 
         * @param timeStamp
         *            the time stamp of data
         * @see #getTimeStamp()
         */
        protected internal virtual void SetTimeStamp(DateTime timeStamp)
        {
            this.timeStamp = timeStamp;
        }

        /**
         * Returns the time stamp of data.
         * 
         * @return the time stamp of data
         * @see #setTimeStamp(Date)
         */
        public virtual DateTime GetTimeStamp()
        {
            return timeStamp;
        }

        /**
         * Sets the MS-DOS time stamp of data.
         * 
         * @param timeStamp
         *            the MS-DOS time stamp of data
         * @see #getDosTimeStamp()
         */
        protected internal virtual void SetDosTimeStamp(long tstamp)
        {
            this.timeStamp = DosToDotNetTime(tstamp);
        }

        /**
         * Returns the MS-DOS time stamp of data.
         * 
         * @return the MS-DOS time stamp of data
         * @see #setDosTimeStamp(long)
         */
        public virtual long GetDosTimeStamp()
        {
            return DotNetToDosTime(timeStamp);
        }

        /**
         * Sets the unix time stamp of header.
         * 
         * @param timeStamp
         *            the unix time stamp of header
         * @see #getHeaderTimeStamp()
         */
        protected internal virtual void SetHeaderTimeStamp(long l)
        {
            this.timeStamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            this.timeStamp = this.timeStamp.AddSeconds(l).ToLocalTime();
        }

        /**
         * Returns the unix time stamp of header.
         * 
         * @return the unix time stamp of header
         * @see #setHeaderTimeStamp(long)
         */
        public virtual long GetHeaderTimeStamp()
        {
            if (timeStamp == null)
            {
                return -1L;
            }
            long time = timeStamp.ToFileTime();
            return time;
        }

        /**
         * Returns the File/Name by string.
         * 
         * @param name
         *            the string of File/Name
         * @see #setFile(File)
         * @see #getFile()
         */
        protected internal virtual void SetPath(string path)
        {
            this.path = path;
        }

        /**
         * Returns the File/Name.
         * 
         * @return the File/Name
         * @see #setFile(String)
         * @see #setFile(File)
         */
        public virtual string GetPath()
        {
            return path;
        }

        /**
         * Sets the CRC value.
         * 
         * @param crc
         *            the CRC value
         * @see #getCRC()
         * @see #hasCRC()
         */
        protected internal virtual void SetCRC(int crc)
        {
            this.fcrc = true;
            this.crc = crc;
        }

        /**
         * Returns the CRC value. Before use this method, you should check this
         * entry has CRC or not.
         * 
         * @return the CRC value
         * @see #setCRC(int)
         * @see #hasCRC()
         */
        public virtual int GetCRC()
        {
            return crc;
        }

        /**
         * Returns this entry has CRC or not.
         * 
         * @return true if this entry has CRC.
         * @see #setCRC(int)
         * @see #getCRC()
         */
        public virtual bool HasCRC()
        {
            return fcrc;
        }

        /**
         * Sets the Operation System signature.
         * 
         * @param os
         *            the Operation System signature
         * @see #getOS()
         */
        protected internal virtual void SetOS(byte os)
        {
            this.os = os;
        }

        /**
         * Returns the Operation System signature.
         * 
         * @return the Operation System signature
         * @see #setOS(byte)
         */
        public virtual byte GetOS()
        {
            return os;
        }

        /**
         * Returns a file path or a name of the lha entry.
         */
        public override string ToString()
        {
            return path;
        }

        /**
         * Sets the offset of compression data in file. Be carefull the offset is
         * not offset in lha entry.
         * 
         * @param offset
         *            the offset of compression data in file
         * @throws IllegalArgumentException
         *             if the length of the offset is less than 0 or greater than
         *             0xFFFFFFFF
         * @see #getOffset()
         */
        protected internal virtual void SetOffset(long offset)
        {
            if (offset < 0 || offset > 0xFFFFFFFFL)
            {
                throw new ArgumentException("Invalid lzh entry offset");
            }
            this.offset = offset;
        }

        /**
         * Returns the offset of compression data in file. Be carefull the offset is
         * not offset in lha entry.
         * 
         * @return the offset from in file
         * @see #setOffset(long)
         */
        public virtual long GetOffset()
        {
            return offset;
        }

        /**
         * Converts MS-DOS time to .NET time.
         */
        private static DateTime DosToDotNetTime(long dtime)
        {
            return new DateTime((int)(((dtime >> 25) & 0x7f) + 1980),
                (int)((dtime >> 21) & 0x0f), (int)((dtime >> 16) & 0x1f),
                (int)((dtime >> 11) & 0x1f), (int)((dtime >> 5) & 0x3f),
                (int)((dtime << 1) & 0x3e));
        }

        /**
         * Converts .NET time to MS-DOS time.
         */
        private static long DotNetToDosTime(DateTime dateTime)
        {
            int year = dateTime.Year;
            if (year < 1980)
                return (1 << 21) | (1 << 16);
            return ((year - 60) << 25) | (dateTime.Month << 21) | (dateTime.Day << 16) | (dateTime.Hour << 11) | (dateTime.Minute << 5) | (dateTime.Second >> 1);
        }
    }
}
