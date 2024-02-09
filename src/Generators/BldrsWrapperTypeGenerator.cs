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
        public static string Generate( BldrsGenerator generator, WrapperType data, Dictionary< string, TypeData > typesData, Dictionary<string, SelectType> selectData, string shortName)
        {
            var badTypes = new List<string> { "boolean", "number", "string", "[Uint8Array, number]" };
            var wrappedTypeImport = new StringBuilder();
            var shortNameLC = shortName.ToLowerInvariant();

            if ( !badTypes.Contains( data.WrappedType) )
            {
                foreach (string expandedType in BldrsSelectGenerator.ExpandPossibleTypes(data.WrappedType, selectData))
                {
                    if (typesData[expandedType.DesanitizedName()] is EnumType)
                    {
                        wrappedTypeImport.AppendLine($"import {{ {expandedType}, {expandedType}DeserializeStep }} from \"./index\"");
                    }
                    else
                    {
                        wrappedTypeImport.AppendLine($"import {{ {expandedType} }} from \"./index\"");
                    }
                }
            }

            AttributeData valueAttribute = new AttributeData(generator, "Value", data.WrappedType, data.Rank, data.IsCollectionType, false, false, false, false);

            var importBuilder = new StringBuilder();
            var importList = new HashSet<string>();

            string attributePropertyString = BldrsAttributeGenerator.AttributePropertyString(valueAttribute, 0, typesData, selectData, data.Rank, data.WrappedType, false, importList, shortName);

            BldrsEntityGenerator.AddDependentFunctions(importBuilder, BldrsEntityGenerator.StepDeserializationFunctions, importList, "../../step/parsing/step_deserialization_functions");
            BldrsEntityGenerator.AddDependentFunctions(importBuilder, BldrsEntityGenerator.IfcIntrinsicFunctions, importList, $"../{shortNameLC}_functions");

            var entityTypesName = $"EntityTypes{shortName}";
            var comment = shortName == "Ifc" ? $"http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm" : "";

            var result =
$@"
/* This is generated code, don't alter */
{importBuilder.ToString()}{wrappedTypeImport}
import {entityTypesName} from './entity_types_{shortNameLC}.gen'
import StepEntityInternalReference from '../../step/step_entity_internal_reference'
import StepEntityBase from '../../step/step_entity_base'
import StepModelBase from '../../step/step_model_base'


///**
// * {comment} */
export class {data.SanitizedName()} extends StepEntityBase< {entityTypesName} > {{    
  public get type(): {entityTypesName} {{
    return {entityTypesName}.{data.SanitizedName().ToUpperInvariant()}
  }}

  {BldrsAttributeGenerator.AttributeDataString(valueAttribute, typesData)};
{attributePropertyString}

  constructor(
      localID: number,
      internalReference: StepEntityInternalReference< {entityTypesName} >,
      model: StepModelBase< {entityTypesName}, StepEntityBase< {entityTypesName} > > ) {{
     super( localID, internalReference, model )
  }}

  public static readonly query =
    [ {entityTypesName}.{data.SanitizedName().ToUpperInvariant()} ]

  public static readonly expectedType: {entityTypesName} =
    {entityTypesName}.{data.SanitizedName().ToUpperInvariant()}
}}
";            
            return result;
        }

    }
}
