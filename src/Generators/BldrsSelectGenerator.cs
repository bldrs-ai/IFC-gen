using Express;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IFC4.Generators
{
    public static class BldrsSelectGenerator
    {
        public static IEnumerable<string> ExpandPossibleTypes(string baseType, Dictionary<string, SelectType> selectTypes)
        {
            if (!selectTypes.ContainsKey(baseType))
            {
                // return right away, it's not a select
                return new List<string> { baseType };
            }

            var values = selectTypes[baseType].Values;
            var result = new List<string>();

            foreach (var v in values)
            {
                result.AddRange(ExpandPossibleTypes(v, selectTypes));
            }

            return result;
        }

        public static string GenerateSelectType(SelectType data, Dictionary< string, SelectType> selectTypes)
        {
            return "";
        }
    }
}
