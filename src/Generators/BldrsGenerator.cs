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

            return $"\t{data.Name}{(data.IsOptional ? "?" : string.Empty)} : {data.Type};";
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

            var badTypes = new List<string> { "boolean", "number", "string", "boolean", "Uint8Array" };
            var types = result.Distinct().Where(t => !badTypes.Contains(t) && t != entity.Name);

            return types;
        }

        public string EntityString(Entity data)
        {
            componentTypes_.Add(data);

            var importBuilder = new StringBuilder();

            foreach (var d in Dependencies(data))
            {
                importBuilder.AppendLine($"import {{{d}}} from \"./{d}.bldrs\"");
            }

            var super = "BaseIfc";
            var newMod = string.Empty;
            //if (data.Subs.Any())
            //{
            //    super = data.Subs[0].Name; ;
            //    newMod = "new";
            //}


            string constructors = string.Empty;

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
{importBuilder.ToString()}

/**
 * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
 */
export default class {data.Name} implements Component< SchemaSpecificationIFC > 
{{
    public readonly __type__ = '{data.Name}';

    public readonly __version__: number = 0;

{data.Properties(SelectData)}{constructors}
}}

export class {data.Name}Specification implements ComponentSpecification
{{
    public readonly name: string = '{data.Name}';

    public readonly required: string[] = [ {string.Join( ", ", data.Parents().Select( (superValue)=> $"'{superValue.Name}'" ) )} ];

    public readonly isAbstract: boolean = {(data.IsAbstract ? "true" : "false" )};

    public readonly attributes: AttributeSpecification[] = 
    [{string.Join( ", ", data.Attributes.Where( attr => !attr.IsInverse && !attr.IsDerived ).Select( attr => $"\n\t\t{{\n\t\t\tname: '{attr.Name}',\n\t\t\tisCollection: {( attr.IsCollection ? "true" : "false")},\n\t\t\trank: {attr.Rank},\n\t\t\tbaseType: '{attr.Type}'\n\t\t}}"))}
    ];
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

        public string SimpleTypeString(WrapperType data)
        {
            var badTypes = new List<string> { "boolean", "number", "string", "boolean", "Uint8Array" };
            var wrappedTypeImport = badTypes.Contains(data.WrappedType) ? string.Empty : $"import {{{data.WrappedType}}} from \"./{data.WrappedType}.g\"";
            var result =
$@"
import {{BaseIfc}} from ""./BaseIfc""
{wrappedTypeImport}

// http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
export type {data.Name} = {WrappedType(data)}";
            return result;
        }

        public string EnumTypeString(EnumType data)
        {
            var result =
$@"
//http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/{data.Name.ToLower()}.htm
export enum {data.Name} 
{{
	{string.Join(",\n\t", data.Values.Select(v => $"{v}=\".{v}.\""))}
}}
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

            schemaBuilder.AppendLine($@"
export default class SchemaSpecificationIFC implements SchemaSpecification
{{
    public readonly name: string = 'IFC';
    
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
            return string.Empty;
        }
    }
}
