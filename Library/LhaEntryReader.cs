
using System.IO;
using System.Text;

namespace LHADecompressor
{
    public class LhaEntryReader
    {
        protected internal const char HD_CHR_DELIM_MSDOS = '\\';
        protected internal const char HD_CHR_DELIM_UNIX = '/';
        protected internal const char HD_CHR_DELIM_MAC = ':';
        protected internal const byte HD_CHR_DELIM_EXTRA = byte.MaxValue;

        protected internal const int HDRU_SIZE = 21;
        protected internal const int HDRU_OFF_HEADERSIZE = 0;
        protected internal const int HDRU_OFF_LVL = 20;
        protected internal const int HDR0_OFF_SUM = 1;
        protected internal const int HDR0_OFF_METHOD = 2;
        protected internal const int HDR0_OFF_COMPSIZE = 7;
        protected internal const int HDR0_OFF_ORIGSIZE = 11;
        protected internal const int HDR0_OFF_TIMESTAMP = 15;
        protected internal const int HDR0_OFF_FILEATTR = 19;

        protected internal const int HDR1_OFF_SUM = 1;
        protected internal const int HDR1_OFF_METHOD = 2;
        protected internal const int HDR1_OFF_SKIPSIZE = 7;
        protected internal const int HDR1_OFF_ORIGSIZE = 11;
        protected internal const int HDR1_OFF_TIMESTAMP = 15;
        protected internal const int HDR1_OFF_19 = 19;

        protected internal const int HDR2_OFF_METHOD = 2;
        protected internal const int HDR2_OFF_COMPSIZE = 7;
        protected internal const int HDR2_OFF_ORIGSIZE = 11;
        protected internal const int HDR2_OFF_TIMESTAMP = 15;
        protected internal const int HDR2_OFF_RESERVED = 19;

        protected internal const byte HDR_SIG_LVL0 = 0;
        protected internal const byte HDR_SIG_LVL1 = 1;
        protected internal const byte HDR_SIG_LVL2 = 2;

        protected internal BinaryReader binaryReader;
        protected internal Encoding encoding;
        protected internal int srcSum;
        protected internal int srcCRC;
        protected internal Checksum calcSum;
        protected internal Checksum calcCRC;
        protected internal bool flagSum;
        protected internal bool flagCRC;
        protected internal string fileName = "";
        protected internal string dirName = "";

        public LhaEntryReader(BinaryReader binaryReader, Encoding enc)
        {
            this.binaryReader = binaryReader;
            this.encoding = enc;
            this.fileName = "";
            this.calcSum = new Sum();
            this.calcCRC = new CRC16();
        }

        public virtual LhaEntry ReadHeader()
        {
            byte[] @base = new byte[HDRU_SIZE];

            calcSum.Reset();
            calcCRC.Reset();
            flagSum = false;
            flagCRC = false;
            fileName = "";
            dirName = "";

            int n = binaryReader.Read(@base, 0, @base.Length);
            if ((n <= 0) || ((n == 1) && (@base[HDRU_OFF_HEADERSIZE] == 0)))
            {
                // if((n==1)&&(base[HDRU_OFF_HEADERSIZE]==0))
                return (null);
            }
            else if (n != HDRU_SIZE)
            {
                throw (new LhaException("header is broken (header size does'nt match)"));
            }

            LhaEntry e;
            switch (@base[HDRU_OFF_LVL])
            {
                case HDR_SIG_LVL0:
                    e = ReadHeader_Lv0(@base);
                    break;

                case HDR_SIG_LVL1:
                    e = ReadHeader_Lv1(@base);
                    break;

                case HDR_SIG_LVL2:
                    e = ReadHeader_Lv2(@base);
                    break;

                default:
                    throw (new LhaException("Unsupported Lha header level: " + @base[HDRU_OFF_LVL]));
            }

            if ((e.GetMethod().Equals(LhaEntry.METHOD_SIG_LHD))
                    && (e.GetPath().Length == 0))
            {
                throw (new LhaException("Lha header is broken (file name length is zero)"));
            }

            if (flagSum && (srcSum != calcSum.GetValue()))
            {
                throw (new LhaException("Lha header is broken (header check sum doesn't match)"));
            }

            if (flagCRC && (srcCRC != calcCRC.GetValue()))
            {
                throw (new LhaException("Lha header is broken (header crc doesn't match"));
            }

            return (e);
        }
        protected internal virtual LhaEntry ReadHeader_Lv0(byte[] @base)
        {
            LhaEntry e = new LhaEntry();

            flagSum = true;

            int headerSize = @base[HDRU_OFF_HEADERSIZE];
            srcSum = @base[HDR0_OFF_SUM];
            if (srcSum < 0)
            {
                srcSum += 256;
            }
            e.SetMethod(encoding.GetString(@base, HDR0_OFF_METHOD, 5));
            e.SetCompressedSize(Get32(@base, HDR0_OFF_COMPSIZE));
            e.SetOriginalSize(Get32(@base, HDR0_OFF_ORIGSIZE));
            e.SetDosTimeStamp(Get32(@base, HDR0_OFF_TIMESTAMP));

            calcSum.Update(@base, 2, @base.Length - 2);

            byte[] buf = new byte[1];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (header size does'nt match)"));
            }
            int nameSize = buf[0];
            calcSum.Update(buf, 0, buf.Length);

            buf = new byte[nameSize];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read name)"));
            }
            string name = encoding.GetString(buf);
            calcSum.Update(buf, 0, buf.Length);

