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
            "stepExtractArray",
            "stepExtractLogical"
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

        public static string Deserialization( AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, bool isCollection, int rank, string type, bool isGeneric, bool useVtable = true, int indent = 0, bool usePrevCursor = false, bool logical = false )
        {
            var commonPrefix = useVtable ? @$"this.guaranteeVTable()

      let internalReference = this.internalReference_ as Required< StepEntityInternalReference< EntityTypesIfc > >

      if ( {vtableOffsset} >= internalReference.vtableCount ) {{
        throw new Error( ""Couldn't read field due to too few fields in record"" )
      }}
            
      let vtableSlot = internalReference.vtableIndex + {vtableOffsset}

      let cursor    = internalReference.vtable[ vtableSlot ]
      let buffer    = internalReference.buffer
      let endCursor = buffer.length
" : (usePrevCursor ? "" : $@"let cursor = address
");
            var commonPostfix = data.IsOptional && useVtable ? 
$@"      if ( value === void 0 ) {{
        if ( stepExtractOptional( buffer, cursor, endCursor ) !== null ) {{
          throw new Error( 'Value in STEP was incorrectly typed' )
        }}

        return null
      }} else {{
        return value
      }}": (!usePrevCursor ?
$@"      if ( value === void 0 )  {{
        throw new Error( 'Value in STEP was incorrectly typed' )
      }}

      return value" : "return value" );

            if (isCollection)
            {
                string arrayPostfix = "return value";
                string nullPrefix = data.IsOptional && useVtable ?
$@"
      if ( stepExtractOptional( buffer, cursor, endCursor ) === null ) {{
        return null
      }}
" : "";

                string valueType;

                if (selectTypes.ContainsKey(type))
                {
                    var unionType = string.Join('|', BldrsSelectGenerator.ExpandPossibleTypes(type, selectTypes));

                    valueType = $"{string.Join("", Enumerable.Repeat("Array<", rank))}{unionType}{string.Join("", Enumerable.Repeat(">", rank))}";
                }
                else
                {

                    if (typesData.TryGetValue(type, out var localTypeData) && localTypeData is WrapperType localWrapper)
                    {
                        while (typesData.TryGetValue(localWrapper.WrappedType, out var innerTypeData) && innerTypeData is WrapperType innerWrapper)
                        {
                            localWrapper = innerWrapper;
                        }

                        valueType = $"{string.Join("", Enumerable.Repeat("Array<", rank))}{localWrapper.WrappedType}{string.Join("", Enumerable.Repeat(">", rank))}";
                    }
                    else
                    {
                        valueType = $"{string.Join("", Enumerable.Repeat("Array<", rank))}{type}{string.Join("", Enumerable.Repeat(">", rank))}";
                    }

                }

                return @$"{commonPrefix}{nullPrefix}
      let value : {valueType} = [];

      for ( let address of stepExtractArray( buffer, cursor, endCursor ) ) {{
        value.push( (() => {{ 
          {Deserialization( data, vtableOffsset, typesData, selectTypes, rank > 1, rank - 1, type, isGeneric, false, indent + 2).ReplaceLineEndings(Environment.NewLine +  String.Join( "", Enumerable.Repeat( "  ", indent + 2 ) ) ) }
        }})() )
      }}

{arrayPostfix}";
            }

            // Item is used in functions.
            if (isGeneric)
            {
                return string.Empty;
            }

            if (!typesData.ContainsKey(type))
            {
                return type switch
                {
                    "boolean" => logical ? @$"{commonPrefix}
     let value = stepExtractLogical( buffer, cursor, endCursor )

{commonPostfix}" : @$"{commonPrefix}
     let value = stepExtractBoolean( buffer, cursor, endCursor )

{commonPostfix}",
                    "number" => @$"{commonPrefix}
     let value = stepExtractNumber( buffer, cursor, endCursor )

{commonPostfix}",
                    "string" => @$"{commonPrefix}
     let value = stepExtractString( buffer, cursor, endCursor )

{commonPostfix}",

                    "[Uint8Array, number]" => @$"{commonPrefix}
     let value = stepExtractBinary( buffer, cursor, endCursor )

{commonPostfix}",
                    _ => throw new Exception("Unknown type requested deserializer string")
                };
            }

            var typeData = typesData[type];

            if (typeData is WrapperType wrapper)
            {
                return Deserialization(data, vtableOffsset, typesData, selectTypes, wrapper.IsCollectionType, wrapper.Rank, wrapper.WrappedType, isGeneric, useVtable, indent, usePrevCursor, wrapper.Name == "IfcLogical");
            }
            else if (typeData is Entity)
            {
                var entityPostFix = data.IsOptional && useVtable ?
$@"     if ( !( value instanceof {type} ) ) {{
        if ( stepExtractOptional( buffer, cursor, endCursor ) !== null ) {{
          throw new Error( 'Value in STEP was incorrectly typed for field' )
        }}

        return null
      }} else {{
        return value
      }}" : ( !usePrevCursor ?
$@"      if ( !( value instanceof {type} ) )  {{
        throw new Error( 'Value in STEP was incorrectly typed for field' )
      }}

      return value" :
$@"            if ( !( value instanceof {type} ) )
            {{                
                return (void 0);
            }};

            return value;");

                return @$"{commonPrefix}
       let expressID = stepExtractReference( buffer, cursor, endCursor );
       let value =
         expressID !== void 0 ? this.model.getElementByExpressID( expressID ) :
         this.model.getInlineElementByAddress( stepExtractInlineElemement( buffer, cursor, endCursor ) )

{entityPostFix}";
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
        
                var selectPostFix = data.IsOptional && useVtable ?
$@"      if ( {instanceCheck} ) {{
        if ( stepExtractOptional( buffer, cursor, endCursor ) !== null ) {{
          throw new Error( 'Value in STEP was incorrectly typed for field' )
        }}

        return null
      }} else {{
        return value{cast};
      }}" : (!usePrevCursor ?
$@"      if ( {instanceCheck} ) {{
        throw new Error( 'Value in STEP was incorrectly typed for field' )
      }}

      return value{cast}" :
$@"      if ( {instanceCheck} ) {{
        return (void 0)
      }}

      return value{cast}");

                return @$"{commonPrefix}
      let expressID = stepExtractReference( buffer, cursor, endCursor );
      let value : StepEntityBase< EntityTypesIfc >{(hasNullStyle ? " | IfcNullStyle" : "")} | undefined =
        expressID !== void 0 ? this.model.getElementByExpressID( expressID ) :
        (this.model.getInlineElementByAddress( stepExtractInlineElemement( buffer, cursor, endCursor )){nullStyle})

{selectPostFix}";
            }
            else if (typeData is EnumData collection)
            {
                return @$"{commonPrefix}
      let value = {typeData.Name}DeserializeStep( buffer, cursor, endCursor )

{commonPostfix}";
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

            string deserialization = Deserialization(data, vtableOffsset, typesData, selectTypes, data.IsCollection, rank, type, isGeneric);

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
      this.{data.Name}_ = (() => {{ 
        {deserialization} }})()
    }}

    return this.{data.Name}_ as {propertyTypeString}
  }}";
        }
    }
}
