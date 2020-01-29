import { FormBase } from "./formBase";
import { RunAction } from "../shared/actionRunner"; 

interface MeasureTaskResponse {
    value: any;
}

export class MeasureTask extends FormBase {

    OnLoad(eventContext: Xrm.Events.EventContext): void {
        
    }

    SendToServicePlatform(): void {

        var response = RunAction<MeasureTaskResponse>('lmr_MeasureTaskSendAction', null, {
            id: Xrm.Page.data.entity.getId(),
            userid: Xrm.Page.context.getUserId()
        }, false);

        //debugger;
        if (response.Result === 0) {
            MeasureTask.SetInfoMessage('Заявка отправлена в Сервисную Платформу');
            Xrm.Page.data.refresh(false); 
        }
        else {
            MeasureTask.SetErrorMessage(response.Error);
        }
    }

    private static SetErrorMessage(message: string): void {
        Xrm.Page.ui.setFormNotification(message, 'ERROR', 'pleasewait');
        window.setTimeout(MeasureTask.SetTimeout, 10000);
    }

    private static SetInfoMessage(message: string): void {
        Xrm.Page.ui.setFormNotification(message, 'INFO', 'pleasewait');
        window.setTimeout(MeasureTask.SetTimeout, 10000);
    }

    private static SetTimeout(): void {
        Xrm.Page.ui.clearFormNotification('pleasewait');
    }
}



window.LMR = window.LMR || {};
window.LMR.MeasureTask = new MeasureTask();