import { RunAction } from "../shared/actionRunner";
import { OpenWebResourceInDialog } from "../shared/dialogHelper";

export class Sales {
    public static DeactivateQuote(quoteid: string, callback: Function) {
        const response = RunAction("lmr_Quote_Action", null, { quoteid: quoteid, actionid: "1" }, false);

        if (response.Result < 0) {
            if (response.Result === -2)
                Xrm.Utility.alertDialog('Расчетное предложение нельзя закрыть т.к. не все действия в отношении него закрыты', () => { });
            else
                Xrm.Utility.alertDialog(response.Error, () => { });
        }
        else {
            OpenWebResourceInDialog('lmr_/html/quote_deactivation_dialog.html', quoteid, { width: 400, height: 190 }, () => callback());
        }
    }
}