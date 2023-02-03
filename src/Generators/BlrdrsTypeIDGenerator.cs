using Bldrs.Hashing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public class BlrdrsTypeIDGenerator
    {
        public readonly MinimalPerfectHash IDTable;
        public readonly string[] Names;
        public readonly byte[][] EncodedNames;
        public readonly int[] PrefixSumEncoded;

        public BlrdrsTypeIDGenerator(IEnumerable<string> names)
        {
            Names = names.ToArray();

            var ids = names.Select(name => Encoding.UTF8.GetBytes(name.ToUpperInvariant())).ToArray();

            EncodedNames = ids;

            IDTable = MinimalPerfectHash.Create(ids);

            PrefixSumEncoded = new int[EncodedNames.Length + 1];


            PrefixSumEncoded[0] = 0;

            for (int where = 0; where < IDTable.Keys.Count; where++)
            {
                PrefixSumEncoded[where + 1] = PrefixSumEncoded[where] + IDTable.Keys[where].Length;
            }
        }

        public void GenerateEnum(StringBuilder output, string name, int indent)
        {
            string indent0 = new string(' ', indent * 4);
            string indent1 = new string(' ', (indent + 1) * 4);

            output.AppendLine($"{indent0}enum {name} {{");

            for (int where = 0; where < Names.Length; ++where)
            {
                var encodedName = new Span<byte>(EncodedNames[where]);

                uint slotValue;

                if (!IDTable.TryGetSlot(encodedName, out slotValue))
                {
                    throw new Exception($"Slot not found for matching name in perfect hash {Names[where]}");
                }

                output.AppendLine($"{indent1}{Names[where]} = {where},");
                output.AppendLine($"{indent1}{Names[where].ToUpper()} = {where},");
            }

            output.AppendLine($"{indent0}}}");

            output.AppendLine($"export default {name};");
        }

#nullable enable
        public void GenerateHashData(StringBuilder output, string name, string? enumFile, int indent)
        {
            string indent0 = new string(' ', indent * 4);
            string indent1 = new string(' ', (indent + 1) * 4);

            output.AppendLine("import MinimalPerfectHash from '../../../dependencies/conway-ds/src/indexing/minimal_perfect_hash';");

            if (!String.IsNullOrEmpty(enumFile))
            {
                output.AppendLine($"import {name} from './{enumFile}';");
            }
            
            output.AppendLine();

            output.Append($"{indent0}let gMap{name} = new Int32Array( [");

            for (int where = 0; where < IDTable.GMap.Count; ++where)
            {
                if (where != 0)
                {
                    output.Append(",");
                }

                output.Append($"{IDTable.GMap[where]}");
            }

            output.AppendLine($"{indent0}] );");
            output.AppendLine();

            output.Append($"let prefixSumAddress{name} = new Uint32Array( [");

            // Prefix sum in hash table order allows us to go straight from hash to string lookup
            for (int where = 0; where < PrefixSumEncoded.Length; ++where)
            {
                if (where != 0)
                {
                    output.Append(",");
                }

                output.Append($"{PrefixSumEncoded[where]}");
            }

            output.AppendLine($"] );");
            output.AppendLine();

            output.Append($"{indent0}let slotMap{name} = new Int32Array( [");

            // Slotmap points back to the original IDs
            for (int where = 0; where < IDTable.SlotMap.Count; ++where)
            {
                if (where != 0)
                {
                    output.Append(",");
                }

                output.Append($"{IDTable.SlotMap[where]}");
            }

            output.AppendLine($"] );");
            output.AppendLine();
            output.Append($"{indent0}let encodedData{name} = (new TextEncoder()).encode( \"");

            foreach ( var encodedKey in IDTable.Keys)
            {
                output.Append(Encoding.UTF8.GetString(encodedKey));
            }

            output.AppendLine("\" );");
            output.AppendLine();

            output.AppendLine($"{indent0}let {name}Search = new MinimalPerfectHash< {name} >( gMap{name}, prefixSumAddress{name}, slotMap{name}, encodedData{name} );");
            output.AppendLine();
            output.AppendLine($"{indent0}export default {name}Search;");

        }
    }
#nullable disable
}