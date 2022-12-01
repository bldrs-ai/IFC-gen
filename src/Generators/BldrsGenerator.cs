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
        private List<string> componentTypes_ = new List<string>();

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

        public string EntityString(Entity data)
        {
            componentTypes_.Add(data.Name);

            return string.Empty;
        }

        public string EnumTypeString(EnumType data)
        {
            return string.Empty;
        }

        public void GenerateManifest(string directory, IEnumerable<string> types)
        {
            var modelBuilder = new StringBuilder();
            var importBuilder = new StringBuilder();

            modelBuilder.AppendLine("import Entity from ../core/entity");
            modelBuilder.AppendLine("");

            modelBuilder.AppendLine("export default class ModelIfc");
            modelBuilder.AppendLine("{");
            modelBuilder.AppendLine("\tpublic readonly components : IfcComponents = {};");
            modelBuilder.AppendLine("");
            modelBuilder.AppendLine("\tpublic readonly entities : Entity< IfcComponentTypeNames > = {};");
            modelBuilder.AppendLine("}");

            modelBuilder.AppendLine($"export type IfcComponentTypeNames = { string.Join('|', componentTypes_.Select(name => $"'{name}'")) };");
            modelBuilder.AppendLine("export interface IfcComponents");
            modelBuilder.AppendLine("{");
      
            foreach ( var componentType in componentTypes_ )
            {
                modelBuilder.AppendLine($"\t{componentType}? : Map< IfcGloballyUniqueId, componentType>;");
                modelBuilder.AppendLine("");
            }

            modelBuilder.AppendLine("}");

            var modelPath = Path.Combine(directory, "model_ifc.bldrs.ts");

            importBuilder.AppendLine($"export * from \"./model_ifc.bldrs\"");

            foreach (var name in types)
            {
                importBuilder.AppendLine($"export * from \"./{name}.bldrs\"");
            }

            var indexPath = Path.Combine(directory, "index.ts");

            File.WriteAllText(indexPath, importBuilder.ToString());
        }

        public string ParseSimpleType(ExpressParser.SimpleTypeContext context)
        {
            return string.Empty;
        }

        public string SelectTypeString(SelectType data)
        {
            return string.Empty;
        }

        public string SimpleTypeString(WrapperType data)
        {
            return string.Empty;
        }
    }
}