            int diff = headerSize - nameSize;
            if ((diff != 20) && (diff != 22) && (diff < 23))
            {
                throw (new LhaException("Lha header is broken (header size does'nt match)"));
            }

            e.SetOS(LhaEntry.OSID_SIG_GENERIC);

            if (diff >= 22)
            {
                buf = new byte[2];
                if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
                {
                    throw (new LhaException("Lha header is broken (cannot read crc value)"));
                }
                e.SetCRC(Get16(buf, 0));
                calcSum.Update(buf, 0, buf.Length);
            }

            if (diff >= 23)
            {
                buf = new byte[1];
                if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
                {
                    throw (new LhaException("Lha header is broken (cannot read os signature)"));
                }
                e.SetOS(buf[0]);
                calcSum.Update(buf, 0, buf.Length);
            }

            if (diff > 23)
            {
                buf = new byte[diff - 24];
                if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
                {
                    throw (new LhaException("Lha header is broken (cannot read ext)"));
                }
                calcSum.Update(buf, 0, buf.Length);
            }

            e.SetPath(ConvertFilePath(name, e.GetOS()));

            return (e);
        }

        protected internal virtual LhaEntry ReadHeader_Lv1(byte[] @base)
        {
            LhaEntry e = new LhaEntry();

            flagSum = true;

            srcSum = @base[HDR1_OFF_SUM];
            if (srcSum < 0)
            {
                srcSum += 256;
            }
            e.SetMethod(encoding.GetString(@base, HDR1_OFF_METHOD, 5));
            e.SetOriginalSize(Get32(@base, HDR1_OFF_ORIGSIZE));
            e.SetDosTimeStamp(Get32(@base, HDR1_OFF_TIMESTAMP));

            if (@base[HDR1_OFF_19] != 0x20)
            {
                throw (new LhaException("Lha header is broken (offset 19 is not 0x20)"));
            }

            calcSum.Update(@base, 2, @base.Length - 2);
            calcCRC.Update(@base, 0, @base.Length);

            byte[] buf = new byte[1];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read name size)"));
            }
            int nameSize = buf[0];
            calcSum.Update(buf, 0, buf.Length);
            calcCRC.Update(buf, 0, buf.Length);

            string name = "";
            if (nameSize > 0)
            {
                buf = new byte[nameSize];
                if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
                {
                    throw (new LhaException("Lha header is broken (cannot read name)"));
                }
                name = encoding.GetString(buf);
                calcSum.Update(buf, 0, buf.Length);
                calcCRC.Update(buf, 0, buf.Length);
            }

            buf = new byte[2];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read crc value)"));
            }
            e.SetCRC(Get16(buf, 0));
            calcSum.Update(buf, 0, buf.Length);
            calcCRC.Update(buf, 0, buf.Length);

            buf = new byte[1];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read os signature)"));
            }
            e.SetOS(buf[0]);
            calcSum.Update(buf, 0, buf.Length);
            calcCRC.Update(buf, 0, buf.Length);

            long extSize = 0;
            buf = new byte[2];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read ext)"));
            }
            calcSum.Update(buf, 0, buf.Length);
            calcCRC.Update(buf, 0, buf.Length);
            for (int next = Get16(buf, 0); next > 0; next = ReadExHeader(e, next))
            {
                extSize += next;
            }

            e.SetCompressedSize(Get32(@base, HDR0_OFF_COMPSIZE) - extSize);
            if (fileName.Length > 0)
            {
                name = dirName + fileName;
            }
            else
            {
                name = ConvertFilePath(name, e.GetOS());
            }

            e.SetPath(name);

            return (e);
        }

        protected internal virtual LhaEntry ReadHeader_Lv2(byte[] @base)
        {
            LhaEntry e = new LhaEntry();

            e.SetMethod(encoding.GetString(@base, HDR2_OFF_METHOD, 5));
            e.SetCompressedSize(Get32(@base, HDR2_OFF_COMPSIZE));
            e.SetOriginalSize(Get32(@base, HDR2_OFF_ORIGSIZE));
            e.SetHeaderTimeStamp(Get32(@base, HDR2_OFF_TIMESTAMP));

            calcCRC.Update(@base, 0, @base.Length);

            byte[] buf = new byte[2];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read crc value)"));
            }
            e.SetCRC(Get16(buf, 0));
            calcCRC.Update(buf, 0, buf.Length);

            buf = new byte[1];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read os signature)"));
            }
            e.SetOS(buf[0]);
            calcCRC.Update(buf, 0, buf.Length);

            buf = new byte[2];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
            {
                throw (new LhaException("Lha header is broken (cannot read ext)"));
            }
            calcCRC.Update(buf, 0, buf.Length);
            for (int next = Get16(buf, 0); next > 0; next = ReadExHeader(e, next)) ;

            e.SetPath(dirName + fileName);

            return (e);
        }

        protected internal virtual int ReadExHeader(LhaEntry e, int size)
        {
            byte[] buf = new byte[size];
            if (binaryReader.Read(buf, 0, buf.Length) != buf.Length)
                throw (new LhaException("header is broken"));

            switch (buf[0])
            {
                case LhaEntry.EXHDR_SIG_COMMON:
                    flagCRC = true;
                    srcCRC = Get16(buf, 1);
                    buf[1] = 0x00;
                    buf[2] = 0x00;
                    break;

                case LhaEntry.EXHDR_SIG_FILENAME:
                    fileName = encoding.GetString(buf, 1, size - 3);
                    break;

                case LhaEntry.EXHDR_SIG_DIRNAME:
                    StringBuilder dname = new StringBuilder();
                    int pi = 0;
                    for (int i = 1; i <= (size - 3); ++i)
                    {
                        if (buf[i] == HD_CHR_DELIM_EXTRA)
                        {
                            dname.Append(encoding.GetString(buf, pi + 1, i - pi - 1));
                            dname.Append(Path.DirectorySeparatorChar);

                            pi = i;
                        }
                    }
                    dname.Append(encoding.GetString(buf, pi + 1, size - 3 - pi));
                    dirName = dname.ToString();
                    break;

                case LhaEntry.EXHDR_SIG_COMMENT:
                    break;

                case LhaEntry.EXHDR_SIG_DOSATTR:
                    break;

                case LhaEntry.EXHDR_SIG_DOSTIMES:
                    break;

                case LhaEntry.EXHDR_SIG_UNIXPERM:
                    break;

                case LhaEntry.EXHDR_SIG_UNIXID:
                    break;

                case LhaEntry.EXHDR_SIG_UNIXGROUPNAME:
                    break;

                case LhaEntry.EXHDR_SIG_UNIXUSERNAME:
                    break;

                case LhaEntry.EXHDR_SIG_UNIXLMTIME:
                    break;

                default:
                    break;
            }

            calcCRC.Update(buf, 0, buf.Length);

            return (Get16(buf, size - 2));
        }

        protected internal virtual string ConvertFilePath(string s, byte os)
        {
            char delim;

            switch (os)
            {
                case LhaEntry.OSID_SIG_GENERIC:
                case LhaEntry.OSID_SIG_MSDOS:
                case LhaEntry.OSID_SIG_WIN32:
                case LhaEntry.OSID_SIG_WINNT:
                    delim = HD_CHR_DELIM_MSDOS;
                    break;

                case LhaEntry.OSID_SIG_MAC:
                    delim = HD_CHR_DELIM_MAC;
                    break;

                default:
                    delim = HD_CHR_DELIM_UNIX;
                    break;
            }

            char[] c = s.ToCharArray();
            for (int i = 0; i < c.Length; ++i)
            {
                if (c[i] == delim)
                    c[i] = Path.DirectorySeparatorChar;
                else if (c[i] == Path.DirectorySeparatorChar)
                    c[i] = delim;
            }

            return (new string(c));
        }

        private static int Get16(byte[] b, int off)
        {
            return ((b[off] & 0xFF) | ((b[off + 1] & 0xFF) << 8));
        }

        private static long Get32(byte[] b, int off)
        {
            return (Get16(b, off) | ((long)Get16(b, off + 2) << 16));
        }
    }
}
