/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// 
	/// </summary>
	public partial class IfcConstructionResourceType : IfcTypeResource 
	{
		public IfcConstructionResourceType(IfcConstructionResourceTypeBaseCosts baseCosts,
				IfcPhysicalQuantity baseQuantity,
				String identification,
				String longDescription,
				String resourceType,
				IfcTypeObjectHasPropertySets hasPropertySets,
				String applicableOccurrence,
				IfcObjectDefinitionIsNestedBy isNestedBy,
				IfcObjectDefinitionIsDecomposedBy isDecomposedBy) : base(identification,
				longDescription,
				resourceType,
				hasPropertySets,
				applicableOccurrence,
				isNestedBy,
				isDecomposedBy)
		{
			this.baseCostsField = baseCosts;
			this.baseQuantityField = baseQuantity;
		}
	}
}