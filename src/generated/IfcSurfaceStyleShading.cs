/*
This code was generated by a tool. DO NOT MODIFY this code manually, unless you really know what you are doing.
 */
using System;
				
namespace IFC4
{
	/// <summary>
	/// 
	/// </summary>
	public partial class IfcSurfaceStyleShading : IfcPresentationItem 
	{
		public IfcSurfaceStyleShading(IfcColourRgb surfaceColour,
				Double transparency,
				Boolean transparencySpecified) : base()
		{
			this.surfaceColourField = surfaceColour;
			this.transparencyField = transparency;
			this.transparencySpecifiedField = transparencySpecified;
		}
	}
}