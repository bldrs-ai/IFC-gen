/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// 
	/// </summary>
	public partial class IfcBlock : IfcCsgPrimitive3D 
	{
		public IfcBlock(Double xLength,
				Boolean xLengthSpecified,
				Double yLength,
				Boolean yLengthSpecified,
				Double zLength,
				Boolean zLengthSpecified,
				IfcAxis2Placement3D position,
				IfcStyledItem styledByItem) : base(position,
				styledByItem)
		{
			this.xLengthField = xLength;
			this.xLengthSpecifiedField = xLengthSpecified;
			this.yLengthField = yLength;
			this.yLengthSpecifiedField = yLengthSpecified;
			this.zLengthField = zLength;
			this.zLengthSpecifiedField = zLengthSpecified;
		}
	}
}