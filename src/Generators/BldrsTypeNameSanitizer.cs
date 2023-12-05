using Express;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsTypeNameSanitizer
    {
        public static readonly string[] UnsanitaryNames = new string[]
        {
            "class"
        };

        public static string SanitizedName( this TypeData data )
        {
            if ( UnsanitaryNames.Any( value => data.Name == value ) )
            {
                return data.Name + "_";
            }

            return data.Name;
        }
        public static string SanitizedName(this string typeName)
        {
            if (UnsanitaryNames.Any(value => typeName == value))
            {
                return typeName + "_";
            }

            return typeName;
        }

        public static string DesanitizedName(this string typeName)
        {
            if (UnsanitaryNames.Any(value => typeName == value + "_"))
            {
                return typeName.Substring( 0, typeName.Length - 1 );
            }

            return typeName;
        }
    }
}
