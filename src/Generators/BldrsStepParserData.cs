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
        
        public static void GenerateTypeIDFiles(string directory, IEnumerable<string> types, IEnumerable<bool> isAbstract, Dictionary< string, TypeData > typesData )
        {
            var typeIDs = new BlrdrsTypeIDGenerator(types, isAbstract);


            var enumFileName = "entity_types_ifc.gen";
            var enumPath = Path.Combine(directory, enumFileName + ".ts" );
            var enumBuilder = new StringBuilder();

            typeIDs.GenerateEnum(enumBuilder, "EntityTypesIfc", 0, true);
                 
            File.WriteAllText(enumPath, enumBuilder.ToString());

            var searchPath = Path.Combine(directory, "entity_types_search.gen.ts");
            var searchBuilder = new StringBuilder();

            typeIDs.GenerateHashData(searchBuilder, "EntityTypesIfc", enumFileName, 0);

            File.WriteAllText(searchPath, searchBuilder.ToString());

            var internalFileName = "index";
            var internalPath = Path.Combine(directory, internalFileName + ".ts");
            var internalBuilder = new StringBuilder();

            typeIDs.GenerateInternal(internalBuilder, typesData);

            File.WriteAllText(internalPath, internalBuilder.ToString());

            var schemaFileName = "schema_ifc.gen";
            var schemaPath = Path.Combine(directory, schemaFileName + ".ts");
            var schemaBuilder = new StringBuilder();

            typeIDs.GenerateSchema(schemaBuilder, "SchemaIfc", 0, "EntityTypesIfc", enumFileName, "EntityTypesIfcSearch", "entity_types_search.gen");
            File.WriteAllText(schemaPath, schemaBuilder.ToString());
        }
    }
}
