
namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public class LhdDecoder : LhaDecoder
    {
        public LhdDecoder()
        {
        }

        public virtual int Read(byte[] b, int off, int len)
        {
            return -1;
        }

        public virtual void Close()
        {
        }
    }
}
