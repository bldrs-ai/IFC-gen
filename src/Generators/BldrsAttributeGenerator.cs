using Express;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsAttributeGenerator
    {
        public static string AttributeDataString(AttributeData data)
        {
            if (data.IsDerived || data.IsInverse)
            {
                return "";
            }

            return $"private {data.Name}_? : {data.Type}";
        }

        public static string AttributePropertyString(AttributeData data, uint serializationOffset)
        {
            if (data.IsDerived || data.IsInverse)
            {
                return "";
            }

            return $@"
    public get {data.Name}() : {data.Type} {(data.IsOptional ? " | undefined" : string.Empty)}
    {{
        if ( this.{data.Name}_ === undefined )
        {{
            if ( this.buffer_ !== undefined )
            {{

            }}
        }}

    }}";
        }
    }
}
