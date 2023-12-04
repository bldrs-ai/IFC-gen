using Express;
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
            if (!typesData.ContainsKey(type))
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

            var typeData = typesData[type];

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
            
            if ( typesData.TryGetValue( type, out var typeData ) && typeData is WrapperType wrapper)
            {
                bool logical = wrapper.Name == "IfcLogical";

                while (typesData.TryGetValue(wrapper.WrappedType, out var innerTypeData) && innerTypeData is WrapperType innerWrapper)
                {
                    wrapper = innerWrapper;

                    logical |= wrapper.Name == "IfcLogical";
                }

                return $"private {data.Name}_? : {string.Join("", Enumerable.Repeat("Array< ", Math.Max( data.Rank, wrapper.Rank )))}{ wrapper.WrappedType }{string.Join("", Enumerable.Repeat(" >", Math.Max(data.Rank, wrapper.Rank)))}{(data.IsOptional | logical ? " | null" : "")}";
            }
            else
            {
                return $"private {data.Name}_? : {data.Type}{(data.IsOptional ? " | null" : "")}";
            }
        }

        public static string DeserializationInner(AttributeData data, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, string type, bool isGeneric, string valueName = "value", string cursorName = "cursor", int indentCount = 0, bool logical = false)
        {
            string indent = new string(' ', indentCount * 2);

            // Item is used in functions.
            if (isGeneric)
            {
                return string.Empty;
            }

            if (!typesData.ContainsKey(type))
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
                return DeserializationInner(data, typesData, selectTypes, wrapper.WrappedType, isGeneric, valueName, cursorName, indentCount, wrapper.Name == "IfcLogical");
            }
            else if (typeData is Entity)
            {
                return @$"
    {indent}const {valueName} = this.extractBufferElement( buffer, {cursorName}, endCursor, {type} )";
            }
            else if (typeData is SelectType select)
            {

                string instanceCheck = string.Join(" && ", BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes).Where(type => type != "IfcNullStyle").Select(type => $"!( {valueName}Untyped instanceof {type} )"));
                string cast = $" as ({string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes))})";

                bool hasNullStyle = BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes).Contains("IfcNullStyle");

                string nullStyle = "";

                if (hasNullStyle)
                {
                    instanceCheck += $" && ({valueName}Untyped !== IfcNullStyle.NULL)";
                    nullStyle = " ?? IfcNullStyleDeserializeStep( buffer, cursor, endCursor )";
                }

                return @$"
    {indent}const {valueName}Untyped : StepEntityBase< EntityTypesIfc >{(hasNullStyle ? " | IfcNullStyle" : "")} | undefined =
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

        public static string Deserialization(AttributeData data, string assignTo, uint vtableOffsset, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, bool isCollection, int rank, string type, bool isGeneric, int indent = 0, bool logical = false)
        {
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

                loopStructure.Append( DeserializationInner(data, typesData, selectTypes, type, isGeneric, $"value{rank}", "cursor", (rank + indent + 1), false) );

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

            if (!typesData.ContainsKey(type))
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

            var typeData = typesData[type];

            if (typeData is WrapperType wrapper)
            {
                return Deserialization(data, assignTo, vtableOffsset, typesData, selectTypes, wrapper.IsCollectionType, wrapper.Rank, wrapper.WrappedType, isGeneric, indent, wrapper.Name == "IfcLogical");
            }
            else if (typeData is Entity)
            {
                return @$"{assignTo} = this.extractElement( {vtableOffsset}, {(data.IsOptional ? "true" : "false")}, {type} )";
            }
            else if (typeData is SelectType select)
            {         
                string instanceCheck = string.Join(" && ", BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes).Where( type => type != "IfcNullStyle" ).Select(type => $"!( value instanceof {type} )"));
                string cast = $" as ({string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes))})";

                bool hasNullStyle = BldrsSelectGenerator.ExpandPossibleTypes(select.Name, selectTypes).Contains("IfcNullStyle");

                string nullStyle = "";

                if ( hasNullStyle )
                {
                    instanceCheck += " && value !== IfcNullStyle.NULL";
                    nullStyle = " ?? IfcNullStyleDeserializeStep( buffer, cursor, endCursor )";
                }
        
                if (data.IsOptional)
                {
                    instanceCheck += " && value !== null";
                }


                return @$"
      const value : StepEntityBase< EntityTypesIfc >{(hasNullStyle ? " | IfcNullStyle" : "")}{(data.IsOptional ? "| null" : "")} =
        this.extractReference( {vtableOffsset}, {(data.IsOptional ? "true" : "false")} ){nullStyle}

      if ( {instanceCheck} ) {{
        throw new Error( 'Value in STEP was incorrectly typed for field' )
      }}

      {assignTo} = value{cast}
";
            }
            else if (typeData is EnumData collection)
            {
                return $"{assignTo} = this.extractLambda( {vtableOffsset}, {typeData.Name}DeserializeStep, {(data.IsOptional ? "true" : "false")} )";
            
            }

            return "";
        }

        public static string AttributePropertyString(AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, int rank, string type, bool isGeneric, HashSet< string > importFunctions )
        {
            if (/*(data.IsDerived && !data.HidesParentAttributeOfSameName) ||*/ data.IsInverse)
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

            if (data.IsDerived)
            {
                string transformedExpression = BldrsDerivedFunctionTranslator.TransformDerivedFunctionToTS(data.DerivedExpression, importFunctions);

                if (  string.IsNullOrEmpty( transformedExpression ) )
                {
                    return "";
                }

                return $@"
  public get {data.Name}() : {propertyTypeString} {{
    return {transformedExpression}
  }}";
            }

            string deserialization = Deserialization(data, $"this.{data.Name}_", vtableOffsset, typesData, selectTypes, data.IsCollection, rank, type, isGeneric);

            foreach (string deserializationFunction in DeserializationFunctions)
            {
                if (deserialization.Contains(deserializationFunction))
                {
                    importFunctions.Add(deserializationFunction);
                }
            }

            return $@"
  public get {data.Name}() : {propertyTypeString} {{
    if ( this.{data.Name}_ === void 0 ) {{
      {deserialization}
    }}

    return this.{data.Name}_ as {propertyTypeString}
  }}";
        }
    }
}
