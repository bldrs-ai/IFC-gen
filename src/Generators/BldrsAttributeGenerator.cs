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

        public static string Deserialization( AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typesData, Dictionary<string, SelectType> selectTypes, bool isCollection, int rank, string type, bool isGeneric, bool useVtable = true, int indent = 0, bool usePrevCursor = false )
        {
            var commonPrefix = useVtable ? @$"this.guaranteeVTable();

            let internalReference = this.internalReference_ as Required< StepEntityInternalReference< EntityTypesIfc > >;

            if ( {vtableOffsset} >= internalReference.vtableCount )
            {{
                throw new Error( ""Couldn't read field due to too few fields in record"" ); 
            }}
            
            let vtableSlot = internalReference.vtableIndex + {vtableOffsset};

            let cursor    = internalReference.vtable[ vtableSlot ];
            let buffer    = internalReference.buffer;
            let endCursor = buffer.length;
" : (usePrevCursor ? "" : $@"let cursor = address;
");
            var commonPostfix = data.IsOptional && useVtable ? 
$@"            if ( value === void 0 )
            {{
                if ( stepExtractOptional( buffer, cursor, endCursor ) !== null )
                {{
                    throw new Error( 'Value in STEP was incorrectly typed' );
                }}

                return null;                
            }}
            else
            {{
                return value;
            }}":
$@"            if ( value === void 0 )
            {{                
                throw new Error( 'Value in STEP was incorrectly typed' );
            }};

            return value;";

            if (isCollection)
            {
                string valueType;

                if (selectTypes.ContainsKey(type))
                {
                    var unionType = string.Join('|', BldrsSelectGenerator.ExpandPossibleTypes(type, selectTypes));

                    valueType = $"{string.Join("", Enumerable.Repeat("Array<", rank))}{unionType}{string.Join("", Enumerable.Repeat(">", rank))}";
                }
                else
                {
                    valueType = $"{string.Join("", Enumerable.Repeat("Array<", rank))}{type}{string.Join("", Enumerable.Repeat(">", rank))}";
                }

                    return @$"{commonPrefix}
            let value : {valueType} = [];

            for ( let address of stepExtractArray( buffer, cursor, endCursor ) )
            {{
                value.push( (() => {{ 
                    {Deserialization( data, vtableOffsset, typesData, selectTypes, rank > 1, rank - 1, type, isGeneric, false, indent + 2).ReplaceLineEndings(Environment.NewLine +  String.Join( "", Enumerable.Repeat( "    ", indent + 2 ) ) ) }
                }})() );
            }}

{commonPostfix}";
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
                    "number" => @$"{commonPrefix}
            let value = stepExtractNumber( buffer, cursor, endCursor );

{commonPostfix}",
                    "string" => @$"{commonPrefix}
            let value = stepExtractString( buffer, cursor, endCursor );

{commonPostfix}",

                    "[Uint8Array, number]" => @$"{commonPrefix}
            let value = stepExtractBinary( buffer, cursor, endCursor );

{commonPostfix}",
                    _ => throw new Exception("Unknown type requested deserializer string")
                };
            }

            var typeData = typesData[type];

            if (typeData is WrapperType wrapper)
            {
                return Deserialization( data, vtableOffsset, typesData, selectTypes, wrapper.IsCollectionType, wrapper.Rank, wrapper.WrappedType, isGeneric, useVtable, indent, usePrevCursor );
            }
            else if (typeData is Entity entity)
            {
                var entityPostFix = data.IsOptional && useVtable ?
$@"            if ( value === void 0 || !( value instanceof {type} ) )
            {{
                if ( stepExtractOptional( buffer, cursor, endCursor ) !== null )
                {{
                    throw new Error( 'Value in STEP was incorrectly typed for field' );
                }}

                return null;                
            }}
            else
            {{
                return value;
            }}" :
$@"            if ( value === void 0 || !( value instanceof {type} ) )
            {{                
                throw new Error( 'Value in STEP was incorrectly typed for field' );
            }};

            return value;";

                return @$"{commonPrefix}
            let expressID = stepExtractReference( buffer, cursor, endCursor );
            let value     = expressID !== void 0 ? this.model.getElementByExpressID( expressID ) : this.model.getInlineElementByAddress( stepExtractInlineElemement( buffer, cursor, endCursor ) );           

{entityPostFix}";
            }
            else if (typeData is SelectType select)
            {
                return @$"{commonPrefix}
            let value = { string.Join( " ?? ", BldrsSelectGenerator.ExpandPossibleTypes(type, selectTypes).Select( innerType => $"( () => {{ try {{ {Deserialization( data, vtableOffsset, typesData, selectTypes, false, 0, innerType, isGeneric, false, indent + 2, true).ReplaceLineEndings(Environment.NewLine +  String.Join( "", Enumerable.Repeat( "    ", indent + 2 ) ) ) } }} catch( e ) {{ return; }} }} )()" ) ) };

{commonPostfix}";
            }
            else if (typeData is EnumData collection)
            {
                return @$"{commonPrefix}
            let value = {typeData.Name}DeserializeStep( buffer, cursor, endCursor );

{commonPostfix}";
            }

            return "";
        }

        public static string AttributePropertyString(AttributeData data, uint vtableOffsset, Dictionary<string, TypeData> typeData, Dictionary<string, SelectType> selectTypes, int rank, string type, bool isGeneric)
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
            this.{data.Name}_ = (() => {{ {Deserialization(data, vtableOffsset, typeData, selectTypes, data.IsCollection, rank, type, isGeneric )} }})();
        }}

        return this.{data.Name}_ as {propertyTypeString};
    }}";
        }
    }
}
