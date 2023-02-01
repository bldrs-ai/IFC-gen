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
        
        public static void GenerateEnumFiles(string directory, IEnumerable<string> types)
        {
            var typeIDs = new BlrdrsTypeIDGenerator(types);

            var enumFileName = "entity_types_ifc.bldrs.ts";
            var enumPath = Path.Combine(directory, enumFileName );
            var enumBuilder = new StringBuilder();

            typeIDs.GenerateEnum(enumBuilder, "EntityTypesIfc", 0);
                 
            File.WriteAllText(enumPath, enumBuilder.ToString());

            var searchPath = Path.Combine(directory, "entity_types_search.bldrs.ts");
            var searchBuilder = new StringBuilder();


            typeIDs.GenerateHashData(searchBuilder, "EntitTypesIfc", enumFileName, 0);

            File.WriteAllText(searchPath, searchBuilder.ToString());
        }
    }
}
