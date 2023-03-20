﻿using Express;
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

        public static string EntityString(Entity data, Dictionary<string, SelectType> selectData, Dictionary< string, TypeData > typeData )
          {
            var importBuilder = new StringBuilder();
            var propertyBuilder = new StringBuilder();

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

                propertyBuilder.Append( BldrsAttributeGenerator.AttributePropertyString(attribute, fieldVtableIndex++, typeData, selectData, attribute.Rank, attribute.InnerType, attribute.IsGeneric) );
            }

            //        constructors = $@"
            //constructor({constructorparams(data, false)}) {{
            //    super({baseconstructorparams(data, false)}){assignments(data, false)}
            //}}";

            var result =
$@"
{importBuilder.ToString()}
import EntityTypesIfc from ""./entity_types_ifc.bldrs""
import StepEntityInternalReference from ""../../core/step_entity_internal_reference""
import StepEntityBase from ""../../core/step_entity_base""
import StepModelBase from ""../../core/step_model_base""
import {{stepExtractBoolean, stepExtractEnum, stepExtractString, stepExtractOptional, stepExtractBinary, stepExtractReference, stepExtractNumber, stepExtractInlineElemement, stepExtractArray, NVL, HIINDEX, SIZEOF}} from '../../../dependencies/conway-ds/src/parsing/step/step_deserialization_functions';
import {{IfcBaseAxis, IfcBooleanChoose, IfcBuild2Axes, IfcBuildAxes, IfcConstraintsParamBSpline, IfcConvertDirectionInto2D, IfcCorrectDimensions, IfcCorrectFillAreaStyle, IfcCorrectLocalPlacement, IfcCorrectObjectAssignment, IfcCorrectUnitAssignment, IfcCrossProduct, IfcCurveDim, IfcDeriveDimensionalExponents, IfcDimensionsForSiUnit, IfcDotProduct, IfcFirstProjAxis, IfcListToArray, IfcLoopHeadToTail, IfcMakeArrayOfArray, IfcMlsTotalThickness, IfcNormalise, IfcOrthogonalComplement, IfcPathHeadToTail, IfcSameAxis2Placement, IfcSameCartesianPoint, IfcSameDirection, IfcSameValidPrecision, IfcSameValue, IfcScalarTimesVector, IfcSecondProjAxis, IfcShapeRepresentationTypes, IfcTaperedSweptAreaProfiles, IfcTopologyRepresentationTypes, IfcUniqueDefinitionNames, IfcUniquePropertyName, IfcUniquePropertySetNames, IfcUniqueQuantityNames, IfcVectorDifference, IfcVectorSum }} from ""../../core/ifc/ifc_functions""

///**
// * http://www.buildingsmart-tech.org/ifc/ifc4/final/html/link/{data.Name.ToLower()}.htm */
export {modifiers} class {data.Name} extends {superClass} 
{{    
    public get type(): EntityTypesIfc
    {{
        return EntityTypesIfc.{data.Name.ToUpperInvariant()};
    }}

{String.Join( '\n', data.Attributes.Where(attribute => !attribute.IsInverse && !attribute.IsDerived).Select( attribute => $"    {BldrsAttributeGenerator.AttributeDataString(attribute, typeData)};" ))}
{propertyBuilder.ToString()}
    constructor(localID: number, internalReference: StepEntityInternalReference< EntityTypesIfc >, model: StepModelBase< EntityTypesIfc, StepEntityBase< EntityTypesIfc > > )
    {{
        super( localID, internalReference, model );
    }}
}}
";

            return result;
        }
    }
}
