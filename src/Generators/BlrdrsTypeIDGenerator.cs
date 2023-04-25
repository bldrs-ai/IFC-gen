using Bldrs.Hashing;
using Express;
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
        public readonly bool[] IsAbstract;
        public readonly byte[][] EncodedNames;
        public readonly int[] PrefixSumEncoded;

        public BlrdrsTypeIDGenerator(IEnumerable<string> names, IEnumerable<bool> isAbstract)
        {
            Names = names.ToArray();
            IsAbstract = isAbstract.ToArray();

            var ids = Names.Select(name => Encoding.UTF8.GetBytes(name.ToUpperInvariant())).ToArray();

            EncodedNames = ids;

            IDTable = MinimalPerfectHash.Create(ids);

            PrefixSumEncoded = new int[EncodedNames.Length + 1];


            PrefixSumEncoded[0] = 0;

            for (int where = 0; where < IDTable.Keys.Count; where++)
            {
                PrefixSumEncoded[where + 1] = PrefixSumEncoded[where] + IDTable.Keys[where].Length;
            }
        }

        public void GenerateInternal(StringBuilder output, Dictionary<string, TypeData> typesData )
        {
            foreach ( string name in typesData.Where(nameType => { return nameType.Value is EnumData && !(nameType.Value is SelectType); }).OrderBy(nameType => nameType.Key).Select( nameType => nameType.Key ) )
            { 
                output.AppendLine($"export {{ {name}, {name}DeserializeStep }} from './{name}.gen'");
            }

            foreach ( string name in Names.OrderBy( name =>
                {
                    var typeData = typesData[name];

                    if (typeData is Entity entity)
                    {
                        return entity.Parents().Count();
                    }

                    return 0;

                }))
            {
                output.AppendLine($"export {{ {name} }} from './{name}.gen'");
            }
        }

        public void GenerateSchema(StringBuilder output, string name, int indent, string entityTypesName, string entityTypesFile, string entitySearchTypesName, string entitySearchTypesFile )
        {
            string indent0 = new string(' ', indent * 2);
            string indent1 = new string(' ', (indent + 1) * 2);

            output.AppendLine($"/* This is generated code, don't alter */");
            output.AppendLine($"{indent0}import {entityTypesName} from './{entityTypesFile}'");
            output.AppendLine($"{indent0}import {entitySearchTypesName} from './{entitySearchTypesFile}'");
            output.AppendLine($"{indent0}import StepEntityConstructor from '../../core/step_entity_constructor'");
            output.AppendLine($"{indent0}import StepEntityBase from '../../core/step_entity_base'");
            output.AppendLine($"{indent0}import StepEntitySchema from '../../core/step_entity_schema'");
            output.AppendLine($"{indent0}import StepParser from '../../../dependencies/conway-ds/src/parsing/step/step_parser'");

            for (int where = 0; where < Names.Length; ++where)
            {
            //    if (!IsAbstract[where])
                {
                    string localName = Names[where];

                    output.AppendLine($"{indent0}import {{ {localName} }} from './index'");
                }
            }

            output.AppendLine($"{indent0}let constructors : ( StepEntityConstructor< {entityTypesName}, StepEntityBase< {entityTypesName} > > | undefined )[]  = [");

            for (int where = 0; where < Names.Length; ++where)  
            {
                if (!IsAbstract[where])
                {
                    string localName = Names[where];

                    output.AppendLine($"{indent1}{localName},");
                }
                else
                {
                    output.AppendLine($"{indent1}void 0,");
                }
            }

            output.AppendLine($"{indent0}]");

            output.AppendLine($"{indent0}let queries : {entityTypesName}[][] = [");

            for (int where = 0; where < Names.Length; ++where)
            {
                string localName = Names[where];

                output.AppendLine($"{indent1}{localName}.query,");
            }

            output.AppendLine($"{indent0}]");

            output.AppendLine();
            output.AppendLine($"let parser =\n  new StepParser< {entityTypesName} >( {entitySearchTypesName} )");
            output.AppendLine();
            output.AppendLine($"let {name} =\n  new StepEntitySchema< {entityTypesName} >( constructors, parser, queries )");
            output.AppendLine();
            output.AppendLine($"export default {name}");
        }

        public void GenerateEnum(StringBuilder output, string name, int indent, bool isDefault )
        {
            string indent0 = new string(' ', indent * 2);
            string indent1 = new string(' ', (indent + 1) * 2);

            output.AppendLine($"/* This is generated code, don't alter */");
            output.AppendLine($"{indent0}enum {name} {{");

            for (int where = 0; where < Names.Length; ++where)
            {
                var encodedName = new Span<byte>(EncodedNames[where]);

                uint slotValue;

                if (!IDTable.TryGetSlot(encodedName, out slotValue))
                {
                    throw new Exception($"Slot not found for matching name in perfect hash {Names[where]}");
                }

                string elementName = Names[where];
                string strippedName = elementName.Replace( ".", "" );

                output.AppendLine($"{indent1}{strippedName.ToUpper()} = {where},");
            }

            output.AppendLine($"{indent0}}}");
            output.AppendLine();

            output.AppendLine($"const {name}Count = {Names.Length}");
            output.AppendLine();

            if ( isDefault )
            {
                output.AppendLine($"export default {name}");

                output.AppendLine($"export {{ {name}Count }}");
            }
            else
            {
                output.AppendLine($"export {{ {name}, {name}Count }}");
            }
        }

