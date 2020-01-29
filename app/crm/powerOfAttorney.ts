import { FormBase } from './formBase';
import { FetchXML, XMLLayouts, Condition } from './fetchXML';
import webapi from 'xrm-webapi-client';
import * as Common from '../shared/common';
import { CustomerTypes } from "../shared/Enums";

export class PowerOfAttorney extends FormBase {
    public OnLoad(context: Xrm.Events.EventContext): void {
        const formContext = context.getFormContext();

        formContext.getControl<Xrm.Controls.LookupControl>("lmr_contactid")
            .addPreSearch(PowerOfAttorney.SetContactLookupPreSeacrh);

        PowerOfAttorney.SetSelectedAccountFilteringViewOnContactLookup(formContext);
        PowerOfAttorney.SetStoreOnLoad(formContext); 
    }
   

    static SetStoreOnLoad(formContext: Xrm.FormContext): void {
        
        const formType: XrmEnum.FormType = formContext.ui.getFormType();
        if (formType == XrmEnum.FormType.Update)
            return;

        const storeAttribute = formContext.getAttribute('lmr_storestorage');
        if (Xrm.Page.getControl('lmr_storestorage') === null)
            return;
        const userId = Xrm.Page.context.getUserId();

        //set store only on attorney creating
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


    private static SetContactLookupPreSeacrh(context: Xrm.Events.EventContext): void {
        const formContext = context.getFormContext();

        PowerOfAttorney.SetB2BFilteringViewOnContactLookup(formContext);
    }

    private static SetSelectedAccountFilteringViewOnContactLookup(formContext: Xrm.FormContext): void {
        const contactControl = formContext.getControl<Xrm.Controls.LookupControl>("lmr_contactid");
        const account: Xrm.Attributes.LookupAttribute = formContext.getAttribute("lmr_accountid");
        const accountValue = account.getValue() == null ? "00000000-0000-0000-0000-000000000000" : account.getValue()[0].id;
        const viewId = "ba0e3fcb-e1f1-4d94-a414-fd1fd909a675";

        var connectionsFetchXML: string = FetchXML.FetchXMLCreator(
            {
                entityName: "connection",
                attributes: ["record2id"],
                filter: {
                    conditions: [
                        { attribute: "record1id", operator: "eq", uitype: "account", value: accountValue },
                        { attribute: "statecode", operator: "eq", value: "0" }
                    ],
                    type: "and"
                }
            }
        );
        var connections = webapi.Retrieve({
            entityName: "connection",
            fetchXml: connectionsFetchXML,
            async: false
        });

        var contactsConditions: Condition[];

        if (connections.value.length == 0)
            contactsConditions = [{ attribute: "contactid", uitype: "contact", operator: "null" }];
        else
            contactsConditions = connections.value.map((con: any) => {
                return { attribute: "contactid", uitype: "contact", operator: "eq", value: con._record2id_value }
            });

        var contactsFetchXML: string = FetchXML.FetchXMLCreator(
            {
                entityName: "contact",
                attributes: ["fullname", "lmr_customertype", "birthdate", "mobilephone", "emailaddress1", "lmr_loyaltycardnumber"],
                filter: {
                    filters: [{ type: "or", conditions: contactsConditions }],
                    conditions: [
                        { attribute: "statecode", operator: "eq", value: "0" },
                        { attribute: "lmr_customertype", operator: "eq", value: "176670001" },
                    ],
                    type: "and"
                }
            }
        );

        contactControl.addCustomView(viewId,
            "contact", "Контакты выбранной организации",
            contactsFetchXML, XMLLayouts.ContactsOnPowerOfAttorney,
            false
        );
    }

    private static SetB2BFilteringViewOnContactLookup(formContext: Xrm.FormContext): void {
        const contactControl = formContext.getControl<Xrm.Controls.LookupControl>("lmr_contactid");

        const entityName = "contact";
        const displayName = "B2B контакты";

        var fetchXML: string = FetchXML.FetchXMLCreator(
            {
                entityName: entityName,
                attributes: ["fullname", "lmr_customertype", "birthdate", "mobilephone", "emailaddress1", "lmr_loyaltycardnumber"],
                filter: {
                    conditions: [
                        { attribute: "lmr_customertype", operator: "eq", value: "176670001" },
                        { attribute: "statecode", operator: "eq", value: "0" },
                    ],
                    type: "and"
                }
            },
            {
                attribute: "fullname",
                descending: false
            }
        );

        contactControl.addCustomView(contactControl.getDefaultView(), entityName,
            displayName, fetchXML,
            XMLLayouts.ContactsOnPowerOfAttorney,
            true
        );
    }

    public OnSave(eventContext: Xrm.Events.SaveEventContext): void {
        
        //console.log('PowerOfAttorney.OnSave 1.3');

        if (eventContext.getEventArgs().getSaveMode() == XrmEnum.SaveMode.AutoSave) {
            eventContext.getEventArgs().preventDefault();
            return;
        }

        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        if (formType == XrmEnum.FormType.Update) {
            if (Xrm.Page.getAttribute('lmr_workfromdate').getValue() != null && Xrm.Page.getAttribute('lmr_worktilldate').getValue() != null) {
                let lmr_workfromdate: Xrm.Controls.DateControl;
                lmr_workfromdate = Xrm.Page.getControl('lmr_workfromdate');

                let lmr_worktilldate: Xrm.Controls.DateControl;
                lmr_worktilldate = Xrm.Page.getControl('lmr_worktilldate');

                let dt_worktilldate: Xrm.Attributes.DateAttribute;
                dt_worktilldate = lmr_worktilldate.getAttribute();

                let dt_workfromdate: Xrm.Attributes.DateAttribute;
                dt_workfromdate = lmr_workfromdate.getAttribute();

                if (dt_workfromdate.getValue() > dt_worktilldate.getValue()) {
                  
                    Xrm.Page.ui.setFormNotification('Невозможно сохранить доверенность ДЕЙСТВУЕТ ПО меньше ДЕЙСТВУЕТ С', 'ERROR', 'invalid_passport');
                    eventContext.getEventArgs().preventDefault();
                    window.setTimeout(this.SetTimeout, 5000);
                    return;
                }
            }
        }

        if (formType == XrmEnum.FormType.Create)// && eventContext.getEventArgs().getSaveMode() != XrmEnum.SaveMode.AutoSave) {
            {
            const contactControl = formContext.getControl<Xrm.Controls.LookupControl>('lmr_contactid');
            let lookUpAttribute: Xrm.Attributes.LookupAttribute;
            lookUpAttribute = contactControl.getAttribute();

            if (lookUpAttribute != null) {

                var query: string = `?$select=fullname,middlename,firstname,lastname,lmr_customertype,lmr_passportissuedby,lmr_passportseriesandnumber,lmr_passportissuedate&$top=1`

                var result = webapi.Retrieve({
                    async: false,
                    entityName: 'contact',
                    entityId: lookUpAttribute.getValue()[0].id,
                    queryParams: query
                });

                Xrm.Page.ui.clearFormNotification('invalid_passport');
                Xrm.Page.ui.clearFormNotification('invalid_type');

                try {
                    //доверенности создаются только для Юр. контактов
                    if (result.lmr_customertype != CustomerTypes.Legal) {
                        Xrm.Page.ui.setFormNotification('Доверенность возможно создать только для контакта с типом клиента = Юр.', 'ERROR', 'invalid_type');
                        eventContext.getEventArgs().preventDefault();
                        window.setTimeout(this.SetTimeout, 5000);
                        return;
                    }

                    let errorMessage: string = this.ValidateFieldSet(
                        [result.lastname, result.firstname, result.middlename, result.lmr_passportseriesandnumber, result.lmr_passportissuedby, result.lmr_passportissuedate],
                        ['фамилия', 'имя', 'отчество', 'серия и номер', 'кем выдано', 'когда']);

                    if (errorMessage) {
                        Xrm.Page.ui.setFormNotification('Невозможно создать доверенность для контакта, у которого не заполнено(ы) ' + errorMessage, 'ERROR', 'invalid_passport');
                        eventContext.getEventArgs().preventDefault();
                        window.setTimeout(this.SetTimeout, 5000);
                        return;
                    }
                    if (Xrm.Page.getAttribute('lmr_workfromdate').getValue() != null && Xrm.Page.getAttribute('lmr_worktilldate').getValue() != null) {
                        let lmr_workfromdate: Xrm.Controls.DateControl;
                        lmr_workfromdate = Xrm.Page.getControl('lmr_workfromdate');

                        let lmr_worktilldate: Xrm.Controls.DateControl;
                        lmr_worktilldate = Xrm.Page.getControl('lmr_worktilldate');

                        let dt_worktilldate: Xrm.Attributes.DateAttribute; 
                        dt_worktilldate = lmr_worktilldate.getAttribute();

                        let dt_workfromdate: Xrm.Attributes.DateAttribute;
                        dt_workfromdate = lmr_workfromdate.getAttribute();

                        if (dt_workfromdate.getValue() > dt_worktilldate.getValue()) {
                            Xrm.Page.ui.setFormNotification('Невозможно сохранить доверенность ДЕЙСТВУЕТ ПО меньше ДЕЙСТВУЕТ С', 'ERROR', 'invalid_passport');
                            eventContext.getEventArgs().preventDefault();
                            window.setTimeout(this.SetTimeout, 5000); 
                            return;
                        }
                    }                    

                } catch (e) {
                    console.log(e);
                }
            }
        }
    }

    private ValidateFieldSet(values: any[], fieldnames: string[]): string {
        if (values.length != fieldnames.length) {
            throw 'Values and fieldnames length must match';
        }

        let result: string = '';

        values.map((v, i) => {
            if (!v) {
                result += fieldnames[i] + ',';
            }
        });

        if (result.endsWith(',')) {
            result = result.substring(0, result.length - 1);
        }

        return result;
    }

    private SetTimeout(): void {
        Xrm.Page.ui.clearFormNotification('invalid_passport');
        Xrm.Page.ui.clearFormNotification('invalid_type');
    }
}

window.LMR = window.LMR || {}
window.LMR.PowerOfAttorney = new PowerOfAttorney();