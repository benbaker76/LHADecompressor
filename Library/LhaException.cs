using System.IO;
using System.Runtime.Serialization;

namespace LHADecompressor
{
    public class LhaException : IOException
    {
        private const long serialVersionUID = -8799459685839682440L;

        public LhaException(string s)
            : base(s)
        {
        }

        public LhaException()
        {
        }

        protected LhaException(SerializationInfo s1, StreamingContext s2)
            : base(s1, s2)
        {
        }
    }
}
