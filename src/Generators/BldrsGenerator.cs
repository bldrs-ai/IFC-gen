using Express;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace IFC4.Generators
{
    public class BldrsGenerator : ILanguageGenerator
    {
        private List<Entity> componentTypes_ = new List<Entity>();
        
        /// <summary>
        /// A map of SelectType by name.
        /// This must be set before operations which require checking dependencies and attribute types.
        /// </summary>
        public Dictionary<string, TypeData> TypesData { get; set; }

        public BldrsGenerator()
        {
            SelectData = new Dictionary<string, SelectType>();
        }

        public string FileExtension => "bldrs.ts";

        public Dictionary<string, SelectType> SelectData { get; set; }

        public string AttributeDataString(AttributeData data)
        {
            if (data.IsDerived || data.IsInverse)
            {
                return "";
            }

            return $"private {data.Name}_? : {data.Type}";
        }

        private string AttributePropertyString(AttributeData data, uint serializationOffset)
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

        private uint ExpandMaximumSizes(string baseType)
        {
            if (!SelectData.ContainsKey(baseType))
            {
                // return right away, it's not a select
                return AttributeSerializationSize( false, 0, baseType, false );
            }

            var values = SelectData[baseType].Values;
            uint result = 0;

            foreach (var v in values)
            {
                result = Math.Max(result, ExpandMaximumSizes(v));
            }

            return result;
        }

        private IEnumerable<string> ExpandPossibleTypes(string baseType)
        {
            if (!SelectData.ContainsKey(baseType))
            {
                // return right away, it's not a select
                return new List<string> { baseType };
            }

            var values = SelectData[baseType].Values;
            var result = new List<string>();

            foreach (var v in values)
            {
                result.AddRange(ExpandPossibleTypes(v));
            }

            return result;
        }

        public uint AttributeSerializationSize(bool isCollection, int rank, string type, bool isGeneric)
        {
            if (isCollection)
            {
                return 4;
            }

            // Item is used in functions.
            if (isGeneric)
            {
                return 0;
            }

            // https://github.com/ikeough/IFC-gen/issues/25
            if (type == "IfcSiUnitName")
            {
                return 4;
            }

            if (SelectData.ContainsKey(type))
            {
                return ExpandMaximumSizes(type) + 2; // 2 byte selector + largest size.
            }

            var typeData = TypesData[type];

            if (typeData is WrapperType wrapper)
            {
                if (wrapper.WrappedType == "number")
                {
                    return 8; // TODO - work out which are integers so we don't have to serialize freakin doubles - CS
                }
                else if (wrapper.WrappedType == "boolean")
                {
                    return 1;
                }
                else if (wrapper.WrappedType == "UInt8Array")
                {
                    return 4;
                }
                else if (wrapper.WrappedType == "string")
                {
                    return 4;
                }
                else
                {
                    return AttributeSerializationSize( false, 0, wrapper.WrappedType, false )
                }
            }
            else if ( typeData is Entity entity )
            {
                return 4; // entities are always referred to by reference.
            }
            else if ( typeData is EnumData collection )
            {
                var items = collection.Values.Count();

                return ( items < 254 ) ? 1u : ( items < 65534 ? 2u : 4u ); // collections are always serialized to an offset reference.
            }

            return 0;
        }

        public string AttributeDataType(bool isCollection, int rank, string type, bool isGeneric)
        {
            if (isCollection)
            {
                if (SelectData.ContainsKey(type))
                {
                    var unionType = string.Join('|', ExpandPossibleTypes(type));

                    return $"{string.Join("", Enumerable.Repeat("Array<", rank))}{unionType}{string.Join("", Enumerable.Repeat(">", rank))}";
                }
                else
                {
                    return $"{string.Join("", Enumerable.Repeat("Array<", rank))}{type}{string.Join("", Enumerable.Repeat(">", rank))}";
                }
            }

            // Item is used in functions.
            if (isGeneric)
            {
                return "T";
            }

            // https://github.com/ikeough/IFC-gen/issues/25
            if (type == "IfcSiUnitName")
            {
                return "IfcSIUnitName";
            }

            if (SelectData.ContainsKey(type))
            {
                return string.Join('|', ExpandPossibleTypes(type));
            }

            return type;
        }

        public string AttributeStepString(AttributeData data, bool isDerivedInChild)
        {
            return string.Empty;
        }

        private IEnumerable<string> AddRelevantTypes(IEnumerable<AttributeData> attrs)
        {
            var result = new List<string>();

            foreach (var a in attrs)
            {
                result.AddRange(ExpandPossibleTypes(a.type));
            }

            return result.Distinct();
        }

        public IEnumerable<string> Dependencies(Entity entity)
        {
            //var parents = entity.ParentsAndSelf().Reverse();
            //var attrs = parents.SelectMany(p => p.Attributes);

            var result = new List<string>();

            //result.AddRange(AddRelevantTypes(attrs)); // attributes for constructor parameters for parents
            result.AddRange(AddRelevantTypes(entity.Attributes)); // atributes of self
            //result.AddRange(this.Supers.Select(s=>s.Name)); // attributes for all sub-types
            //result.AddRange(entity.Subs.Select(s => s.Name)); // attributes for all super types

            var badTypes = new List<string> { "boolean", "number", "string", "Uint8Array" };
            var types = result.Distinct().Where(t => !badTypes.Contains(t) && t != entity.Name);

            return types;
        }

        public IEnumerable<string> Dependencies(SelectType selectType)
        {
            //var parents = entity.ParentsAndSelf().Reverse();
            //var attrs = parents.SelectMany(p => p.Attributes);

            var result = new List<string>();

            //result.AddRange(AddRelevantTypes(attrs)); // attributes for constructor parameters for parents
            result.AddRange(selectType.Values); // atributes of self
            //result.AddRange(this.Supers.Select(s=>s.Name)); // attributes for all sub-types
            //result.AddRange(entity.Subs.Select(s => s.Name)); // attributes for all super types

            var badTypes = new List<string> { "boolean", "number", "string", "Uint8Array" };
            var types = result.Distinct().Where(t => !badTypes.Contains(t) && t != selectType.Name && t != "IfcNullStyle");

            return types;
        }

        public string EntityString(Entity data)
        {
            componentTypes_.Add(data);

            var importBuilder = new StringBuilder();

            foreach (var d in Dependencies(data))
            {
                importBuilder.AppendLine($"import {d} from \"./{d}.bldrs\"");
            }

            var newMod = string.Empty;
            //if (data.Subs.Any())
            //{
            //    super = data.Subs[0].Name; ;
            //    newMod = "new";
            //}
            string superClass = "EntityBase< SchemaSpecificationIFC >";

            if ( data.Subs.Count > 0 )
            {
                superClass = data.Subs[ 0 ].Name;
            }

            string componentTypeNames = $"[{string.Join(", ", data.ParentsAndSelf().Select(value => value.Name))}]";

            string modifiers = data.IsAbstract ? "abstract" : string.Empty;


            //        constructors = $@"
            //constructor({ConstructorParams(data, false)}) {{
            //    super({BaseConstructorParams(data, false)}){Assignments(data, false)}
            //}}";

            var result =
$@"
import Component from ""../../core/component""
import ComponentSpecification from ""../../core/component_specification""
import AttributeSpecification from ""../../core/attribute_specification""
import SchemaSpecificationIFC from ""./schema_ifc.bldrs""
import {{ IFCSchema }} from ""./schema_ifc.bldrs""
{importBuilder.ToString()}

/**
 * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
 */
export default {modifiers} class {data.Name} extends {superClass} 
{{    
    public readonly specification: {data.Name}Specification = {data.Name}Specification.instance;

{string.Join("\n    ", data.Attributes.Where( value => value.ToString() != string.Empty).Select( value => value.ToString() ) ) }

    constructor( buffer: SnapshotBuffer< T >, dirtyProvider?: ( entity: Entity< T > ) => void )
    constructor( fileIDProvider: () => number, dirtyProvider?: ( entity: Entity< T > ) => void )
    constructor( bufferOrFileIDProvider: SnapshotBuffer< T > | ( () => number ), private readonly dirtyProvider_?: ( entity: Entity< T > ) => void ) 
    {{
        super( bufferOrFileIDProvider, dirtyProvider_ );
    }}

}}

export class {data.Name}Specification implements ComponentSpecification
{{
    public readonly name: string = '{data.Name}';

    public readonly required: ReadonlyArray< string > = [ {string.Join( ", ", data.ParentsAndSelf().Select( (superValue)=> $"'{superValue.Name}'" ) )} ];

    public readonly isAbstract: boolean = {(data.IsAbstract ? "true" : "false" )};

    public readonly attributes: ReadonlyArray< AttributeSpecification > = 
    [{string.Join( ", ", data.Attributes.Where( attr => !attr.IsInverse && !attr.IsDerived ).Select( attr => $"\n\t\t{{\n\t\t\tname: '{attr.Name}',\n\t\t\tisCollection: {( attr.IsCollection ? "true" : "false")},\n\t\t\trank: {attr.Rank},\n\t\t\tbaseType: '{attr.Type}',\n\t\t\toptional: {(attr.IsOptional ? "true" : "false")}\n\t\t}}"))}
    ];

    public readonly schema: IFCSchema = 'IFC';

    public static readonly instance: {data.Name}Specification = new {data.Name}Specification();
}}
";
            return result;
        }

        private string WrappedType(WrapperType data)
        {
            if (data.IsCollectionType)
            {
                return $"{string.Join("", Enumerable.Repeat("Array<", data.Rank))}{data.WrappedType}{string.Join("", Enumerable.Repeat(">", data.Rank))}";
            }
            return data.WrappedType;
        }


        private string DeserializerString(bool isCollection, int rank, string type, bool isGeneric, bool isOuterCollection = true)
        {
            if (isCollection)
            {
                throw new NotImplementedException("TODO - Not Implemented yet - CS");
            }

            // Item is used in functions.
            if (isGeneric)
            {
                return string.Empty;
            }

            var typeData = TypesData[type];

            if (typeData is WrapperType wrapper)
            {
                return wrapper.WrappedType switch
                {
                    "boolean" => @"
    ( () => { 
        let readValue = from.readUInt8( offset ); 
        
        if ( readValue > 2 )
        {
            throw new Error( 'Read Invalid Value' );
        }

        return readValue == 0 ? false : ( readValue == 1 ? true : undefined );
    })()",
                    "number" =>
@"
    ( () => { 
        let readValue = from.readDoubleLE( offset ); 

        return Number.isNaN( readValue ) ? undefined : readValue;
    })()",
                    "string" => @"
    ( () => { 
        let readOffset = from.readUInt32LE( offset ); 

        if ( readOffset == 0 )
        {
            return;
        }

        let stringSize = from.readUInt32LE( readOffset );
        
        return from.readString( stringSize );
    })()",

                    "UInt8Array" => @"
    ( () => { 
        let readOffset = from.readUInt32LE( offset ); 

        if ( readOffset == 0 )
        {
            return;
        }

        let stringSize = from.readUInt32LE( readOffset );
        
        return from.readBuffer( stringSize );
    })()",
                    _ => SerializerString(false, 0, wrapper.WrappedType, false)
                };
            }
            else if (typeData is Entity entity)
            {
                return @"
    ( () => { 
        let readOffset = from.readUInt32LE( offset ); 

        if ( readOffset == 0 )
        {
            return;
        }
        
        return from.readBuffer( stringSize );
    })()"
            }
            else if (typeData is SelectType select)
            {
                return $"    {typeData.Name}Serializer( value, offset );";
            }
            else if (typeData is EnumData collection)
            {
                return $"    {typeData.Name}Serializer( value, offset );";
            }

            return "";
        }

        private string SerializerString(bool isCollection, int rank, string type, bool isGeneric, bool isOuterCollection = true )
        {
            if (isCollection)
            {
                throw new NotImplementedException("TODO - Not Implemented yet - CS");
            }

            // Item is used in functions.
            if (isGeneric)
            {
                return string.Empty;
            }

            var typeData = TypesData[type];

            if (typeData is WrapperType wrapper)
            {
                return wrapper.WrappedType switch
                {
                    "boolean" => "    to.writeUInt8( ( value === undefined ) ? 3 : ( value ? 1 : 0 ), offset )",
                    "number" => "    to.writeDoubleLE( ( value === undefined ) ? NaN : value, offset )",
                    "string" => @"
    if ( value == undefined ) 
    {
        to.writeUInt32LE( 0 );
    } 
    else 
    {
        to.writeUInt32LE( to.length, offset );
        to.writeUInt32LE( value.length );          
        to.writeString( value );          
    }",
                    "UInt8Array" => @"
    if ( value == undefined ) 
    {
        to.writeUInt32LE( 0 );
    } 
    else 
    {
        to.writeUInt32LE( to.length, offset );
        to.writeUInt32LE( value.length );          
        to.writeBuffer( value );          
    }",
                    _ => SerializerString(false, 0, wrapper.WrappedType, false)
                };
            }
            else if (typeData is Entity entity)
            {
                return @"    to.writeUInt32LE( value === undefined ? 0 : value.fileID, offset );";
            }
            else if (typeData is SelectType select )
            {
                return $"    {typeData.Name}Serializer( value, offset );";
            }
            else if (typeData is EnumData collection)
            {
                return $"    {typeData.Name}Serializer( value, offset );";
            }

            return "";
        }

        public string SimpleTypeString(WrapperType data)
        {
            var badTypes = new List<string> { "boolean", "number", "string", "Uint8Array" };
            var wrappedTypeImport = badTypes.Contains(data.WrappedType) ? string.Empty : $"import {data.WrappedType}, {{{data.WrappedType}Serialize, {data.WrappedType}Deserialize, {data.WrappedType}Size }} from \"./{data.WrappedType}.bldrs\"";

            uint typeSize = AttributeSerializationSize(false, 0, data.WrappedType, false);

            string serializationFunctions =
                @$"
export const {data.Name}Size = {typeSize};

export function {data.Name}Serializer( value?: {data.Name}, to: SmartBuffer, offset?: number ): void
{{{
    SerializerString(false, 0, data.WrappedType, false)
}}}

export function {data.Name}Deserializer( value?: {data.Name}, from: SnapshotBuffer< SchemaSpecificationIFC >, offset?: number ): void
{{{
    DeserializerString(false, 0, data.WrappedType, false)
}}}";

            
            var result =
$@"
import SchemaSpecificationIFC from ""./schema_ifc.bldrs""
import {{ SnapshotBuffer }} from './ snapshot';
import {{ SmartBuffer }} from 'smart-buffer';
{wrappedTypeImport}

// http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
type {data.Name} = {WrappedType(data)};

{serializationFunctions}

export default {data.Name};";
            return result;
        }

        public string EnumTypeString(EnumType data)
        {
            uint typeSize = AttributeSerializationSize(false, 0, data.Name, false);

            var items = data.Values.Count();

            var writer = (items < 254) ? "writeUInt8" : (items < 65534 ? "writeUInt16LE" : "writeUInt32LE");
            var reader = (items < 254) ? "readUInt8" : (items < 65534 ? "readUInt16LE" : "readUInt32LE");

            var serializationBuilder = new StringBuilder();
            var deserializationBuilder = new StringBuilder();

            serializationBuilder.AppendLine("    let writeValue = 0;\n");
            serializationBuilder.AppendLine("    switch ( value )\n    {");

            deserializationBuilder.AppendLine($"    let readValue = {reader};\n");
            deserializationBuilder.AppendLine("    switch ( readValue )\n    {");
            deserializationBuilder.AppendLine("        case 0: { return; }");

            int counter = 1;

            foreach (var item in data.Values)
            {
                serializationBuilder.AppendLine($@"        case {data.Name}.{item}: {{ writeValue = {counter}; break; }} ");
                deserializationBuilder.AppendLine($@"        case {counter}: return {data.Name}.{item};");

                ++counter;
            }

            deserializationBuilder.AppendLine("    }\n");
            deserializationBuilder.AppendLine("    throw new Error( 'Invalid value from deserializing enum' );");

            serializationBuilder.AppendLine("    }\n");
            serializationBuilder.AppendLine($"    {writer}( writeValue, offset );");

            string serializationFunctions =
                @$"
export const {data.Name}Size = {typeSize};

export function {data.Name}Serializer( value?: {data.Name}, to: SmartBuffer, offset?: number ): void
{{
{serializationBuilder.ToString()}
}}

export function {data.Name}Deserializer( to: SmartBuffer, offset?: number ): {data.Name} | undefined
{{{
{deserializationBuilder.ToString()}
}}}";

            var result =
$@"
//http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
enum {data.Name} 
{{
	{string.Join(",\n\t", data.Values.Select(v => $"{v}=\".{v}.\""))}
}};

export default {data.Name};
";
            return result;
        }
        public string ParseSimpleType(ExpressParser.SimpleTypeContext context)
        {
            var type = string.Empty;
            if (context.binaryType() != null)
            {
                type = "Uint8Array";
            }
            else if (context.booleanType() != null)
            {
                type = "boolean";
            }
            else if (context.integerType() != null)
            {
                type = "number";
            }
            else if (context.logicalType() != null)
            {
                type = "boolean";
            }
            else if (context.numberType() != null)
            {
                type = "number";
            }
            else if (context.realType() != null)
            {
                type = "number";
            }
            else if (context.stringType() != null)
            {
                type = "string";
            }
            return type;
        }

        public void GenerateManifest(string directory, IEnumerable<string> types)
        {
            var schemaBuilder = new StringBuilder();

            schemaBuilder.AppendLine("import SchemaSpecification from '../../core/schema_specification'");
            schemaBuilder.AppendLine("import ComponentSpecification from '../../core/component_specification'");

            foreach (var componentType in componentTypes_.Select(type => type.Name))
            {
                schemaBuilder.AppendLine($"import {{{componentType}Specification}} from './{componentType}.bldrs'");
            }

            schemaBuilder.AppendLine("");
            schemaBuilder.AppendLine("export type IFCSchema = 'IFC';");
            schemaBuilder.AppendLine("");

            schemaBuilder.AppendLine($@"
export default class SchemaSpecificationIFC implements SchemaSpecification
{{
    public readonly name: IFCSchema = 'IFC';
    
    public readonly components : IfcComponentTypeNames[] = [ { string.Join(", ", componentTypes_.Select(componentType => $"'{componentType.Name}'")) } ];

    public readonly specifications : ReadonlyMap< IfcComponentTypeNames, ComponentSpecification >;

    constructor()
    {{
        let localSpecifications = new Map< IfcComponentTypeNames, ComponentSpecification >();

{componentTypes_.Select(componentType => $"\t\tlocalSpecifications.set( '{componentType.Name}', new {componentType.Name}Specification() );\n").Aggregate( string.Empty, ( left, right ) => left + right )}
        this.specifications = localSpecifications;
    }}
}}");

            schemaBuilder.AppendLine($"export type IfcComponentTypeNames = { string.Join('|', componentTypes_.Select(componentType => $"'{componentType.Name}'")) };");

            var schemaPath = Path.Combine(directory, "schema_ifc.bldrs.ts");

            File.WriteAllText(schemaPath, schemaBuilder.ToString());

            var modelBuilder = new StringBuilder();

            modelBuilder.AppendLine("import {IfcComponentTypeNames} from './schema_ifc.bldrs'");
            modelBuilder.AppendLine("import Entity from '../../core/entity'");
            modelBuilder.AppendLine("");
            modelBuilder.AppendLine("import IfcGloballyUniqueId from './IfcGloballyUniqueId.bldrs'");

            foreach (var componentType in componentTypes_.Select( type => type.Name ) )
            {
                modelBuilder.AppendLine($"import {componentType} from './{componentType}.bldrs'");
            }

            modelBuilder.AppendLine("");
            modelBuilder.AppendLine("");
            modelBuilder.AppendLine("export default class ModelIfc");
            modelBuilder.AppendLine("{");
            modelBuilder.AppendLine("\tpublic readonly components : IfcComponents = {};");
            modelBuilder.AppendLine("");
            modelBuilder.AppendLine("\tpublic readonly entities : Map< IfcGloballyUniqueId, Entity< IfcComponentTypeNames > > = new Map< IfcGloballyUniqueId, Entity< IfcComponentTypeNames > >();");
            modelBuilder.AppendLine("}");

            modelBuilder.AppendLine("");

            modelBuilder.AppendLine("export interface IfcComponents");
            modelBuilder.AppendLine("{");
      
            foreach ( var componentType in componentTypes_.Select( type => type.Name ) )
            {
                modelBuilder.AppendLine($"\t{componentType}? : Map< IfcGloballyUniqueId, {componentType}>;");
                modelBuilder.AppendLine("");
            }

            modelBuilder.AppendLine("}");

            var modelPath = Path.Combine(directory, "model_ifc.bldrs.ts");

            File.WriteAllText(modelPath, modelBuilder.ToString());

            var importBuilder = new StringBuilder();

            importBuilder.AppendLine($"export * from \"./model_ifc.bldrs\"");
            importBuilder.AppendLine($"export * from \"./schema_ifc.bldrs\"");

            foreach (var name in types)
            {
                importBuilder.AppendLine($"export * from \"./{name}.bldrs\"");
            }

            var indexPath = Path.Combine(directory, "index.ts");

            File.WriteAllText(indexPath, importBuilder.ToString());
        }
        public string SelectTypeString(SelectType data)
        {
            var containerTypes = new StringBuilder();


            var importBuilder = new StringBuilder();

            foreach (var d in Dependencies(data))
            {
                importBuilder.AppendLine($"import {d} from \"./{d}.bldrs\"");
            }

            var selectSize = data.Values.Count();

            var result =
$@"
{importBuilder.ToString()}

/**
 * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
 */

export default class {data.Name}
{{
    constructor( public readonly value: {data.Name}Variant ) {{}}
}}

export type {data.Name}Type = { string.Join('|', data.Values.Where( value => value != "IfcNullStyle" ).Select( value => $"'{value}'" ) ) };

export type {data.Name}Choices = { string.Join('|', data.Values.Where(value => value != "IfcNullStyle") ) };

export type {data.Name}Variant = ({ string.Join('|', data.Values.Where(value => value != "IfcNullStyle").Select(value => $"{{ type: '{value}'; value: {value} }}")) }) & {{ type: {data.Name}Type; value: {data.Name}Choices }};

export function {data.Name}Serializer( value?: {data.Name}, to: SmartBuffer, offset?: number )
{{
    switch
    {
        
    }
}}
";

            return result;
        }
    }
}
