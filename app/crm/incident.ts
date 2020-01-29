
import webapi from 'xrm-webapi-client'
import { RunAction } from '../shared/actionRunner'
import { FormBase } from './formBase';
import { OpenWebResourceInDialog } from '../shared/dialogHelper'
import * as fileManager from '../components/fileStore/fileManager'
import Build from 'odata-query';
import * as Common from '../shared/common';
import { CaseOriginCode, IncidentStatusCode} from "../shared/Enums";

interface DownloadFileResult {
    fileName: string;
    mimeType: string;
    documentBody: string;
}

export class Incident extends FormBase {
    public OnLoad(context: Xrm.Events.EventContext): void {
        Incident.SetStoreOnLoad();
        Incident.LeroyMerlinMarketplaceRoleAction(context);
    }

    public OnSave(eventContext: Xrm.Events.SaveEventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();
    }

    private static LeroyMerlinMarketplaceRoleAction(context: Xrm.Events.EventContext): void {

        const formContext: Xrm.FormContext = context.getFormContext();

        if (Xrm.Page.data.entity.getId() == '') {

            var roleConfig: any = webapi.Retrieve({
                async: false,
                entityName: "lmr_config",
                queryParams: "?$select=lmr_value&$filter=lmr_name eq 'Marketplace_IncidentRole'&$top=1"
            });
            if (roleConfig.value.length > 0) {
                if (Incident.CheckUserRoleExistence(roleConfig.value[0].lmr_value)) {

                    var contactConfig: any = webapi.Retrieve({
                        async: false,
                        entityName: "lmr_config",
                        queryParams: "?$select=lmr_value&$filter=lmr_name eq 'UnknownContactId'&$top=1"
                    });
                    if (contactConfig.value.length > 0) {

                        var lookupContactRecord = new Array();
                        lookupContactRecord[0] = new Object();
                        lookupContactRecord[0].id = contactConfig.value[0].lmr_value;
                        lookupContactRecord[0].name = 'Неизвестный, Клиент';
                        lookupContactRecord[0].entityType = 'contact';

                        Xrm.Page.getAttribute('customerid').setValue(lookupContactRecord);
                        formContext.getAttribute('caseorigincode').setValue(CaseOriginCode.merchant);
                    }
                    else {
                        alert("Отсутствует параметр конфигурации: UnknownContactId");
                    }
                }
            }
            else {
                alert("Отсутствует параметр конфигурации: Marketplace_IncidentRole");
            }
        }
    }

    private static CheckUserRoleExistence(userRoleName: string): boolean {
        var roleIsExists = false;
        var filter = " roleid eq " + Xrm.Page.context.getUserRoles().join(' or roleid eq ')
        var userRolesNames = webapi.Retrieve({
            async: false,
            entityName: "role",
            queryParams: "?$select=name&$filter=" + filter
        });

        if (userRolesNames.value.length > 0) {
            roleIsExists = userRolesNames.value
                .some(function (role: any, index: any) {
                    return role.name == userRoleName;
                });
        }

        return roleIsExists;
    }

    ResolveButtonEnableRule(): boolean {

        var WorkgroupsIdForCloseIncident: string = "";
        var configResponse: any = webapi.Retrieve({
            entityName: "lmr_config",
            queryParams: Build({
                filter: { and: [{ "lmr_name": { eq: "WorkgroupsIdForCloseIncident" } }] }
            }),
            async: false
        })
        if (configResponse) {
            if (configResponse.value.length <= 0) {
                alert("Отсутствует параметр конфигурации WorkgroupsIdForCloseIncident");
            }
            WorkgroupsIdForCloseIncident = configResponse.value[0].lmr_value;
        }
        if (WorkgroupsIdForCloseIncident == "") {
            console.log("GMCS error: пустое значение WorkgroupsIdForCloseIncident");
        }

        var workGroups = WorkgroupsIdForCloseIncident.split(',');

        var userId: string = Xrm.Page.context.getUserId();
        userId = userId.replace('{', '').replace('}', '');
        var response: any = webapi.Retrieve({
            entityName: "systemuser",
            queryParams: "(" + userId + ")?$expand=teammembership_association($select=name)",
            async: false
        })
        var el: boolean = response.teammembership_association.some(function (element1: any, index: number) {
            return workGroups.some(function (element2: any, index: number) {
                return element1['teamid'].toLowerCase() == element2.toLowerCase();
            })

        })
        return el;
    }


