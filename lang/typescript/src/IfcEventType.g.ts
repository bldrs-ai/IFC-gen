
import {BaseIfc} from "./BaseIfc"
import {IfcGloballyUniqueId} from "./IfcGloballyUniqueId.g"
import {IfcOwnerHistory} from "./IfcOwnerHistory.g"
import {IfcLabel} from "./IfcLabel.g"
import {IfcText} from "./IfcText.g"
import {IfcRelAssigns} from "./IfcRelAssigns.g"
import {IfcRelNests} from "./IfcRelNests.g"
import {IfcRelDeclares} from "./IfcRelDeclares.g"
import {IfcRelAggregates} from "./IfcRelAggregates.g"
import {IfcRelAssociates} from "./IfcRelAssociates.g"
import {IfcIdentifier} from "./IfcIdentifier.g"
import {IfcPropertySetDefinition} from "./IfcPropertySetDefinition.g"
import {IfcRelDefinesByType} from "./IfcRelDefinesByType.g"
import {IfcRelAssignsToProcess} from "./IfcRelAssignsToProcess.g"
import {IfcEventTypeEnum} from "./IfcEventTypeEnum.g"
import {IfcEventTriggerTypeEnum} from "./IfcEventTriggerTypeEnum.g"
import {IfcTypeProcess} from "./IfcTypeProcess.g"

/**
 * http://www.buildingsmart-tech.org/ifc/IFC4/final/html/link/ifceventtype.htm
 */
export class IfcEventType extends IfcTypeProcess {
	PredefinedType : IfcEventTypeEnum
	EventTriggerType : IfcEventTriggerTypeEnum
	UserDefinedEventTriggerType : IfcLabel // optional

    constructor(globalId : IfcGloballyUniqueId, predefinedType : IfcEventTypeEnum, eventTriggerType : IfcEventTriggerTypeEnum) {
        super(globalId)

		this.PredefinedType = predefinedType
		this.EventTriggerType = eventTriggerType

    }
    getStepParameters() : string {
        var parameters = new Array<string>();
		parameters.push(this.GlobalId != null ? BaseIfc.toStepValue(this.GlobalId) : "$");
		parameters.push(this.OwnerHistory != null ? BaseIfc.toStepValue(this.OwnerHistory) : "$");
		parameters.push(this.Name != null ? BaseIfc.toStepValue(this.Name) : "$");
		parameters.push(this.Description != null ? BaseIfc.toStepValue(this.Description) : "$");
		parameters.push(this.ApplicableOccurrence != null ? BaseIfc.toStepValue(this.ApplicableOccurrence) : "$");
		parameters.push(this.HasPropertySets != null ? BaseIfc.toStepValue(this.HasPropertySets) : "$");
		parameters.push(this.Identification != null ? BaseIfc.toStepValue(this.Identification) : "$");
		parameters.push(this.LongDescription != null ? BaseIfc.toStepValue(this.LongDescription) : "$");
		parameters.push(this.ProcessType != null ? BaseIfc.toStepValue(this.ProcessType) : "$");
		parameters.push(BaseIfc.toStepValue(this.PredefinedType));
		parameters.push(BaseIfc.toStepValue(this.EventTriggerType));
		parameters.push(this.UserDefinedEventTriggerType != null ? BaseIfc.toStepValue(this.UserDefinedEventTriggerType) : "$");

        return parameters.join();
    }
}