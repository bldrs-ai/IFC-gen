using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsDerivedFunctionTranslator
    {
        private enum DerivedDotTransformState
        {
            EATING_WHITESPACE = 0,
            STARTING_RUN = 1,
            REACHED_DOT = 2
        }

        public static readonly string[] IntrinsicFunctions =
        {
            "NVL",
            "HIINDEX",
            "SIZEOF",
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

        public static string TransformDerivedFunctionToTS( string input, HashSet< string > importFunctions )
        {
            if ( input.Contains( "QUERY") || input.Contains( "<*" ) || input.Contains( "SELF\\" ) || input.Contains("NSegments" ) || input.Contains( "||" ) )
            {
                return "";
            }

            foreach ( string intrinsicFunction in IntrinsicFunctions )
            {
                if ( input.Contains( intrinsicFunction) )
                {
                    importFunctions.Add(intrinsicFunction);
                }
            }

            input = input.Replace("[", ".[");
            input = input.Replace("]", " - 1]");
            input = input.Replace("<>", "!==");

            StringBuilder result = new StringBuilder();

            int index = 0;

            DerivedDotTransformState state = DerivedDotTransformState.EATING_WHITESPACE;

            int runStart = 0;

            while (index < input.Length)
            {
                char c = input[index];

                switch (state)
                {
                case DerivedDotTransformState.EATING_WHITESPACE:

                    if (c != ' ' && c != '\t' && c != '\n' && c != '\r')
                    {
                        runStart = index;
                        state = DerivedDotTransformState.STARTING_RUN;
                    }
                    else
                    {
                        ++index;
                    }
                    break;

                case DerivedDotTransformState.STARTING_RUN:

                    if (c == '.')
                    {
                        string run = input.Substring(runStart, index - runStart);

                        if (run.Trim() == "SELF")
                        {
                            run = "this";
                        }
                        else if (run.Trim().Length > 0 && char.IsLetter(run.Trim()[0]))
                        {
                            run = "this?." + run;
                        }

                        result.Append(run);

                        if ( !Double.TryParse( run, out double _ ) )
                        {
                            result.Append("?.");
                        }
                        else
                        {
                             result.Append(".");
                        }

                        ++index;

                        state = DerivedDotTransformState.REACHED_DOT;
                    }
                    else if (c == ',' || c == ')' || c == ';')
                    {
                        string run = input.Substring(runStart, index - runStart);

                        if (run.TrimEnd() == "SELF")
                        {
                            run = "this";
                        }
                        else if ( run.Trim().Length > 0 && char.IsLetter( run.Trim()[ 0 ] ) )
                        {
                            run = "this?." + run.TrimEnd();
                        }

                        result.Append(run);
                        result.Append(c);

                        ++index;

                        state = DerivedDotTransformState.EATING_WHITESPACE;
                    }
                    else if (c == '(')
                    {
                        string run = input.Substring(runStart, index - runStart);

                        result.Append(run);
                        result.Append(c);

                        ++index;

                        state = DerivedDotTransformState.EATING_WHITESPACE;
                    }
                    else
                    {
                        ++index;
                    }
                    break;

                case DerivedDotTransformState.REACHED_DOT:

                    if (c == ',' || c == ')' || c == '(' || c == ';')
                    {
                        state = DerivedDotTransformState.EATING_WHITESPACE;
                    }

                    result.Append(c);
                    ++index;

                    break;
                }
            }

            return result.ToString();
        }
    }
}
