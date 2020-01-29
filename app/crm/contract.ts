import { FormBase } from "./formBase";
import * as Common from "../shared/common";
import { ContractStatusCode } from "../shared/Enums";

export class Contract extends FormBase {
    OnLoad(сontext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = сontext.getFormContext();
        const statusCode = formContext.getAttribute("lmr_statuscode");
        this.statusChange(statusCode, formContext);
        statusCode.addOnChange(this.statusChange.bind(null, statusCode, formContext));
        Contract.SetStoreOnLoad(formContext);
    };
    statusChange = (statusCode: Xrm.Page.Attribute, formContext: Xrm.FormContext): void => {
        let statusCodeValue = statusCode.getValue();
        if (!Common.isEmpty(statusCodeValue)) {
            if (statusCodeValue == ContractStatusCode.signed)
                formContext.getAttribute("lmr_storestorage").setRequiredLevel("required");
            if (statusCodeValue == ContractStatusCode.unsigned)
                formContext.getAttribute("lmr_storestorage").setRequiredLevel("none");
        }
    }
    static SetStoreOnLoad(formContext: Xrm.FormContext): void {
        const storeAttribute = formContext.getAttribute('lmr_storestorage');
        if (Xrm.Page.getControl('lmr_storestorage') === null)
            return;
        const userId = Xrm.Page.context.getUserId();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        //set store only on contract creating
        if (formType == XrmEnum.FormType.Create) {
            Common.RetrieveUserStore(userId,
                (store: any) => {
                    storeAttribute.setValue([store]);
                },
                (error: any) => {
                    console.log(error);
                }
            );
        }
    }

}

window.LMR = window.LMR || {};
window.LMR.Contract = new Contract();

