/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/ifcstructuralsurfacereaction.htm
	/// </summary>
	internal  partial class StructuralSurfaceReaction : StructuralReaction 
	{
		public StructuralSurfaceActivityTypeEnum PredefinedType {get;set;}

		public StructuralSurfaceReaction(StructuralSurfaceActivityTypeEnum predefinedType,
				StructuralLoad appliedLoad,
				GlobalOrLocalEnum globalOrLocal,
				ObjectPlacement objectPlacement,
				ProductRepresentation representation,
				RelDefinesByObject isDeclaredBy,
				RelDefinesByType isTypedBy,
				ObjectIsDefinedBy isDefinedBy,
				String objectType,
				ObjectDefinitionIsNestedBy isNestedBy,
				ObjectDefinitionIsDecomposedBy isDecomposedBy) : base(appliedLoad,
				globalOrLocal,
				objectPlacement,
				representation,
				isDeclaredBy,
				isTypedBy,
				isDefinedBy,
				objectType,
				isNestedBy,
				isDecomposedBy)
		{
			this.PredefinedType = predefinedType;
		}
	}
}