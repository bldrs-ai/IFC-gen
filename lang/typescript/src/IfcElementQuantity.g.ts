
import {BaseIfc} from "./BaseIfc"
import {IfcGloballyUniqueId} from "./IfcGloballyUniqueId.g"
import {IfcOwnerHistory} from "./IfcOwnerHistory.g"
import {IfcLabel} from "./IfcLabel.g"
import {IfcText} from "./IfcText.g"
import {IfcRelDeclares} from "./IfcRelDeclares.g"
import {IfcRelAssociates} from "./IfcRelAssociates.g"
import {IfcTypeObject} from "./IfcTypeObject.g"
import {IfcRelDefinesByTemplate} from "./IfcRelDefinesByTemplate.g"
import {IfcRelDefinesByProperties} from "./IfcRelDefinesByProperties.g"
import {IfcPhysicalQuantity} from "./IfcPhysicalQuantity.g"
import {IfcQuantitySet} from "./IfcQuantitySet.g"

/**
 * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/ifcelementquantity.htm
 */
export class IfcElementQuantity extends IfcQuantitySet {
	MethodOfMeasurement : IfcLabel // optional
	Quantities : Array<IfcPhysicalQuantity>

    constructor(globalId : IfcGloballyUniqueId, quantities : Array<IfcPhysicalQuantity>) {
        super(globalId)

		this.Quantities = quantities

    }
    getStepParameters() : string {
        var parameters = new Array<string>();
		parameters.push(this.GlobalId != null ? BaseIfc.toStepValue(this.GlobalId) : "$");
		parameters.push(this.OwnerHistory != null ? BaseIfc.toStepValue(this.OwnerHistory) : "$");
		parameters.push(this.Name != null ? BaseIfc.toStepValue(this.Name) : "$");
		parameters.push(this.Description != null ? BaseIfc.toStepValue(this.Description) : "$");
		parameters.push(this.MethodOfMeasurement != null ? BaseIfc.toStepValue(this.MethodOfMeasurement) : "$");
		parameters.push(this.Quantities != null ? BaseIfc.toStepValue(this.Quantities) : "$");

        return parameters.join();
    }
}