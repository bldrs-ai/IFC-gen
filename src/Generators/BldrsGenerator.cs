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
            return BldrsAttributeGenerator.AttributeDataString(data);
        }


        public string AttributeDataType(bool isCollection, int rank, string type, bool isGeneric)
        {
            if (isCollection)
            {
                if (SelectData.ContainsKey(type))
                {
                    var unionType = string.Join('|', BldrsSelectGenerator.ExpandPossibleTypes(type, SelectData));

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
                return string.Join('|', BldrsSelectGenerator.ExpandPossibleTypes(type, SelectData));
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
                result.AddRange(BldrsSelectGenerator.ExpandPossibleTypes(a.type, SelectData));
            }

            return result.Distinct();
        }

        public string EntityString(Entity data)
        {
            componentTypes_.Add(data);

            return BldrsEntityGenerator.EntityString( data, SelectData, TypesData );
        }

        private string WrappedType(WrapperType data)
        {
            if (data.IsCollectionType)
            {
                return $"{string.Join("", Enumerable.Repeat("Array<", data.Rank))}{data.WrappedType}{string.Join("", Enumerable.Repeat(">", data.Rank))}";
            }
            return data.WrappedType;
        }


        public string SimpleTypeString(WrapperType data)
        {
            var badTypes = new List<string> { "boolean", "number", "string", "[Uint8Array, number]" };
            var wrappedTypeImport = badTypes.Contains(data.WrappedType) ? string.Empty : $"import {data.WrappedType} from \"./{data.WrappedType}.bldrs\"";

            var result =
$@"
    {wrappedTypeImport}

    // http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
    type {data.Name} = {WrappedType(data)};

    export default {data.Name};";
            return result;
        }

        public string EnumTypeString(EnumType data)
        {
            return BldrsEnumGenerator.GenerateEnumString(data);
        }
        public string ParseSimpleType(ExpressParser.SimpleTypeContext context)
        {
            var type = string.Empty;
            if (context.binaryType() != null)
            {
                type = "[Uint8Array, number]";
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
            //var schemaBuilder = new StringBuilder();

            //schemaBuilder.AppendLine("import SchemaSpecification from '../../core/schema_specification'");
            //schemaBuilder.AppendLine("import ComponentSpecification from '../../core/component_specification'");

            var componentTypeNames = componentTypes_.Select(type => type.Name);
            var abstractTypeNames = componentTypes_.Select(type => type.IsAbstract);

            //            foreach (var componentType in componentTypeNames)
            //            {
            //                schemaBuilder.AppendLine($"import {{{componentType}Specification}} from './{componentType}.bldrs'");
            //            }

            //            schemaBuilder.AppendLine("");
            //            schemaBuilder.AppendLine("export type IFCSchema = 'IFC';");
            //            schemaBuilder.AppendLine("");

            //            schemaBuilder.AppendLine($@"
            //export default class SchemaSpecificationIFC implements SchemaSpecification
            //{{
            //    public readonly name: IFCSchema = 'IFC';

            //    public readonly components : IfcComponentTypeNames[] = [ { string.Join(", ", componentTypes_.Select(componentType => $"'{componentType.Name}'")) } ];

            //    public readonly specifications : ReadonlyMap< IfcComponentTypeNames, ComponentSpecification >;

            //    constructor()
            //    {{
            //        let localSpecifications = new Map< IfcComponentTypeNames, ComponentSpecification >();

            //{componentTypes_.Select(componentType => $"\t\tlocalSpecifications.set( '{componentType.Name}', new {componentType.Name}Specification() );\n").Aggregate( string.Empty, ( left, right ) => left + right )}
            //        this.specifications = localSpecifications;
            //    }}
            //}}");

            //            schemaBuilder.AppendLine($"export type IfcComponentTypeNames = { string.Join('|', componentTypes_.Select(componentType => $"'{componentType.Name}'")) };");

            //            var schemaPath = Path.Combine(directory, "schema_ifc.bldrs.ts");

            //            File.WriteAllText(schemaPath, schemaBuilder.ToString());

            //            var modelBuilder = new StringBuilder();

            //            modelBuilder.AppendLine("import {IfcComponentTypeNames} from './schema_ifc.bldrs'");
            //            modelBuilder.AppendLine("import Entity from '../../core/entity'");
            //            modelBuilder.AppendLine("");
            //            modelBuilder.AppendLine("import IfcGloballyUniqueId from './IfcGloballyUniqueId.bldrs'");

            //            foreach (var componentType in componentTypes_.Select( type => type.Name ) )
            //            {
            //                modelBuilder.AppendLine($"import {componentType} from './{componentType}.bldrs'");
            //            }

            //            modelBuilder.AppendLine("");
            //            modelBuilder.AppendLine("");
            //            modelBuilder.AppendLine("export default class ModelIfc");
            //            modelBuilder.AppendLine("{");
            //            modelBuilder.AppendLine("\tpublic readonly components : IfcComponents = {};");
            //            modelBuilder.AppendLine("");
            //            modelBuilder.AppendLine("\tpublic readonly entities : Map< IfcGloballyUniqueId, Entity< IfcComponentTypeNames > > = new Map< IfcGloballyUniqueId, Entity< IfcComponentTypeNames > >();");
            //            modelBuilder.AppendLine("}");

            //            modelBuilder.AppendLine("");

            //            modelBuilder.AppendLine("export interface IfcComponents");
            //            modelBuilder.AppendLine("{");

            //            foreach ( var componentType in componentTypes_.Select( type => type.Name ) )
            //            {
            //                modelBuilder.AppendLine($"\t{componentType}? : Map< IfcGloballyUniqueId, {componentType}>;");
            //                modelBuilder.AppendLine("");
            //            }

            //            modelBuilder.AppendLine("}");

            //            var modelPath = Path.Combine(directory, "model_ifc.bldrs.ts");

            //            File.WriteAllText(modelPath, modelBuilder.ToString());

            //            var importBuilder = new StringBuilder();

            //            importBuilder.AppendLine($"export * from \"./model_ifc.bldrs\"");
            //            importBuilder.AppendLine($"export * from \"./schema_ifc.bldrs\"");

            //            foreach (var name in types)
            //            {
            //                importBuilder.AppendLine($"export * from \"./{name}.bldrs\"");
            //            }

            //            var indexPath = Path.Combine(directory, "index.ts");

            //            File.WriteAllText(indexPath, importBuilder.ToString());

            BldrsStepParserData.GenerateTypeIDFiles(directory, componentTypeNames, abstractTypeNames);
        }
        public string SelectTypeString(SelectType data)
        {
//            var containerTypes = new StringBuilder();


//            var importBuilder = new StringBuilder();

//            foreach (var d in Dependencies(data))
//            {
//                importBuilder.AppendLine($"import {d} from \"./{d}.bldrs\"");
//            }

//            var selectSize = data.Values.Count();

//            var result =
//$@"
//{importBuilder.ToString()}

///**
// * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
// */

//export default class {data.Name}
//{{
//    constructor( public readonly value: {data.Name}Variant ) {{}}
//}}

//export type {data.Name}Type = { string.Join('|', data.Values.Where( value => value != "IfcNullStyle" ).Select( value => $"'{value}'" ) ) };

//export type {data.Name}Choices = { string.Join('|', data.Values.Where(value => value != "IfcNullStyle") ) };

//export type {data.Name}Variant = ({ string.Join('|', data.Values.Where(value => value != "IfcNullStyle").Select(value => $"{{ type: '{value}'; value: {value} }}")) }) & {{ type: {data.Name}Type; value: {data.Name}Choices }};

//export function {data.Name}Serializer( value?: {data.Name}, to: SmartBuffer, offset?: number )
//{{
//    switch
//    {
//      ""  
//    }
//}}
//";

//            return result;

            return "";
        }
    }
}
