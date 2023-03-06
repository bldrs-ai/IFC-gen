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
        public static IEnumerable<string> Dependencies(Entity entity, Dictionary<string, SelectType> selectData)
        {
            //var parents = entity.ParentsAndSelf().Reverse();
            //var attrs = parents.SelectMany(p => p.Attributes);

            var result = new List<string>();

            //result.AddRange(AddRelevantTypes(attrs)); // attributes for constructor parameters for parents
            result.AddRange(AddRelevantTypes(entity.Attributes, selectData)); // atributes of self
            //result.AddRange(this.Supers.Select(s=>s.Name)); // attributes for all sub-types
            result.AddRange(entity.Subs.Select(s => s.Name)); // attributes for all super types

            var badTypes = new List<string> { "boolean", "number", "string", "Uint8Array" };
            var types = result.Distinct().Where(t => !badTypes.Contains(t) && t != entity.Name);

            return types;
        }

        private static IEnumerable<string> AddRelevantTypes(IEnumerable<AttributeData> attrs, Dictionary<string, SelectType> selectData)
        {
            var result = new List<string>();

            foreach (var a in attrs)
            {
                result.AddRange(ExpandPossibleTypes(a.type, selectData));
            }

            return result.Distinct();
        }

        private static IEnumerable<string> ExpandPossibleTypes(string baseType, Dictionary<string, SelectType> selectData)
        {
            if (!selectData.ContainsKey(baseType))
            {
                // return right away, it's not a select
                return new List<string> { baseType };
            }

            var values = selectData[baseType].Values;
            var result = new List<string>();

            foreach (var v in values)
            {
                result.AddRange(ExpandPossibleTypes(v, selectData));
            }

            return result;
        }
        public static uint FieldCount(Entity data)
        {
            return (uint)(data.Attributes.Where(attribute => !attribute.IsInverse && !attribute.IsDerived).Count());
        }

        public static uint FieldCountWithParents( Entity data )
        {
            uint ownCount = FieldCount(data);

            if ( data.Subs.Count > 0 )
            {
                ownCount += FieldCountWithParents(data.Subs[0]);
            }

            return ownCount;
        }

        public static string EntityString(Entity data, Dictionary<string, SelectType> selectData, Dictionary< string, TypeData > typeData )
          {
            var importBuilder = new StringBuilder();
            var propertyBuilder = new StringBuilder();

            foreach (var d in Dependencies(data, selectData))
            {

                if ( typeData[d] is EnumType )
                {
                    importBuilder.AppendLine($"import {d}, {{ {d}DeserializeStep }} from \"./{d}.bldrs\"");
                }
                else
                {
                    importBuilder.AppendLine($"import {d} from \"./{d}.bldrs\"");
                }
            }

            var newmod = string.Empty;

            string superClass = "StepEntityBase< EntityTypesIfc >";

            uint baseFieldCount = 0;

            if (data.Subs.Count > 0)
            {
                superClass = data.Subs[0].Name;
                baseFieldCount = FieldCountWithParents(data.Subs[0]);
            }

            string componenttypenames = $"[{string.Join(", ", data.ParentsAndSelf().Select(value => value.Name))}]";
            string modifiers = data.IsAbstract ? "abstract" : string.Empty;

            bool first = true;

            uint fieldVtableIndex = baseFieldCount;

            foreach ( var attribute in data.Attributes )
            {
                if ( !first )
                {
                    propertyBuilder.AppendLine();
                }

                first = false;

                propertyBuilder.AppendLine( BldrsAttributeGenerator.AttributePropertyString(attribute, fieldVtableIndex++, typeData, attribute.Rank, attribute.type, attribute.IsGeneric) );
            }

            //        constructors = $@"
            //constructor({constructorparams(data, false)}) {{
            //    super({baseconstructorparams(data, false)}){assignments(data, false)}
            //}}";

            var result =
$@"import EntityTypesIfc from ""./entity_types_ifc.bldrs""
import SchemaIfc from ""./schema_ifc.bldrs""
import StepEntityInternalReference from ""../../core/step_entity_internal_reference""
import StepEntityBase from ""../../core/step_entity_base""
import StepModelBase from ""../../core/step_model_base""
import StepEntitySchema from ""../../core/step_entity_schema""
import {{stepExtractBoolean, stepExtractEnum, stepExtractString, stepExtractOptional, stepExtractBinary, stepExtractReference, stepExtractNumber}} from '../../../dependencies/conway-ds/src/parsing/step/step_deserialization_functions';
{importBuilder.ToString()}

///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm */
export default {modifiers} class {data.Name} extends {superClass} 
{{    
    public get type(): EntityTypesIfc
    {{
        return EntityTypesIfc.{data.Name.ToUpperInvariant()};
    }}

    public get schema(): StepEntitySchema< EntityTypesIfc >
    {{
        return SchemaIfc;
    }}

{String.Join( '\n', data.Attributes.Where(attribute => !attribute.IsInverse && !attribute.IsDerived).Select( attribute => $"    {BldrsAttributeGenerator.AttributeDataString(attribute)};" ))}

{propertyBuilder.ToString()}

    constructor(localID: number, internalReference: StepEntityInternalReference< EntityTypesIfc >, model: StepModelBase< EntityTypesIfc, StepEntityBase< EntityTypesIfc > > )
    {{
        super( localID, internalReference, model );
    }}
}}
";
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

            return result;
        }
    }
}