    static SetStoreOnLoad(): void {
        if (Xrm.Page.data.entity.getId() == '') {//на создании обращени
            const storeAttribute = Xrm.Page.getAttribute('lmr_storeid');
            if (Xrm.Page.getControl('lmr_storeid') === null)
                return;

            const userId = Xrm.Page.context.getUserId();

            Common.RetrieveUserStore(userId,
                (store: any) => {
                    storeAttribute.setValue([store]);
                },
                (error: any) => {

                    alert("У текущего пользователя не заполнен магазин, обратитесь к администратору");
                    console.log(error);
                }
            );
        }
    }
    OnPassToClaimButtonClick(): void {
        RunAction('lmr_PassIncidentToClaim', {
            entityName: Xrm.Page.data.entity.getEntityName(),
            id: Xrm.Page.data.entity.getId()
        })
            .then(() => {
                Xrm.Page.data.refresh(false);
                OpenWebResourceInDialog('lmr_/html/pass_to_claim_success_alert.html', null, { width: 620, height: 150 });
            })
            .catch((e: any) => {
                alert("Произошла ошибка, обратитесь к администратору");
                console.error(e);
            });
    }

    OnAnswerTemplateChanged(): void {
        const decisionAttribute = Xrm.Page.getAttribute('lmr_decision');

        var answerTemplateValue = Xrm.Page.getAttribute('lmr_answertemplateid').getValue();
        if (answerTemplateValue == null) {
            decisionAttribute.setValue(null);
            return;
        }
        webapi.Retrieve({
            entityName: answerTemplateValue[0].entityType,
            entityId: answerTemplateValue[0].id,
            queryParams: '?$select=lmr_solution'
        }).then((result: any) => decisionAttribute.setValue(result['lmr_solution']))
            .catch(console.error);
    }

    OnAssignButtonClick() {
        OpenWebResourceInDialog('lmr_/html/team_tree.html', null, { width: 500, height: 600 }, teamId => Incident.AssignThis("team", teamId));
    }
    OnClassifierButtonClick() {
        OpenWebResourceInDialog('lmr_/html/classifier.html', null, { width: 500, height: 600 }, (result: any[]) => {
            for (let i = 1; i <= 5; i++) {
                Xrm.Page.getAttribute(`lmr_classifierid_level${i}`).setValue(null)
            }
            result.forEach((value, i) => Xrm.Page.getAttribute(`lmr_classifierid_level${i + 1}`).setValue([{ entityType: 'lmr_classifier', id: value.id, name: value.name }]));
        })
    }
    OnAssignToMeButtonClick() {
        Incident.AssignThis("systemuser");
    }
    private static AssignThis(type: "systemuser" | "team", userOrTeamId?: string) {
        userOrTeamId = userOrTeamId || Xrm.Page.context.getUserId();
        RunAction('lmr_AssignRecord', null, {
            Target: {
                "@odata.type": "Microsoft.Dynamics.CRM.incident",
                incidentid: Xrm.Page.data.entity.getId()
            },
            Assignee: {
                "@odata.type": `Microsoft.Dynamics.CRM.${type}`,
                [`${type}id`]: userOrTeamId
            }
        }).then(() => Incident.AfterAssign(type))
            .catch ((e: any) => console.error(e))
    }

    private static AfterAssign(logicalType: string): void {     

        if (logicalType == 'systemuser') {

            let statuscode: number = Xrm.Page.getAttribute('statuscode').getValue();

            if (statuscode == IncidentStatusCode.New) {
                webapi.Update({
                    entityId: Xrm.Page.data.entity.getId(),
                    entityName: 'incident',
                    entity: {
                        'statuscode': IncidentStatusCode.InWork
                    },
                    async: false
                });                               
            }
        }
        if (logicalType == 'team') {          

            webapi.Update({
                entityId: Xrm.Page.data.entity.getId(),
                entityName: 'incident',
                entity: {
                    'statuscode': IncidentStatusCode.Store
                },
                async: false
            });           
        }        
        Xrm.Page.data.refresh(false);
    }
    CreatePDFAnswer() {
        try {
            const result = RunAction<DownloadFileResult>
                ('lmr_DownloadPDFAnswer', null, { IncidentId: Xrm.Page.data.entity.getId() }, false);
            fileManager.SaveFile(result.fileName, result.mimeType, result.documentBody);
        } catch (e) {
            alert('Не удалось загрузить файл');
            console.error(e);
        }
    }

