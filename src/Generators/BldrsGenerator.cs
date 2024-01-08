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
        private List<WrapperType> wrappedTypes_ = new List<WrapperType>();

        /// <summary>
        /// A map of SelectType by name.
        /// This must be set before operations which require checking dependencies and attribute types.
        /// </summary>
        public Dictionary<string, TypeData> TypesData { get; set; }

        public BldrsGenerator()
        {
            SelectData = new Dictionary<string, SelectType>();
        }

        public string FileExtension => "gen.ts";

        public Dictionary<string, SelectType> SelectData { get; set; }

        public string AttributeDataString(AttributeData data)
        {
            return BldrsAttributeGenerator.AttributeDataString(data, TypesData);
        }

        public string AttributeDataType(bool isCollection, int rank, string type, bool isGeneric)
        {
            if (isCollection)
            {
                if (SelectData.ContainsKey(type))
                {
                    var unionType = string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(type, SelectData));

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
                return string.Join(" | ", BldrsSelectGenerator.ExpandPossibleTypes(type, SelectData));
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

        public string SimpleTypeString(WrapperType data)
        {
            wrappedTypes_.Add(data);

            return BldrsWrapperTypeGenerator.Generate( this, data, TypesData, SelectData );
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
            var componentTypeNames = Enumerable.Concat( componentTypes_.Select(type => type.SanitizedName()), wrappedTypes_.Select( type => type.SanitizedName()) );
            var abstractTypeNames = Enumerable.Concat( componentTypes_.Select(type => type.IsAbstract), wrappedTypes_.Select( type => false ) );

            BldrsStepParserData.GenerateTypeIDFiles(directory, componentTypeNames, abstractTypeNames, TypesData, SelectData);
        }
        public string SelectTypeString(SelectType data)
        {
            return BldrsSelectGenerator.GenerateSelectType( data, SelectData );
        }
    }
}
