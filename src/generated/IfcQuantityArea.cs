/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// 
	/// </summary>
	public partial class IfcQuantityArea : IfcPhysicalSimpleQuantity 
	{
		public IfcQuantityArea(Double areaValue,
				Boolean areaValueSpecified,
				String formula,
				IfcNamedUnit unit,
				String name,
				String description) : base(unit,
				name,
				description)
		{
			this.areaValueField = areaValue;
			this.areaValueSpecifiedField = areaValueSpecified;
			this.formulaField = formula;
		}
	}
}