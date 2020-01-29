import webapi from 'xrm-webapi-client'
import { FormBase } from './formBase';
import { OpenWebResourceInDialog } from '../shared/dialogHelper'
import Build from 'odata-query'

export class SystemUser extends FormBase {  
    public OnLoad(context: Xrm.Events.EventContext): void {
        
    }
    OnPyxisButtonClick() {
        
        OpenWebResourceInDialog('lmr_/html/userSettings_form.html', Xrm.Page.data.entity.getId(), { width: 500, height: 620 }, (result: any[]) => {
            
        })
    }
    PyxisButtonEnableRule() {
        if (Xrm.Page.data.entity.getId()) {
            return true;
        }
        else { return false;}
    }
    OnImportLoginsButtonClick() {
        OpenWebResourceInDialog('lmr_/html/import_logins.html', null, { width: 500, height: 250 },
            (result: any[]) => { }
        )
    }

    OnImportLoginsButtonEnableRule() {
        var RolesForImportLogins: string = "";
        var configResponse: any = webapi.Retrieve({
            entityName: "lmr_config",
            queryParams: Build({
                filter: { and: [{ "lmr_name": { eq: "RolesForImportLogins" } }] }
            }),
            async: false
        })
        if (configResponse) {
            if (configResponse.value.length <= 0) {
                alert("Не найден парметр конфигурации RolesForImportLogins");
            }
            RolesForImportLogins = configResponse.value[0].lmr_value;
        }
        if (RolesForImportLogins == "") {
            console.log("GMCS error: пустое значение RolesForImportLogins");
        }

        var rolesForImport = RolesForImportLogins.split(',');

        var userRoles = Xrm.Page.context.getUserRoles()

        var el: boolean = rolesForImport.some(function (element1: any, index: number) {
            return userRoles.some(function (element2: any, index: number) {
                return element1.toLowerCase() == element2.toLowerCase();
            })
        })
        return el;      
    }
    
}

window.LMR = window.LMR || {};
window.LMR.SystemUser = new SystemUser();