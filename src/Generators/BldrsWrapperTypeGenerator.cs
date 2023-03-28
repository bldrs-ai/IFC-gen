using Express;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsWrapperTypeGenerator
    {
        public static string Generate( BldrsGenerator generator, WrapperType data, Dictionary< string, TypeData > typesData, Dictionary<string, SelectType> selectData)
        {
            var badTypes = new List<string> { "boolean", "number", "string", "[Uint8Array, number]" };
            var wrappedTypeImport = badTypes.Contains(data.WrappedType) ? string.Empty : $"import {{ {data.WrappedType} }} from \"./index\"";

            AttributeData valueAttribute = new AttributeData(generator, "Value", data.WrappedType, data.Rank, data.IsCollectionType, false, false, false, false);

            var result =
$@"
{wrappedTypeImport}
import EntityTypesIfc from ""./entity_types_ifc.bldrs""
import StepEntityInternalReference from ""../../core/step_entity_internal_reference""
import StepEntityBase from ""../../core/step_entity_base""
import StepModelBase from ""../../core/step_model_base""
import {{stepExtractBoolean, stepExtractEnum, stepExtractString, stepExtractOptional, stepExtractBinary, stepExtractReference, stepExtractNumber, stepExtractInlineElemement, stepExtractArray}} from '../../../dependencies/conway-ds/src/parsing/step/step_deserialization_functions';


///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm */
export class {data.Name} extends StepEntityBase< EntityTypesIfc >
{{    
    public get type(): EntityTypesIfc
    {{
        return EntityTypesIfc.{data.Name.ToUpperInvariant()};
    }}

    {BldrsAttributeGenerator.AttributeDataString(valueAttribute, typesData)};
{BldrsAttributeGenerator.AttributePropertyString( valueAttribute, 0, typesData, selectData, data.Rank, data.WrappedType, false )}

    constructor(localID: number, internalReference: StepEntityInternalReference< EntityTypesIfc >, model: StepModelBase< EntityTypesIfc, StepEntityBase< EntityTypesIfc > > )
    {{
        super( localID, internalReference, model );
    }}
}}
";            return result;

        }

    }
}
