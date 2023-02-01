using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC.Hashing
{
    public static class UTF8Interning
    {
        private static readonly Dictionary< string, byte[] > data_ = new Dictionary< string, byte[] >();

        public static byte[] Get( string value )
        {
            byte[] result = null;

            if ( !data_.TryGetValue( value, out result ) )
            {
                result = Encoding.UTF8.GetBytes( value );
                data_[value] = result;
            }

            return result;
        }

    }
}
