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

            var importBuilder = new StringBuilder();
            var importList = new HashSet<string>();

            string attributePropertyString = BldrsAttributeGenerator.AttributePropertyString(valueAttribute, 0, typesData, selectData, data.Rank, data.WrappedType, false, importList);

            BldrsEntityGenerator.AddDependentFunctions(importBuilder, BldrsEntityGenerator.StepDeserializationFunctions, importList, "../../../dependencies/conway-ds/src/parsing/step/step_deserialization_functions");
            BldrsEntityGenerator.AddDependentFunctions(importBuilder, BldrsEntityGenerator.IfcIntrinsicFunctions, importList, "../../core/ifc/ifc_functions");

            var result =
$@"
/* This is generated code, don't alter */
{wrappedTypeImport}
import EntityTypesIfc from ""./entity_types_ifc.gen""
import StepEntityInternalReference from ""../../core/step_entity_internal_reference""
import StepEntityBase from ""../../core/step_entity_base""
import StepModelBase from ""../../core/step_model_base""
import {{
  stepExtractBoolean,
  stepExtractEnum,
  stepExtractString,
  stepExtractOptional,
  stepExtractBinary,
  stepExtractReference,
  stepExtractNumber,
  stepExtractInlineElemement,
  stepExtractArray
}} from '../../../dependencies/conway-ds/src/parsing/step/step_deserialization_functions'


///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm */
export class {data.Name} extends StepEntityBase< EntityTypesIfc > {{    
  public get type(): EntityTypesIfc {{
    return EntityTypesIfc.{data.Name.ToUpperInvariant()}
  }}

  {BldrsAttributeGenerator.AttributeDataString(valueAttribute, typesData)};
{attributePropertyString}

  constructor(
      localID: number,
      internalReference: StepEntityInternalReference< EntityTypesIfc >,
      model: StepModelBase< EntityTypesIfc, StepEntityBase< EntityTypesIfc > > ) {{
     super( localID, internalReference, model )
  }}

  public static readonly query =
    [ EntityTypesIfc.{data.Name.ToUpperInvariant()} ]

  public static readonly expectedType: EntityTypesIfc =
    EntityTypesIfc.{data.Name.ToUpperInvariant()}
}}
";            return result;

        }

    }
}
