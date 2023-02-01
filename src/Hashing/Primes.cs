using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bldrs.Hashing
{
    public static class Primes
    {
        public static bool IsPrime( uint value )
        {
            if ( value < 2 )
            {
                return false;
            }
            else if ( value == 2 )
            {
                return true;
            } 
            else if ( ( value & 1 ) == 0 )
            {
                return false;
            }
            
            for ( uint where = 3, end = (value / 2) + 2; where < end; where += 2 )
            {
                if ( ( value % where  ) == 0 )
                {
                    return true;
                }
            }

            return false;
        }

        public static uint Next( uint value )
        {
            for (; !IsPrime( value ); ++value ) {}

            return value;
        }
    }
}
