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
            var wrappedTypeImport = badTypes.Contains(data.WrappedType) ? string.Empty : $"import {{ {data.WrappedType} }} from './index\'";

            AttributeData valueAttribute = new AttributeData(generator, "Value", data.WrappedType, data.Rank, data.IsCollectionType, false, false, false, false);

            var importBuilder = new StringBuilder();
            var importList = new HashSet<string>();

            string attributePropertyString = BldrsAttributeGenerator.AttributePropertyString(valueAttribute, 0, typesData, selectData, data.Rank, data.WrappedType, false, importList);

            BldrsEntityGenerator.AddDependentFunctions(importBuilder, BldrsEntityGenerator.StepDeserializationFunctions, importList, "../../step/parsing/step_deserialization_functions");
            BldrsEntityGenerator.AddDependentFunctions(importBuilder, BldrsEntityGenerator.IfcIntrinsicFunctions, importList, "../ifc_functions");

            var result =
$@"
/* This is generated code, don't alter */
{importBuilder.ToString()}{wrappedTypeImport}
import EntityTypesIfc from './entity_types_ifc.gen'
import StepEntityInternalReference from '../../step/step_entity_internal_reference'
import StepEntityBase from '../../step/step_entity_base'
import StepModelBase from '../../step/step_model_base'


///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm */
export class {data.SanitizedName()} extends StepEntityBase< EntityTypesIfc > {{    
  public get type(): EntityTypesIfc {{
    return EntityTypesIfc.{data.SanitizedName().ToUpperInvariant()}
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
    [ EntityTypesIfc.{data.SanitizedName().ToUpperInvariant()} ]

  public static readonly expectedType: EntityTypesIfc =
    EntityTypesIfc.{data.SanitizedName().ToUpperInvariant()}
}}
";            return result;

        }

    }
}
