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
         //   var parents = entity.ParentsAndSelf().Reverse();
       //     var attrs = parents.SelectMany(p => p.Attributes);

            var result = new List<string>();

            if (entity.Subs.Count > 0)
            {
                result.Add( entity.Subs[ 0 ].Name ); // attributes for all super types
            }
            //result.AddRange(entity.Supers.Select(s => s.Name)); // attributes for all super types
        //    result.AddRange(AddRelevantTypes(attrs, selectData)); // attributes for constructor parameters for parents
            result.AddRange(AddRelevantTypes(entity.Attributes.Where( attr => /*(!attr.IsDerived || attr.HidesParentAttributeOfSameName) &&*/ !attr.IsInverse ), selectData)); // atributes of self
            //result.AddRange(this.Supers.Select(s=>s.Name)); // attributes for all sub-types

            var badTypes = new List<string> { "boolean", "number", "string", "[Uint8Array, number]" };
            var types = result.Distinct().Where(t => !badTypes.Contains(t) && t != entity.Name);

            return types;
        }

        private static IEnumerable<string> AddRelevantTypes(IEnumerable<AttributeData> attrs, Dictionary<string, SelectType> selectData)
        {
            var result = new List<string>();

            foreach (var a in attrs)
            {
                result.AddRange(BldrsSelectGenerator.ExpandPossibleTypes(a.type, selectData));
            }

            return result.Distinct();
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

        public static readonly string[] StepDeserializationFunctions =
        {
            "stepExtractBoolean",
            "stepExtractEnum",
            "stepExtractString",
            "stepExtractOptional",
            "stepExtractBinary",
            "stepExtractReference",
            "stepExtractNumber",
            "stepExtractInlineElemement",
            "stepExtractArray",
            "stepExtractLogical",
            "NVL",
            "HIINDEX",
            "SIZEOF",
        };

        public static readonly string[] IfcIntrinsicFunctions =
        {
            "IfcBaseAxis",
            "IfcBooleanChoose",
            "IfcBuild2Axes",
            "IfcBuildAxes",
            "IfcConstraintsParamBSpline",
            "IfcConvertDirectionInto2D",
            "IfcCorrectDimensions",
            "IfcCorrectFillAreaStyle",
            "IfcCorrectLocalPlacement",
            "IfcCorrectObjectAssignment",
            "IfcCorrectUnitAssignment",
            "IfcCrossProduct",
            "IfcCurveDim",
            "IfcDeriveDimensionalExponents",
            "IfcDimensionsForSiUnit",
            "IfcDotProduct",
            "IfcFirstProjAxis",
            "IfcListToArray",
            "IfcLoopHeadToTail",
            "IfcMakeArrayOfArray",
            "IfcMlsTotalThickness",
            "IfcNormalise",
            "IfcOrthogonalComplement",
            "IfcPathHeadToTail",
            "IfcSameAxis2Placement",
            "IfcSameCartesianPoint",
            "IfcSameDirection",
            "IfcSameValidPrecision",
            "IfcSameValue",
            "IfcScalarTimesVector",
            "IfcSecondProjAxis",
            "IfcShapeRepresentationTypes",
            "IfcTaperedSweptAreaProfiles",
            "IfcTopologyRepresentationTypes",
            "IfcUniqueDefinitionNames",
            "IfcUniquePropertyName",
            "IfcUniquePropertySetNames",
            "IfcUniqueQuantityNames",
            "IfcVectorDifference",
            "IfcVectorSum",
            "IfcPointListDim",
            "IfcGetBasisSurface"
        };

        public static void AddDependentFunctions( StringBuilder importBuilder, string[] functions, HashSet< string > importList, string fromFile )
        {
            bool firstFunction = true;

            foreach (string stepDeserializationFunction in functions)
            {
                if (importList.Contains(stepDeserializationFunction))
                {
                    if (firstFunction)
                    {
                        importBuilder.AppendLine("import {");
                        firstFunction = false;
                    }

                    importBuilder.AppendLine($"  {stepDeserializationFunction},");
                }
            }

            if (!firstFunction)
            {
                importBuilder.AppendLine($"}} from '{fromFile}'");
            }
        }

        public static string EntityString(Entity data, Dictionary<string, SelectType> selectData, Dictionary< string, TypeData > typeData )
          {
            var importBuilder = new StringBuilder();
            var propertyBuilder = new StringBuilder();
            var importList = new HashSet<string>();

            foreach (var d in Dependencies(data, selectData))
            {

                if ( typeData[d] is EnumType )
                {
                    importBuilder.AppendLine($"import {{ {d}, {d}DeserializeStep }} from \"./index\"");
                }
                else
                {
                    importBuilder.AppendLine($"import {{ {d} }} from \"./index\"");
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

                propertyBuilder.Append( BldrsAttributeGenerator.AttributePropertyString(attribute, fieldVtableIndex++, typeData, selectData, attribute.Rank, attribute.InnerType, attribute.IsGeneric, importList) );
            }

            //        constructors = $@"
            //constructor({constructorparams(data, false)}) {{
            //    super({baseconstructorparams(data, false)}){assignments(data, false)}
            //}}";

            AddDependentFunctions(importBuilder, StepDeserializationFunctions, importList, "../../step/parsing/step_deserialization_functions");
            AddDependentFunctions(importBuilder, IfcIntrinsicFunctions, importList, "../ifc_functions");

            var result =
$@"
{importBuilder.ToString()}
/* This is generated code, don't modify */
import EntityTypesIfc from './entity_types_ifc.gen'
import StepEntityInternalReference from '../../step/step_entity_internal_reference'
import StepEntityBase from '../../step/step_entity_base'
import StepModelBase from '../../step/step_model_base'

///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm */
export {modifiers} class {data.Name} extends {superClass} {{
  public get type(): EntityTypesIfc {{
    return EntityTypesIfc.{data.Name.ToUpperInvariant()}
  }}
{String.Join( '\n', data.Attributes.Where(attribute => !attribute.IsInverse && !attribute.IsDerived).Select( attribute => $"  {BldrsAttributeGenerator.AttributeDataString(attribute, typeData)}" ))}
{propertyBuilder.ToString()}
  constructor(
    localID: number,
    internalReference: StepEntityInternalReference< EntityTypesIfc >,
    model: StepModelBase< EntityTypesIfc, StepEntityBase< EntityTypesIfc > > ) {{
    super( localID, internalReference, model )
  }}

  public static readonly query = 
    [ { string.Join( ", ", data.ChildrenAndSelf().Where( childEntity => !childEntity.IsAbstract ).Select( childEntity => $"EntityTypesIfc.{childEntity.Name.ToUpperInvariant()}" ) ) } ]

  public static readonly expectedType: EntityTypesIfc =
    EntityTypesIfc.{data.Name.ToUpperInvariant()}
}}
";

            return result;
        }
    }
}
