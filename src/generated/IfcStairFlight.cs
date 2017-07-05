/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// 
	/// </summary>
	public partial class IfcStairFlight : IfcBuildingElement 
	{
		public IfcStairFlight(Int64 numberOfRisers,
				Boolean numberOfRisersSpecified,
				Int64 numberOfTreads,
				Boolean numberOfTreadsSpecified,
				Double riserHeight,
				Boolean riserHeightSpecified,
				Double treadLength,
				Boolean treadLengthSpecified,
				IfcStairFlightTypeEnum predefinedType,
				Boolean predefinedTypeSpecified,
				IfcRelProjectsElement hasProjections,
				IfcRelVoidsElement hasOpenings,
				String tag,
				IfcObjectPlacement objectPlacement,
				IfcProductRepresentation representation,
				IfcRelDefinesByObject isDeclaredBy,
				IfcRelDefinesByType isTypedBy,
				IfcObjectIsDefinedBy isDefinedBy,
				String objectType,
				IfcObjectDefinitionIsNestedBy isNestedBy,
				IfcObjectDefinitionIsDecomposedBy isDecomposedBy) : base(hasProjections,
				hasOpenings,
				tag,
				objectPlacement,
				representation,
				isDeclaredBy,
				isTypedBy,
				isDefinedBy,
				objectType,
				isNestedBy,
				isDecomposedBy)
		{
			this.numberOfRisersField = numberOfRisers;
			this.numberOfRisersSpecifiedField = numberOfRisersSpecified;
			this.numberOfTreadsField = numberOfTreads;
			this.numberOfTreadsSpecifiedField = numberOfTreadsSpecified;
			this.riserHeightField = riserHeight;
			this.riserHeightSpecifiedField = riserHeightSpecified;
			this.treadLengthField = treadLength;
			this.treadLengthSpecifiedField = treadLengthSpecified;
			this.predefinedTypeField = predefinedType;
			this.predefinedTypeSpecifiedField = predefinedTypeSpecified;
		}
	}
}