    SendEmail(): void {
        try {
            var response = RunAction('lmr_SendEmailAnswer', {
                entityName: Xrm.Page.data.entity.getEntityName(),
                id: Xrm.Page.data.entity.getId(),
            }, undefined, false);
            if (response["IsSuccess"]) {
                alert("Сообщение отправлено");
            }
            else {
                alert("Произошла ошибка при отправке, обратитесь к адмминистратору: " + response["ErrorMessage"]);
            }
        }
        catch (error) {
            alert("Произошла ошибка, обратитесь к администратору: " + error);
        }
    }
    SendEmailButtonEnableRule() {
        //запрашиваем контакт клиента, смотрим есть ли у него емейл
        var lmr_contacttypecode: number = Xrm.Page.getAttribute("lmr_contacttypecode").getValue();
        var customerId: any = Xrm.Page.getAttribute("customerid").getValue();
        var result: any = webapi.Retrieve({
            entityName: "contact",
            entityId: customerId[0]["id"],
            queryParams: '?$select=emailaddress1',
            async: false
        });
        //На дочерних обращениях заблокировать кнопку "Отправить ответ" 1049
        var parentcaseid = Xrm.Page.getAttribute("parentcaseid").getValue()
        return (lmr_contacttypecode == 176670001 && result.emailaddress1 != null && result.emailaddress1 != '' && !parentcaseid);
    }

    private static isEmpty(str: any) {
        return (typeof str === "undefined" || str === null || str === "");
    }

    private static AssignActionRequest(isReturnIncident: boolean) {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: Xrm.Page.context.getClientUrl() + "/api/data/v8.2/lmr_Claim_Assign_Action",
                method: "POST",
                dataType: "json",
                data: JSON.stringify({ isReturnIncident }),
                headers: {
                    "OData-MaxVersion": "4.0",
                    "OData-Version": "4.0",
                    "Accept": "application/json",
                },
                contentType: "application/json; charset=utf-8",
                async: true
            })
                .done(data => resolve({
                    result: data.Result, error: data.Error, incidentId: data.IncidentId
                }))
                .fail((req, status, code) => reject({ status, code }));
        });
    }

    OpenIncident() {
        Incident.AssignActionRequest(false)
            .then((res: any) => {
                if (res.result >= 0 && !Incident.isEmpty(res.incidentId)) {

                    webapi.Update({
                        entityId: res.incidentId,
                        entityName: 'incident',
                        entity: {
                            'statuscode': IncidentStatusCode.InWork
                        },
                        async: false
                    });

                    Xrm.Utility.openEntityForm("incident", res.incidentId);
                }
                else {
                    if (res.result == -4)
                        Xrm.Utility.alertDialog(res.error, function () { })
                };
            })
            .catch(err => console.log(`AssignActionRequest error status: ${err.status}; code: ${err.code}`));
    }

    isButtonEnabled(): boolean {
        var isReturnIncident: boolean = true;
        var response = $.ajax({
            url: Xrm.Page.context.getClientUrl() + "/api/data/v8.2/lmr_Claim_Assign_Action",
            method: "POST",
            dataType: "json",
            data: JSON.stringify({ isReturnIncident }),
            headers: {
                "OData-MaxVersion": "4.0",
                "OData-Version": "4.0",
                "Accept": "application/json",
            },
            contentType: "application/json; charset=utf-8",
            async: false
        })
            //.done(data => {

            //    //return data.Result == 3 ? true : false;
            //    //return { result: data.Result, error: data.Error, incidentId: data.IncidentId}
            //})
            .fail((req, status, code) => { console.log(`isButtonEnabled error status: ${status}; code: ${code}`) });
        return response.responseJSON["Result"] == 3 ? true : false;
    }
}

window.LMR = window.LMR || {};
window.LMR.Incident = new Incident();