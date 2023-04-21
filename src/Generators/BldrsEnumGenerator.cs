using Bldrs.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Express;

namespace IFC4.Generators
{
    public static class BldrsEnumGenerator
    {
        public static string GenerateEnumString( EnumType data )
        {
            var typeIDGenerator = new BlrdrsTypeIDGenerator( data.Values.Select( name => $".{name}."), data.Values.Select( _ => true ) );

            var builder = new StringBuilder();

            typeIDGenerator.GenerateEnum(builder, data.Name, 0, false);
            builder.AppendLine();
            typeIDGenerator.GenerateHashData(builder, data.Name, null, 0, false);
            builder.AppendLine();
            builder.Append($@"
/* This is generated cold, don't alter */
import StepEnumParser from '../../../dependencies/conway-ds/src/parsing/step/step_enum_parser'

const parser = StepEnumParser.Instance

export function {data.Name}DeserializeStep(
  input: Uint8Array,
  cursor: number,
  endCursor: number ): {data.Name} | undefined {{
  return parser.extract< {data.Name} >( {data.Name}Search, input, cursor, endCursor )
}}
");

            return builder.ToString();
        }
    }
}
