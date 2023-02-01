using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bldrs.Hashing
{
    public static class FNV1A
    {
        public static uint Hash( Span< byte > data, uint hash = 0x811C9DC5 )
        {
            foreach ( byte value in data )
            {
                hash ^= (uint)value;
                hash *= 0x01000193;
            }

            return hash;
        }
    }
}
