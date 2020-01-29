import { FormBase } from "./formBase";
import { Sales } from '../shared/sales';

export class Quote extends FormBase {
    OnLoad(context: Xrm.Events.EventContext): void {
        Quote.ProjectLinksReferenceAction(context);
    };

    private static ProjectLinksReferenceAction(context: Xrm.Events.EventContext): void {
        if (Xrm.Page.data.entity.getId() != '') {            

            const formContext: Xrm.FormContext = context.getFormContext();

            if (Xrm.Page.getControl('lmr_linktofileproject') != null) {

                if (formContext.getAttribute('lmr_linktofileproject') != null &&
                    formContext.getAttribute('lmr_linktofileproject').getValue() != null) {
                    let linkToFileproject: Xrm.Controls.StringControl = Xrm.Page.getControl('lmr_linktofileproject');
                    linkToFileproject.setVisible(true);
                }
            }

            if (Xrm.Page.getControl('lmr_linkofproject') != null) {

                if (formContext.getAttribute('lmr_linkofproject') != null &&
                    formContext.getAttribute('lmr_linkofproject').getValue() != null) {
                    let linkOfProject: Xrm.Controls.StringControl = Xrm.Page.getControl('lmr_linkofproject');
                    linkOfProject.setVisible(true);
                }
            }
        }
    }

    public DeactivateQuoteAction(): void {
        const quoteid: string = Xrm.Page.data.entity.getId();

        Sales.DeactivateQuote(quoteid, () => Xrm.Page.data.refresh(true));
    }
}

window.LMR = window.LMR || {};
window.LMR.Quote = new Quote();