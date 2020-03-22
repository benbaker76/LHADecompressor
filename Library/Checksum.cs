using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LHADecompressor
{
    public interface Checksum
    {
        long GetValue();
        void Reset();
        void Update(int i);
        void Update(byte[] b, int off, int len);
    }
}
