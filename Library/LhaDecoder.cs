namespace LHADecompressor
{
    /**
     * 
     * @author Nobuyasu SUEHIRO <nosue@users.sourceforge.net>
     */
    public interface LhaDecoder
    {
        int Read(byte[] b, int off, int len);

        void Close();
    }
}
