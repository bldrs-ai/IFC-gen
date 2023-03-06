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
            return $"private {data.Name}_? : {data.Type}{(data.IsOptional ? " | null" : "")}";
        }

        public static string Deserialization( AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typesData, bool isCollection, int rank, string type, bool isGeneric, bool isOuterCollection = true)
        {
            var commonPrefix = @$"this.guaranteeVTable();

            let internalReference = this.internalReference_ as Required< StepEntityInternalReference< EntityTypesIfc > >;

            if ( {vtableOffsset} >= internalReference.vtableCount )
            {{
                throw new Error( ""Couldn't read field {data.Name} due to too few fields in record"" ); 
            }}
            
            let vtableSlot = internalReference.vtableIndex + {vtableOffsset};

            let cursor    = internalReference.vtable[ vtableSlot ];
            let buffer    = internalReference.buffer;
            let endCursor = buffer.length;
";
            var commonPostfix = data.IsOptional ? 
$@"            if ( value !== void 0 )
            {{
                if ( stepExtractOptional( buffer, cursor, endCursor ) !== null )
                {{
                    throw new Error( 'Value in STEP was incorrectly typed for field {data.Name}' );
                }}

                this.{data.Name}_ = null;                
            }}
            else
            {{
                this.{data.Name}_ = value;
            }}":
$@"            if ( value === void 0 )
            {{                
                throw new Error( 'Value in STEP was incorrectly typed for field {data.Name}' );
            }};

            this.{data.Name}_ = value;";

            if (isCollection)
            {
                return "";
                //throw new NotImplementedException("TODO - Not Implemented yet - CS");
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
                    "boolean" => @$"{commonPrefix}
            let value = stepExtractBoolean( buffer, cursor, endCursor );

{commonPostfix}",
                    "number" => @$"{commonPrefix /* TODO - CS */}
            let value = stepExtractNumber( buffer, cursor, endCursor );

{commonPostfix}",
                    "string" => @$"{commonPrefix}
            let value = stepExtractString( buffer, cursor, endCursor );

{commonPostfix}",

                    "Uint8Array" => @$"{commonPrefix}
            let value = stepExtractBinary( buffer, cursor, endCursor );

{commonPostfix}",
                    _ => throw new Exception("Unknown type requested deserializer string")
                };
            }

            var typeData = typesData[type];

            if (typeData is WrapperType wrapper)
            {
                return Deserialization( data, vtableOffsset, typesData, false, 0, wrapper.WrappedType, false );
            }
            else if (typeData is Entity entity)
            {
                return @"";
            }
            else if (typeData is SelectType select)
            {
                return $"";
            }
            else if (typeData is EnumData collection)
            {
                return @$"{commonPrefix}
            let value = {typeData.Name}DeserializeStep( buffer, cursor, endCursor );

{commonPostfix}";
            }

            return "";
        }

        public static string AttributePropertyString(AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typeData, int rank, string type, bool isGeneric, bool isOuterCollection = true)
        {
            if (data.IsDerived || data.IsInverse)
            {
                return "";
            }

            var propertyTypeString = $"{ data.Type }{ (data.IsOptional ? " | null" : string.Empty)}";

            return $@"
    public get {data.Name}() : {propertyTypeString}
    {{
        if ( this.{data.Name}_ === void 0 )
        {{
            {Deserialization(data, vtableOffsset, typeData, data.IsCollection, rank, type, isGeneric, isOuterCollection )}
        }}

        return this.{data.Name}_ as {propertyTypeString};
    }}";
        }
    }
}
