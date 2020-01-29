
import webapi from 'xrm-webapi-client'
import { RunAction } from '../shared/actionRunner'
import { FormBase } from './formBase';
import { OpenWebResourceInDialog } from '../shared/dialogHelper'

export class News extends FormBase {  
    public ViewNewsButtonIsClicked: boolean = false;
    public OnLoad(context: Xrm.Events.EventContext): void {      
    }

    public OnSave(context: Xrm.Events.EventContext): void {
        console.log('OnSave');
        //debugger;

        if (Xrm.Page.getControl('WebResource_quill_shortdescription') != null && Xrm.Page.getControl('WebResource_quill_shortdescription') != undefined) {

            var quill_shortdescription = Xrm.Page.getControl<Xrm.Controls.FramedControl>('WebResource_quill_shortdescription');
            if (quill_shortdescription != null && quill_shortdescription != undefined) {
                var contentWindow = quill_shortdescription.getObject().contentWindow;

                if (contentWindow != null && contentWindow != undefined) {
                    Xrm.Page.getAttribute('lmr_shortdescription').setValue(contentWindow.document.getElementsByClassName('ql-editor')[0].innerHTML);
                }
            }           
        }

        if (Xrm.Page.getControl('WebResource_quill_description') != null && Xrm.Page.getControl('WebResource_quill_description') != undefined) {

            var quill_description = Xrm.Page.getControl<Xrm.Controls.FramedControl>('WebResource_quill_description');
            if (quill_description != null && quill_description != undefined) {
                var contentWindow = quill_description.getObject().contentWindow;

                if (contentWindow != null && contentWindow != undefined) {
                    Xrm.Page.getAttribute('lmr_description').setValue(contentWindow.document.getElementsByClassName('ql-editor')[0].innerHTML);
                }
            }
        }                         
    }
    OnViewNewsButtonClick() {   
        if (window.LMR.News.ViewNewsButtonIsClicked) {
            alert("Просмотр новости для вашего пользователя уже создан");
            return;
        }
        else {
            webapi.Create({
                entityName: "lmr_newsview",
                overriddenSetName: "",
                entity: {
                    lmr_name: Xrm.Page.context.getUserName() + " - " + new Date().toISOString(),
                    "lmr_newsid@odata.bind": `/lmr_newses(${Xrm.Page.data.entity.getId().replace(/[{}]/g, '')})`,
                    "lmr_viewerid@odata.bind": `/systemusers(${Xrm.Page.context.getUserId().replace(/[{}]/g, '')})`
                }
            });
            window.LMR.News.ViewNewsButtonIsClicked = true;
        }
    }
    OnViewNewsGridButtonClick(ids: any, b: any) {
        //debugger;

        webapi.Create({
            entityName: "lmr_newsview",
            overriddenSetName: "",
            entity: {
                lmr_name: Xrm.Page.context.getUserName() + " - " + new Date().toISOString(),
                "lmr_newsid@odata.bind": `/lmr_newses(${ids[0].replace(/[{}]/g, '')})`,
                "lmr_viewerid@odata.bind": `/systemusers(${Xrm.Page.context.getUserId().replace(/[{}]/g, '')})`
            }
        });
    }
    ViewNewsGridButtonEnableRule() {
        return true;
    }
    ViewNewsButtonEnableRule() {
        return true;
    }
}

window.LMR = window.LMR || {};
window.LMR.News = new News();