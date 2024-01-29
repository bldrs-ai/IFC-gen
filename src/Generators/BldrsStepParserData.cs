using Express;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsStepParserData
    {
        
        public static void GenerateTypeIDFiles(string directory, IEnumerable<string> types, IEnumerable<bool> isAbstract, Dictionary< string, TypeData > typesData, Dictionary<string, SelectType> selectTypes, string shortName)
        {
            var entityTypesName = $"EntityTypes{shortName}";
            var shortNameLC = shortName.ToLowerInvariant();

            var typeIDs =
                new BlrdrsTypeIDGenerator(
                    Enumerable.Concat(
                        Enumerable.Repeat("ExternalMappingContainer", 1), types),
                    Enumerable.Concat(
                        Enumerable.Repeat(false, 1),
                        isAbstract));

            var enumFileName = $"entity_types_{shortNameLC}.gen";
            var enumPath = Path.Combine(directory, enumFileName + ".ts" );
            var enumBuilder = new StringBuilder();

            typeIDs.GenerateEnum(enumBuilder, entityTypesName, 0, true);
                 
            File.WriteAllText(enumPath, enumBuilder.ToString());

            var searchPath = Path.Combine(directory, "entity_types_search.gen.ts");
            var searchBuilder = new StringBuilder();

            typeIDs.GenerateHashData(searchBuilder, entityTypesName, enumFileName, 0);

            File.WriteAllText(searchPath, searchBuilder.ToString());

            var internalFileName = "index";
            var internalPath = Path.Combine(directory, internalFileName + ".ts");
            var internalBuilder = new StringBuilder();

            typeIDs.GenerateInternal(internalBuilder, typesData);

            File.WriteAllText(internalPath, internalBuilder.ToString());

            var schemaFileName = $"schema_{shortNameLC}.gen";
            var schemaPath = Path.Combine(directory, schemaFileName + ".ts");
            var schemaBuilder = new StringBuilder();

            typeIDs.GenerateSchema(schemaBuilder, $"Schema{shortName}", 0, entityTypesName, enumFileName, $"{entityTypesName}Search", "entity_types_search.gen", typesData, selectTypes);
            File.WriteAllText(schemaPath, schemaBuilder.ToString());
        }
    }
}
