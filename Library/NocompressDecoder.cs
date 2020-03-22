using System.IO;

namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class NocompressDecoder : LhaDecoder
    {
        private BinaryReader @in;
        private long originalSize;

        public NocompressDecoder(BinaryReader @in, long originalSize)
        {
            this.@in = @in;
            this.originalSize = originalSize;
        }

        public virtual int Read(byte[] b, int off, int len)
        {
            if (len <= 0)
            {
                return (0);
            }

            if (originalSize <= 0)
            {
                return (-1);
            }

            int sl = len;

            while ((originalSize > 0) && (len > 0))
            {
                b[off++] = @in.ReadByte();

                --originalSize;
                --len;
            }

            return (sl - len);
        }

        public virtual void Close()
        {
            this.@in = null;
        }
    }
}
