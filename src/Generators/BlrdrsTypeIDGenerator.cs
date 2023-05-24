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

        public void GenerateAttributeDescription(StringBuilder output, string type, Dictionary<string, TypeData> typesData, Dictionary< string, SelectType> selectTypes, string indent, bool isOptional, bool isDerived, bool isCollection, int rank, bool parentIsSelect, uint vtableOffset )
        {
            var typeKind = BldrsAttributeGenerator.GetAttributeKind(type, typesData, parentIsSelect);

            output.AppendLine($"{{");
            output.AppendLine($"{indent}  kind: f.{typeKind},");

            if ( isCollection )
            {
                output.AppendLine($"{indent}  rank: {rank},");
            }

            output.AppendLine($"{indent}  optional: {(isOptional || (type == "IfcLogical" && !parentIsSelect) ? "true" : "false")},");
            output.AppendLine($"{indent}  derived: {(isDerived ? "true" : "false")},");

            if ( !isDerived && !parentIsSelect )
            {
                output.AppendLine($"{indent}  offset: {vtableOffset},");
            }

            switch ( typeKind )
            {
            case BldrsStepKind.SELECT:
                {
                    output.AppendLine($"{indent}  options: [");

                    string indentOptions = indent + "    ";

                    foreach ( var option in BldrsSelectGenerator.ExpandPossibleTypes(type, selectTypes) )
                    {
                        output.Append($"{indent}    ");

                        GenerateAttributeDescription( output, option, typesData, selectTypes, indentOptions, false, false, false, 0, true, vtableOffset );
                    }

                    output.AppendLine($"{indent}  ],");
                }

                break;

            case BldrsStepKind.ENUM:
                {
                    output.AppendLine($"{indent}  type: {type},");
                }
                break;

            case BldrsStepKind.STEP_REFERENCE:
                {
                    output.AppendLine($"{indent}  type: e.{type.ToUpperInvariant()},");
                }
                break;
            }

            output.AppendLine($"{indent}}},");
        }

        public void GenerateEntityDescription( StringBuilder output, Entity entity, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, string indent )
        {
            output.AppendLine($"{indent}{{");
            output.AppendLine($"{indent}  fields: {{");

            string attributeIndent = indent + "    ";

            uint baseFieldOffset = 0;

            if (entity.Subs.Count > 0)
            {
                baseFieldOffset = BldrsEntityGenerator.FieldCountWithParents(entity.Subs[0]);
            }

            foreach ( var attribute in entity.Attributes )
            {
                output.Append($"{attributeIndent}{attribute.Name}: ");

                GenerateAttributeDescription(output, attribute.type, typesData, selectTypes, attributeIndent, attribute.IsOptional, attribute.IsDerived, attribute.IsCollection, attribute.Rank, false, ++baseFieldOffset);
            }

            output.AppendLine($"{indent}  }},");
            output.AppendLine($"{indent}  typeId: e.{entity.Name.ToUpperInvariant()},");
            output.AppendLine($"{indent}  isAbstract: {(entity.IsAbstract ? "true" : "false")},");

            // The terminology for IFC-gen is actually backwards re super and sub-types.
            if ( entity.Subs.Count > 0 )
            {
                output.AppendLine($"{indent}  superType: e.{entity.Subs[0].Name.ToUpperInvariant()},");
            }
            
            if (entity.Supers.Count > 0)
            {
                output.AppendLine($"{indent}  subTypes: [");

                foreach (var super in entity.Supers)
                {
                    output.AppendLine($"{indent}     e.{super.Name.ToUpperInvariant()},");
                }

                output.AppendLine($"{indent}  ],");
            }


            output.AppendLine($"{indent}}},");
        }

        public void GenerateWrappedDescription(StringBuilder output, WrapperType wrapperType, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, string indent)
        {
            output.AppendLine($"{indent}{{");
            output.AppendLine($"{indent}  fields: {{");

            string attributeIndent = indent + "    ";

            output.Append($"{attributeIndent}Value: ");

            GenerateAttributeDescription(output, wrapperType.WrappedType, typesData, selectTypes, attributeIndent, false, false, wrapperType.IsCollectionType, wrapperType.Rank, false, 0);

            output.AppendLine($"{indent}  }},");
            output.AppendLine($"{indent}  typeId: e.{wrapperType.Name.ToUpperInvariant()},");
            output.AppendLine($"{indent}  isAbstract: false,");
            output.AppendLine($"{indent}}},");
        }

        public void GenerateSchema(StringBuilder output, string name, int indent, string entityTypesName, string entityTypesFile, string entitySearchTypesName, string entitySearchTypesFile, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes)
        {
            string indent0 = new string(' ', indent * 2);
            string indent1 = new string(' ', (indent + 1) * 2);

            output.AppendLine($"/* This is generated code, don't alter */");
            output.AppendLine(@"import {
  FieldDescriptionKind,
} from '../../core/entity_field_description'" );
            output.AppendLine("import { EntityDescription } from '../../core/entity_description'");
            output.AppendLine($"{indent0}import {entityTypesName} from './{entityTypesFile}'");
            output.AppendLine($"{indent0}import {entitySearchTypesName} from './{entitySearchTypesFile}'");
            output.AppendLine($"{indent0}import StepEntityConstructor from '../../step/step_entity_constructor'");
            output.AppendLine($"{indent0}import StepEntityBase from '../../step/step_entity_base'");
            output.AppendLine($"{indent0}import StepEntitySchema from '../../step/step_entity_schema'");
            output.AppendLine($"{indent0}import StepParser from '../../step/parsing/step_parser'");

            for (int where = 0; where < Names.Length; ++where)
            {
                string localName = Names[where];

                output.AppendLine($"{indent0}import {{ {localName} }} from './index'");
            }

            foreach ( var enumType in typesData.Values.Where( type => type is EnumData && !(type is SelectType)).Select( type => type as EnumData).OrderBy( type => type.Name ) )
            {
                output.AppendLine($"{indent0}import {{ {enumType.Name} }} from './index'");
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

            output.AppendLine("const f = FieldDescriptionKind");
            output.AppendLine($"const e = {entityTypesName}");

            output.AppendLine($"{indent0}let queries : {entityTypesName}[][] = [");

            for (int where = 0; where < Names.Length; ++where)
            {
                string localName = Names[where];

                output.AppendLine($"{indent1}{localName}.query,");
            }

            output.AppendLine($"{indent0}]");
            output.AppendLine($"{indent0}let descriptions : EntityDescription< {entityTypesName} >[] = [");


            for (int where = 0; where < Names.Length; ++where)
            {
                string localName = Names[where];

                var typeData = typesData[localName];

                if (typeData is Entity entity)
                {
                    GenerateEntityDescription(output, entity, typesData, selectTypes, indent1);
                }
                else if (typeData is WrapperType wrapper)
                {
                    GenerateWrappedDescription(output, wrapper, typesData, selectTypes, indent1);
                }
            }

            output.AppendLine($"{indent0}]");

            output.AppendLine($"let parser =\n  new StepParser< {entityTypesName} >( {entitySearchTypesName} )");
            output.AppendLine();
            output.AppendLine($"let {name} =\n  new StepEntitySchema< {entityTypesName} >( constructors, parser, queries, descriptions )");
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
            output.AppendLine("import MinimalPerfectHash from '../../indexing/minimal_perfect_hash'");

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