﻿using Express;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public enum BldrsStepKind
    {
        SELECT = 0,
        NUMBER = 1,
        STRING = 2,
        BOOLEAN = 3,
        STEP_REFERENCE = 4,
        ENUM = 5,
        BINARY_DATA = 6
    }

    public static class BldrsAttributeGenerator
    {
        public static string CleanName(AttributeData data)
        {
            string cleanName = data.Name;
            int lastOfSeparator = Math.Max(cleanName.LastIndexOf('.'), cleanName.LastIndexOf('/'));

            if (lastOfSeparator >= 0)
            {
                cleanName = cleanName.Substring(lastOfSeparator + 1);
            }

            return cleanName;
        }

        public static readonly string[] DeserializationFunctions =
        {
            "stepExtractBoolean",
            "stepExtractEnum",
            "stepExtractString",
            "stepExtractOptional",
            "stepExtractBinary",
            "stepExtractReference",
            "stepExtractNumber",
            "stepExtractInlineElemement",
            "stepExtractLogical",
            "stepExtractArrayBegin",
            "stepExtractArrayToken",
            "skipValue"
        };

        public static BldrsStepKind GetAttributeKind(string type, Dictionary<string, TypeData> typesData, bool parentIsSelect = false)
        {
            if (!typesData.ContainsKey(type.DesanitizedName()))
            {
                return type switch
                {
                    "boolean" => BldrsStepKind.BOOLEAN,
                    "number" => BldrsStepKind.NUMBER,
                    "string" => BldrsStepKind.STRING,
                    "[Uint8Array, number]" => BldrsStepKind.BINARY_DATA,
                    _ => throw new Exception("Unknown type requested for attribute kind")
                };
            }

            var typeData = typesData[type.DesanitizedName()];

            if (typeData is WrapperType wrapper)
            {
                if ( parentIsSelect )
                {
                    return BldrsStepKind.STEP_REFERENCE;
                }

                return GetAttributeKind(wrapper.WrappedType, typesData, false);
            }
            else if (typeData is Entity)
            {
                return BldrsStepKind.STEP_REFERENCE;
            }
            else if (typeData is SelectType)
            {
                return BldrsStepKind.SELECT;
            }
            else if (typeData is EnumData)
            {
                return BldrsStepKind.ENUM;
            }

            throw new Exception("Unknown type requested for attribute kind");
        }

        public static string AttributeDataString(AttributeData data, Dictionary<string, TypeData> typesData)
        {
            var type = data.InnerType;
            string cleanName = CleanName(data);

            if ( typesData.TryGetValue( type, out var typeData ) && typeData is WrapperType wrapper)
            {
                bool logical = wrapper.Name == "IfcLogical";

                while (typesData.TryGetValue(wrapper.WrappedType, out var innerTypeData) && innerTypeData is WrapperType innerWrapper)
                {
                    wrapper = innerWrapper;

                    logical |= wrapper.Name == "IfcLogical";
                }

                return $"private {cleanName}_? : {string.Join("", Enumerable.Repeat("Array< ", Math.Max( data.Rank, wrapper.Rank )))}{ wrapper.WrappedType }{string.Join("", Enumerable.Repeat(" >", Math.Max(data.Rank, wrapper.Rank)))}{(data.IsOptional | logical ? " | null" : "")}";
            }
            else
            {
                return $"private {cleanName}_? : {data.Type}{(data.IsOptional ? " | null" : "")}";
            }
        }

        public static string DeserializationInner(AttributeData data, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, string shortName, string type, bool isGeneric, string valueName = "value", string cursorName = "cursor", int indentCount = 0, bool logical = false)
        {
            var entityTypesName = $"EntityTypes{shortName}";
            var shortNameLC = shortName.ToLowerInvariant();
            string indent = new string(' ', indentCount * 2);

            // Item is used in functions.
            if (isGeneric)
            {
                return string.Empty;
            }

            if (!typesData.ContainsKey(type.DesanitizedName()))
            {
                return type switch
                {
                    "boolean" => logical ? @$"
    {indent}const {valueName} = stepExtractLogical( buffer, {cursorName}, endCursor )" : @$"
    {indent}const {valueName} = stepExtractBoolean( buffer, {cursorName}, endCursor )",
                    "number" => @$"
    {indent}const {valueName} = stepExtractNumber( buffer, {cursorName}, endCursor )
",
                    "string" => @$"
    {indent}const {valueName} = stepExtractString( buffer, {cursorName}, endCursor )",

                    "[Uint8Array, number]" => @$"
    {indent}const {valueName} = stepExtractBinary( buffer, {cursorName}, endCursor )",
                    _ => throw new Exception("Unknown type requested deserializer string")
                };
            }

            var typeData = typesData[type];

            if (typeData is WrapperType wrapper)
            {
                return DeserializationInner(data, typesData, selectTypes, shortName, wrapper.WrappedType, isGeneric, valueName, cursorName, indentCount, wrapper.Name == "IfcLogical");
            }
            else if (typeData is Entity)
            {
                return @$"
    {indent}const {valueName} = this.extractBufferElement( buffer, {cursorName}, endCursor, {type} )";
            }
            else if (typeData is SelectType select)
            {
                EnumData[] enums =
                    BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                    .Select(type => typesData.GetValueOrDefault(type) as EnumData)
                    .Where(type => type != null && type.Name != "null_style" && type.Name != "IfcNullStyle").ToArray();
                string instanceCheck =
                    string.Join(
                        " && ",
                        BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                        .Where(
                            type => type != "IfcNullStyle" &&
                                type != "null_style" &&
                                typesData.GetValueOrDefault(type) is not EnumData)
                        .Select(type => $"!( {valueName}Untyped instanceof {type} )"));
                string cast = $" as ({string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes))})";

                bool hasIfcNullStyle =
                    BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                    .Any(type => type == "IfcNullStyle");
                bool hasNormalNullStyle =
                    BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                    .Any(type => type == "null_style");

                string nullStyle = "";

                bool hasEnums = enums.Length > 0;
                StringBuilder enumStyle = new StringBuilder();

                string enumTypes = string.Join(" | ", enums.Select(enumType => enumType.SanitizedName()));

                if (hasEnums)
                {
                    enumStyle.Append($@"
      const {valueName}Enum : {enumTypes} | null =");
                    bool first = true;

                    foreach (EnumData enumData in enums)
                    {
                        enumStyle.Append($@"{(!first ? " ??" : "")}
    {indent}{enumData.SanitizedName()}DeserializeStep( buffer, cursor, endCursor )");
                    }
                }

                if (hasIfcNullStyle)
                {
                    instanceCheck += $" && {valueName}Untyped !== IfcNullStyle.NULL";
                    nullStyle = " ?? IfcNullStyleDeserializeStep( buffer, cursor, endCursor )";
                }

                if (hasNormalNullStyle)
                {
                    instanceCheck += $" && {valueName}Untyped !== null_style.NULL";
                    nullStyle = " ?? null_styleDeserializeStep( buffer, cursor, endCursor )";
                }

                return @$"
    {indent}const {valueName}Untyped : StepEntityBase< {entityTypesName} >{(hasIfcNullStyle ? " | IfcNullStyle" : "")}{(hasNormalNullStyle ? " | null_style" : "")}{(hasEnums ? " | " + enumTypes : "")} | undefined = {(hasEnums ? $"{valueName}Enum ?? " : "")}
      {indent}this.extractBufferReference( buffer, cursor, endCursor ){nullStyle}

    {indent}if ( {instanceCheck} ) {{
    {indent}  throw new Error( 'Value in select must be populated' )
    {indent}}}

    {indent}const {valueName} = {valueName}Untyped{cast}";
            }
            else if (typeData is EnumData collection)
            {
                return @$"
    {indent}const {valueName} = {typeData.Name}DeserializeStep( buffer, {cursorName}, endCursor )";
            }

            return "";
        }

        public static string Deserialization(AttributeData data, string assignTo, uint vtableOffsset, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, string shortName, bool isCollection, int rank, string type, bool isGeneric, int indent = 0, bool logical = false)
        {
            var entityTypesName = $"EntityTypes{shortName}";
            var shortNameLC = shortName.ToLowerInvariant();

            if (isCollection)
            {
                string nullCheck = data.IsOptional ?
$@"
      if ( stepExtractOptional( buffer, cursor, endCursor ) === null ) {{
        return null
      }}
" : 
$@"
      if ( stepExtractOptional( buffer, cursor, endCursor ) === null ) {{
        return []
      }}
";

                string innerType;

                if (selectTypes.ContainsKey(type))
                {
                    var unionType = string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(type, selectTypes));

                    innerType = unionType;
                }
                else
                {

                    if (typesData.TryGetValue(type, out var localTypeData) && localTypeData is WrapperType localWrapper)
                    {
                        while (typesData.TryGetValue(localWrapper.WrappedType, out var innerTypeData) && innerTypeData is WrapperType innerWrapper)
                        {
                            localWrapper = innerWrapper;
                        }

                        innerType = localWrapper.WrappedType;
                    }
                    else
                    {
                        innerType = type;
                    }
                }

                string valueType = $"{string.Join("", Enumerable.Repeat("Array<", rank))}{innerType}{string.Join("", Enumerable.Repeat(">", rank))}";

                var loopStructure = new StringBuilder();

                for (int where = 0; where < rank; ++where)
                {
                    string currentValueType = $"{string.Join("", Enumerable.Repeat("Array<", rank - where))}{innerType}{string.Join("", Enumerable.Repeat(">", rank - where))}";
                    string loopIndent = new string(' ', 2 * ( where + indent ));

                    loopStructure.Append(@$"
      {loopIndent}const value{(where == 0 ? "" : where.ToString())} : {currentValueType} = []

      {loopIndent}let signedCursor{where} = stepExtractArrayBegin( buffer, cursor, endCursor )
      {loopIndent}cursor = Math.abs( signedCursor{where} )

      {loopIndent}while ( signedCursor{where} >= 0 ) {{");
                }

                loopStructure.Append( DeserializationInner(data, typesData, selectTypes, shortName, type, isGeneric, $"value{rank}", "cursor", (rank + indent + 1), false) );

                string innerIndent = new string(' ', 2 * (rank + indent));

loopStructure.Append(@$"
      {innerIndent}if ( value{rank} === void 0 ) {{
      {innerIndent}  throw new Error( 'Value in STEP was incorrectly typed' )
      {innerIndent}}}
      {innerIndent}cursor = skipValue( buffer, cursor, endCursor )");

                for (int where = rank - 1; where >= 0; --where)
                {
                    string loopIndent = new string(' ', 2 * where);

                    loopStructure.Append(@$"
      {loopIndent}  value{(where == 0 ? "" : where.ToString())}.push( value{where + 1} )
      {loopIndent}  signedCursor{where} = stepExtractArrayToken( buffer, cursor, endCursor )
      {loopIndent}  cursor = Math.abs( signedCursor{where} )
      {loopIndent}}}");
                }


                return @$"
      let   cursor    = this.getOffsetCursor( {vtableOffsset} )
      const buffer    = this.buffer
      const endCursor = buffer.length
{nullCheck}{loopStructure.ToString()}

      {assignTo} = value";
            }

            // Item is used in functions.
            if (isGeneric)
            {
                return string.Empty;
            }

            if (!typesData.ContainsKey(type.DesanitizedName()))
            {
                string extractor = type switch
                {
                    "boolean" => logical ? $"this.extractLogical( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} )" :
                                            $"this.extractBoolean( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} )",
                    "number" => @$"this.extractNumber( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} )",
                    "string" => @$"this.extractString( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} )",
                    "[Uint8Array, number]" => @$"this.extractBinary( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} )",
                    _ => throw new Exception("Unknown type requested deserializer string")
                };

                return $"{assignTo} = {extractor}";
            }

            var typeData = typesData[type.DesanitizedName()];

            if (typeData is WrapperType wrapper)
            {
                return Deserialization(data, assignTo, vtableOffsset, typesData, selectTypes, shortName, wrapper.IsCollectionType, wrapper.Rank, wrapper.WrappedType, isGeneric, indent, wrapper.Name == "IfcLogical");
            }
            else if (typeData is Entity)
            {
                return @$"{assignTo} = this.extractElement( {vtableOffsset}, {(data.IsOptional ? "true" : "false")}, {type.SanitizedName()} )";
            }
            else if (typeData is SelectType select)
            {
                EnumData[] enums =
                    BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                    .Select(type => typesData.GetValueOrDefault(type) as EnumData)
                    .Where( type => type != null && type.Name != "null_style" && type.Name != "IfcNullStyle").ToArray();
                string instanceCheck = 
                    string.Join(
                        " && ", 
                        BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                        .Where( 
                            type => type != "IfcNullStyle" &&
                            type != "null_style" &&
                            typesData.GetValueOrDefault( type ) is not EnumData)
                        .Select(type => $"!( value instanceof {type.SanitizedName()} )"));
                string cast = $" as ({string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes))})";

                bool hasIfcNullStyle =
                    BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                    .Any( type => type == "IfcNullStyle" );
                bool hasNormalNullStyle =
                    BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes)
                    .Any(type => type == "null_style");

                string nullStyle = "";

                bool hasEnums = enums.Length > 0;
                StringBuilder enumStyle = new StringBuilder();

                string enumTypes = string.Join(" | ", enums.Select(enumType => enumType.SanitizedName()));

                if ( hasEnums )
                {
                    enumStyle.Append($@"
      const enumValue : {enumTypes} | null =");
                    bool first = true;

                    foreach ( EnumData enumData in enums )
                    {
                        enumStyle.Append($@"{(!first ? " ??" : "")}
        this.extractLambda( {vtableOffsset}, {enumData.SanitizedName()}DeserializeStep, true )");
                    }
                }

                if (hasIfcNullStyle)
                {
                    instanceCheck += " && value !== IfcNullStyle.NULL";
                    nullStyle = " ?? IfcNullStyleDeserializeStep( buffer, cursor, endCursor )";
                }

                if (hasNormalNullStyle)
                {
                    instanceCheck += " && value !== null_style.NULL";
                    nullStyle = " ?? null_styleDeserializeStep( buffer, cursor, endCursor )";
                }

                if ( data.IsOptional )
                {
                    instanceCheck += " && value !== null";
                }

                return @$"{enumStyle}
      const value : StepEntityBase< {entityTypesName} >{(hasIfcNullStyle ? " | IfcNullStyle" : "")}{(hasNormalNullStyle ? " | null_style" : "")}{(data.IsOptional ? "| null" : "")}{(hasEnums ? " | " + enumTypes : "")} = {(hasEnums ? "enumValue ?? " : "")}
        this.extractReference( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} ){nullStyle}

      if ( {(hasEnums ? "enumValue === null && " : "")}{instanceCheck} ) {{
        throw new Error( 'Value in STEP was incorrectly typed for field' )
      }}

      {assignTo} = value{cast}
";
            }
            else if (typeData is EnumData collection)
            {
                return $"{assignTo} = this.extractLambda( {vtableOffsset}, {typeData.SanitizedName()}DeserializeStep, {(data.IsOptional ? "true" : "false")} )";
            
            }

            return "";
        }

        public static string AttributePropertyString(AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, int rank, string type, bool isGeneric, HashSet< string > importFunctions, string shortName )
        {
            if ( (!data.IsDerived && data.HidesParentAttributeOfSameName) || data.IsInverse)
            {
                return "";
            }

            string propertyTypeString;

            if (typesData.TryGetValue(type, out var typeData) && typeData is WrapperType wrapper)
            {
                bool logical = wrapper.Name == "IfcLogical";

                while (typesData.TryGetValue(wrapper.WrappedType, out var innerTypeData) && innerTypeData is WrapperType innerWrapper)
                {
                    wrapper = innerWrapper;

                    logical |= wrapper.Name == "IfcLogical";
                }

                propertyTypeString = $"{string.Join("", Enumerable.Repeat("Array< ", Math.Max(rank, wrapper.Rank)))}{ wrapper.WrappedType }{string.Join("", Enumerable.Repeat(" >", Math.Max(rank, wrapper.Rank)))}{ (data.IsOptional | logical ? " | null" : string.Empty)}";
            }
            else
            {
                propertyTypeString = $"{ data.Type }{ (data.IsOptional ? " | null" : string.Empty)}";
            }

            string cleanName = CleanName( data );

            if (data.IsDerived)
            {
                string transformedExpression = BldrsDerivedFunctionTranslator.TransformDerivedFunctionToTS(data.DerivedExpression, importFunctions);

                if (  string.IsNullOrEmpty( transformedExpression ) )
                {
                    return "";
                }

                return $@"
  public get {cleanName}() : {propertyTypeString} {{
    return {transformedExpression}
  }}";
            }

            string deserialization = Deserialization(data, $"this.{cleanName}_", vtableOffsset, typesData, selectTypes, shortName, data.IsCollection, rank, type, isGeneric);

            foreach (string deserializationFunction in DeserializationFunctions)
            {
                if (deserialization.Contains(deserializationFunction))
                {
                    importFunctions.Add(deserializationFunction);
                }
            }

            return $@"
  public get {cleanName}() : {propertyTypeString} {{
    if ( this.{cleanName}_ === void 0 ) {{
      {deserialization}
    }}

    return this.{cleanName}_ as {propertyTypeString}
  }}";
        }
    }
}