#nullable enable
        public void GenerateHashData(StringBuilder output, string name, string? enumFile, int indent, bool exportDefault = true)
        {
            string indent0 = new string(' ', indent * 4);
            string indent1 = new string(' ', (indent + 1) * 4);

            output.AppendLine("/* This is generated code, don't alter */");
            output.AppendLine("import MinimalPerfectHash from '../../../dependencies/conway-ds/src/indexing/minimal_perfect_hash'");

            if (!String.IsNullOrEmpty(enumFile))
            {
                output.AppendLine($"import {name} from './{enumFile}'");
            }
            
            output.AppendLine();

            output.Append($"{indent0}let gMap{name} =\n  new Int32Array( [");

            for (int where = 0; where < IDTable.GMap.Count; ++where)
            {
                if (where != 0)
                {
                    output.Append(",");
                }

                output.Append($"{IDTable.GMap[where]}");
            }

            output.AppendLine($"{indent0}] )");
            output.AppendLine();

            output.Append($"let prefixSumAddress{name} =\n  new Uint32Array( [");

            // Prefix sum in hash table order allows us to go straight from hash to string lookup
            for (int where = 0; where < PrefixSumEncoded.Length; ++where)
            {
                if (where != 0)
                {
                    output.Append(",");
                }

                output.Append($"{PrefixSumEncoded[where]}");
            }

            output.AppendLine($"] )");
            output.AppendLine();

            output.Append($"{indent0}let slotMap{name} =\n  new Int32Array( [");

            // Slotmap points back to the original IDs
            for (int where = 0; where < IDTable.SlotMap.Count; ++where)
            {
                if (where != 0)
                {
                    output.Append(",");
                }

                output.Append($"{IDTable.SlotMap[where]}");
            }

            output.AppendLine($"] )");
            output.AppendLine();
            output.Append($"{indent0}let encodedData{name} =\n  (new TextEncoder()).encode( \"");

            foreach ( var encodedKey in IDTable.Keys)
            {
                output.Append(Encoding.UTF8.GetString(encodedKey));
            }

            output.AppendLine("\" )");
            output.AppendLine();

            output.AppendLine($"{indent0}let {name}Search =\n  new MinimalPerfectHash< {name} >( gMap{name}, prefixSumAddress{name}, slotMap{name}, encodedData{name} )");
            output.AppendLine();

            if (exportDefault)
            {
                output.AppendLine($"{indent0}export default {name}Search");
            }
            else
            {
                output.AppendLine($"{indent0}export {{ {name}Search }}");
            }

        }
    }
#nullable disable
}