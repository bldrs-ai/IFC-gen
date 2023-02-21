using Express;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsEntityGenerator
    {
//        public static IEnumerable<string> Dependencies(Entity entity)
//        {
//            //var parents = entity.ParentsAndSelf().Reverse();
//            //var attrs = parents.SelectMany(p => p.Attributes);

//            var result = new List<string>();

//            //result.AddRange(AddRelevantTypes(attrs)); // attributes for constructor parameters for parents
//            result.AddRange(AddRelevantTypes(entity.Attributes)); // atributes of self
//            //result.AddRange(this.Supers.Select(s=>s.Name)); // attributes for all sub-types
//            //result.AddRange(entity.Subs.Select(s => s.Name)); // attributes for all super types

//            var badTypes = new List<string> { "boolean", "number", "string", "Uint8Array" };
//            var types = result.Distinct().Where(t => !badTypes.Contains(t) && t != entity.Name);

//            return types;
//        }

//        private static IEnumerable<string> AddRelevantTypes(IEnumerable<AttributeData> attrs)
//        {
//            var result = new List<string>();

//            foreach (var a in attrs)
//            {
//                result.AddRange(ExpandPossibleTypes(a.type));
//            }

//            return result.Distinct();
//        }

//        private static  IEnumerable<string> ExpandPossibleTypes(string baseType)
//        {
//            if (!SelectData.ContainsKey(baseType))
//            {
//                // return right away, it's not a select
//                return new List<string> { baseType };
//            }

//            var values = SelectData[baseType].Values;
//            var result = new List<string>();

//            foreach (var v in values)
//            {
//                result.AddRange(ExpandPossibleTypes(v));
//            }

//            return result;
//        }

          public static string EntityString(Entity data)
          {
//            var importBuilder = new StringBuilder();

//            foreach (var d in Dependencies(data))
//            {
//                importBuilder.AppendLine($"import {d} from \"./{d}.bldrs\"");
//            }

//            var newmod = string.Empty;

//            string superclass = "entitybase< schemaspecificationifc >";

//            if (data.subs.count > 0)
//            {
//                superclass = data.subs[0].name;
//            }

//            string componenttypenames = $"[{string.join(", ", data.parentsandself().select(value => value.name))}]";
//            string modifiers          = data.isabstract ? "abstract" : string.empty;


//            //        constructors = $@"
//            //constructor({constructorparams(data, false)}) {{
//            //    super({baseconstructorparams(data, false)}){assignments(data, false)}
//            //}}";

//            var result =
//$@"
//import EntityTypesIfc from ""./entity_types_search.bldrs""
//import component from ""../../core/component""
//import componentspecification from ""../../core/component_specification""
//import attributespecification from ""../../core/attribute_specification""
//import schemaspecificationifc from ""./schema_ifc.bldrs""
//import {{ ifcschema }} from ""./schema_ifc.bldrs""
//{importBuilder.tostring()}

///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.name.tolower()}.htm
// */
//export default {modifiers} class {data.name} extends {superclass} 
//{{    
//    public readonly specification: {data.name}specification = {data.name}specification.instance;

//{string.join("\n    ", data.attributes.where(value => value.tostring() != string.empty).select(value => value.tostring())) }

//    constructor( buffer: snapshotbuffer< t >, dirtyprovider?: ( entity: entity< t > ) => void )
//    constructor( fileidprovider: () => number, dirtyprovider?: ( entity: entity< t > ) => void )
//    constructor( bufferorfileidprovider: snapshotbuffer< t > | ( () => number ), private readonly dirtyprovider_?: ( entity: entity< t > ) => void ) 
//    {{
//        super( bufferorfileidprovider, dirtyprovider_ );
//    }}

//}}

//export class {data.name}specification implements componentspecification
//{{
//    public readonly name: string = '{data.name}';

//    public readonly required: readonlyarray< string > = [ {string.join(", ", data.parentsandself().select((supervalue) => $"'{supervalue.name}'"))} ];

//    public readonly isabstract: boolean = {(data.isabstract ? "true" : "false")};

//    public readonly attributes: readonlyarray< attributespecification > = 
//    [{string.join(", ", data.attributes.where(attr => !attr.isinverse && !attr.isderived).select(attr => $"\n\t\t{{\n\t\t\tname: '{attr.name}',\n\t\t\tiscollection: {(attr.iscollection ? "true" : "false")},\n\t\t\trank: {attr.rank},\n\t\t\tbasetype: '{attr.type}',\n\t\t\toptional: {(attr.isoptional ? "true" : "false")}\n\t\t}}"))}
//    ];

//    public readonly schema: ifcschema = 'ifc';

//    public static readonly instance: {data.name}specification = new {data.name}specification();
//}}
//";
//            return result;

            return "";
        }
    }
}
