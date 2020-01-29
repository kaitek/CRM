import { FormBase } from "./formBase";
import webapi from 'xrm-webapi-client';
import * as Common from '../shared/common';
import { Sales } from '../shared/sales';
import { TypeOfSaleCode, OpportunityStateCode, OpportunityStatusCode } from "../shared/Enums";
import { OpenWebResourceInDialog } from "../shared/dialogHelper";
import { RunAction } from "../shared/actionRunner";

export class Opportunity extends FormBase {
    private static isCompleteProcess: boolean = false;
    private static isContactHasApprove: boolean = false;
    private static ContactErrorMessage: string = 'ВНИМАНИЕ! Для продолжения сопровождения этого проекта, получите согласия на обработку персональных данных и коммуникацию от Клиента';
    private static OverrideCompleteProcessFunc(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const currentBpf: any = formContext.data.process;

        currentBpf.completeProcess = () => {
            Opportunity.isCompleteProcess = true;
            currentBpf.$3_1.completeProcess();
        }
    }

    public OnLoad(eventContext: Xrm.Events.EventContext): void {
        Opportunity.AddOnQuoteRowSelectionChange(eventContext);
        Opportunity.SetStoreOnLoad();
        Opportunity.CheckWindows();
        Opportunity.AddOnOpportunityProcessFinish(eventContext);
        Opportunity.SetProjectTypeByUserRole(eventContext);
        Opportunity.AddOnTypeOfSaleChange(eventContext);
        Opportunity.SubscribeOnLoad(eventContext);
        Opportunity.SetHookOnLoad(eventContext);
        //всегда вызывать последней
        Opportunity.OverrideCompleteProcessFunc(eventContext);
    }

    public static FilterContacts(): void {
        const typeofSaleControl = Xrm.Page.getControl<Xrm.Page.OptionSetControl>('lmr_typeofsale');
        var typeofSaleValue = typeofSaleControl.getAttribute().getValue();

        var accountControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('header_process_parentaccountid');
        let accountId = null;

        if (accountControl) {
            accountId = accountControl.getAttribute().getValue();
        }

        var preFilter: string = `<filter><condition attribute='fullname' operator='not-null'/>`;
        if (typeofSaleValue != null && typeofSaleValue != TypeOfSaleCode.Unknown) {
            preFilter += `<condition attribute='lmr_customertype' operator='eq' value='${typeofSaleValue}'/>`
        }

        preFilter += `</filter>`;

        let createCustomView: boolean = false;
        if (accountId && accountId.length) {
            preFilter += `<link-entity name='connection' from='record2id' to='contactid' link-type='inner' alias='con'><filter type='and'><condition attribute='record1id' operator='eq' value='${Common.toTrimBrackets(accountId[0].id)}' /></filter></link-entity>`;
            createCustomView = true;
        }

        const contactControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('parentcontactid');
        const contactHeaderControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('header_process_parentcontactid');

        Opportunity.SetCustomViewOrFilter(contactControl, preFilter, createCustomView ? "{4a3a1df7-6896-4452-adbd-b11fc1de17d0}" : '');
        Opportunity.SetCustomViewOrFilter(contactHeaderControl, preFilter, createCustomView ? "{eb9afead-32e2-471d-ab9b-aa403bf9238b}" : '');
    }

    private static SetCustomViewOrFilter(control: Xrm.Page.LookupControl, filter: string, viewId: string): void {
        if (control == null) { return; }

        if (viewId) {
            var fetchXml = "<fetch mapping='logical' ><entity name='contact'>" + filter + '</entity></fetch>';
            var layoutXml = '<grid name="resultset" jump="fullname" select="1" preview="0" icon="1" object="2"><row name="result" id="contactid"><cell name="fullname" width="300" /><cell name="lmr_customertype" width="100" /><cell name="birthdate" width="100" /><cell name="mobilephone" width="150" imageproviderfunctionname="" imageproviderwebresource="$webresource:" /><cell name="emailaddress1" width="200" /><cell name="lmr_loyaltycardnumber" width="150" imageproviderfunctionname="" imageproviderwebresource="$webresource:" /><cell name="fax" ishidden="1" width="100" /><cell name="address1_name" ishidden="1" width="100" /><cell name="address1_fax" ishidden="1" width="100" /></row></grid>';
            control.addCustomView(viewId, 'contact', 'Контакты организации', fetchXml, layoutXml, true);
        } else {
            control.addCustomFilter(filter, 'contact');
            control.setDefaultView('{A2D479C5-53E3-4C69-ADDD-802327E67A0D}');
        }
    }

    private static CheckIfEraseContact(typeOfSaleValue: number, contactId: string): boolean {

        var query: string = `?$select=fullname,lmr_customertype&$filter=lmr_customertype eq ${typeOfSaleValue}&$top=1`

        var result = webapi.Retrieve({
            async: false,
            entityName: "contact",
            entityId: contactId,
            queryParams: query
        });

        let res: boolean = result.lmr_customertype === typeOfSaleValue ? false : true;

        return res;
    }

    public static ShowCustomMarkAsLostButton(): boolean {
        //hide the custom button while the functionality is not fully implemented
        return false;
    }

    public static ContactPreSearch(typeofSaleValue: number): void {

        if (typeofSaleValue === null)
            return;

        const contactControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('parentcontactid');
        const contactHeaderControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('header_process_parentcontactid');

        if (contactControl != null) {
            Common.addPreSearch(contactControl, Opportunity.FilterContacts);
        }

        if (contactHeaderControl != null) {
            Common.addPreSearch(contactHeaderControl, Opportunity.FilterContacts);
        }

        let needErase: boolean = false;
        let lookUpAttribute: Xrm.Attributes.LookupAttribute;

        if (contactHeaderControl != null) {
            if (typeofSaleValue === null || typeofSaleValue === TypeOfSaleCode.Unknown)
                return;

            lookUpAttribute = contactHeaderControl.getAttribute();
            if (lookUpAttribute) {
                let lookupValue = lookUpAttribute.getValue();
                if (lookupValue && lookupValue.length)
                    needErase = Opportunity.CheckIfEraseContact(typeofSaleValue, lookupValue[0].id);
                if (needErase) {
                    var parentContact = Xrm.Page.getAttribute('parentcontactid');
                    if (parentContact != null)
                        parentContact.setValue(null);
                }
            }
        }

        var headerParentContactId = Xrm.Page.getAttribute('header_process_parentcontactid');
        if (headerParentContactId != null && needErase) {
            headerParentContactId.setValue(null);
        }
    }


    public static AddPreFilter(eventContext: Xrm.Events.EventContext): void {

        const typeofSaleControl = Xrm.Page.getControl<Xrm.Page.OptionSetControl>('lmr_typeofsale');
        var typeofSaleValue = typeofSaleControl.getAttribute().getValue();

        if (typeofSaleValue === null)
            return;

        Opportunity.ContactPreSearch(typeofSaleValue);
    }

    public static SetProjectTypeByUserRole(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        if (formType != XrmEnum.FormType.Create)
            return;

        var typeofSaleHeaderControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('header_process_lmr_projecttypeid');
        if (!typeofSaleHeaderControl) {
            return;
        }

        const userGroupsResult: any = RunAction("lmr_retrieve_user_project_type", null, undefined, false);

        if (userGroupsResult["userProjectType"]) {
            var entity = JSON.parse(userGroupsResult["userProjectType"]);
            if (entity.id) {
                typeofSaleHeaderControl.getAttribute().setValue([entity]);
            }
        }
    }

    public static AddOnTypeOfSaleChange(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        if (formType == XrmEnum.FormType.ReadOnly || formType == XrmEnum.FormType.Disabled)
            return;

        if (formType == XrmEnum.FormType.Update && formContext.getAttribute('statecode').getValue() != OpportunityStateCode.Open)
            return;

        if (formContext.getAttribute('statuscode') === null)
            return;

        if (formContext.getAttribute('statuscode').getValue() == OpportunityStatusCode.Won
            || formContext.getAttribute('statuscode').getValue() == OpportunityStatusCode.Canceled)
            return;

        const typeofSaleHeaderControl = Xrm.Page.getControl<Xrm.Page.OptionSetControl>('header_process_lmr_typeofsale');
        if (typeofSaleHeaderControl != null) {
            Xrm.Page.getAttribute('lmr_typeofsale').addOnChange(Opportunity.OnHeaderTypeOfSaleChanged);
        }
        let typeofSaleValue: number = TypeOfSaleCode.Unknown;
        if (Xrm.Page.data.entity.getId() != '') {

            const typeofSaleControl = Xrm.Page.getControl<Xrm.Page.OptionSetControl>('lmr_typeofsale');

            if (typeofSaleControl != null) {
                typeofSaleValue = typeofSaleControl.getAttribute().getValue();

                if (typeofSaleValue === null)
                    return;

                if (typeofSaleValue == TypeOfSaleCode.organization) {
                    Xrm.Page.ui.tabs.get('tab_main').sections.get('account_information').setVisible(true);
                }
                if (typeofSaleValue == TypeOfSaleCode.physical) {
                    var parentaccountid = Xrm.Page.getAttribute('parentaccountid');

                    if (parentaccountid != null)
                        parentaccountid.setValue(null);
                }
            }
        }
        if (typeofSaleValue === null)
            return;
        Opportunity.ContactPreSearch(typeofSaleValue);
    }

    public static OnHeaderTypeOfSaleChanged(eventContext: Xrm.Events.EventContext) {

        var typeofSaleValue = Xrm.Page.getAttribute('lmr_typeofsale').getValue();

        if (typeofSaleValue === null)
            return;

        if (typeofSaleValue == TypeOfSaleCode.organization) {
            Xrm.Page.ui.tabs.get('tab_main').sections.get('account_information').setVisible(true);
        }
        else {
            Xrm.Page.ui.tabs.get('tab_main').sections.get('account_information').setVisible(false);
        }

        if (typeofSaleValue == TypeOfSaleCode.physical) {
            var parentaccountid = Xrm.Page.getAttribute('parentaccountid');

            if (parentaccountid != null)
                parentaccountid.setValue(null);
        }

        Opportunity.AddPreFilter(eventContext);
    }

    public static AddOnOpportunityProcessFinish(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();

        if (formContext.data.process)
            formContext.data.entity.addOnSave(Opportunity.prototype.CheckOpenedTasks);
    }

    private CheckOpenedTasks(context: any): void {
        if (!Opportunity.isCompleteProcess)
            return;

        const eventContext: Xrm.Events.SaveEventContext = context;
        if (eventContext.getEventArgs().getSaveMode() != XrmEnum.SaveMode.AutoSave) {
            Opportunity.isCompleteProcess = false;

            const formContext: Xrm.FormContext = eventContext.getFormContext();
            const stageName = formContext.data.process.getSelectedStage().getName().toLowerCase();
            const russianStageName = "завершено";

            if (stageName == russianStageName) {
                const formatedId: any = Common.toTrimBrackets(formContext.data.entity.getId());
                const query = `?$select=activityid&$filter=_regardingobjectid_value eq ${formatedId} and statecode eq ${0}&$top=1`;
                const openedTask: any = webapi.Retrieve({
                    async: false,
                    entityName: 'task',
                    queryParams: query
                });

                if (openedTask.value && openedTask.value.length > 0) {
                    context.getEventArgs().preventDefault();

                    const refreshingStage = formContext.data.process.getActiveStage().getId();
                    formContext.data.process.setActiveStage(refreshingStage);

                    OpenWebResourceInDialog('lmr_/html/tasks_close_dialog.html', formatedId, { width: 900, height: 400 }, () => {
                        debugger;
                        const tasksSubgrid: Xrm.Controls.GridControl = formContext.getControl("tasks_subgrid");
                        if (tasksSubgrid)
                            tasksSubgrid.refresh();
                    });
                }
            }
        }
    }

    public static AddOnQuoteRowSelectionChange(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const quotesGrid: any = formContext.getControl("quotes_subgrid");

        if (quotesGrid)
            quotesGrid.getGrid().$3_1.add_onSelectionChange(() => { formContext.ui.refreshRibbon(); });
    }

    public CheckSelectedQuoteRow() {
        const quotesGrid: Xrm.Controls.GridControl = Xrm.Page.getControl("quotes_subgrid");

        let showButtons: boolean = false;

        if (quotesGrid)
            showButtons = quotesGrid.getGrid().getSelectedRows().getLength() === 1;

        return showButtons;
    }

    public CloseQuoteAsWonAction(): void {
        const quotesGridControl: Xrm.Controls.GridControl = Xrm.Page.getControl("quotes_subgrid");
        const quoteId = quotesGridControl.getGrid().getSelectedRows().get()[0].data.entity.getId()
            .replace("{", "").replace("}", "");

        let statecode: number = Opportunity.prototype.GetSelectedQuoteStateCode(quoteId);

        if (statecode == 0) {
            webapi.Update({
                entityId: quoteId,
                entityName: "quote",
                entity: {
                    "statecode": 1
                },
                async: false
            });

            statecode = Opportunity.prototype.GetSelectedQuoteStateCode(quoteId);
        }
        if (statecode == 1) {
            let url = webapi.GetApiUrl() + "WinQuote";
            let payload = {
                "Status": 4,
                "QuoteClose": {
                    "subject": "Won quote",
                    "quoteid@odata.bind": `/quotes(${quoteId})`
                }
            };

            webapi.SendRequest("POST", url, payload)
                .then(() => Opportunity.prototype.RefreshQuotesSubgrid())
                .catch(() => Xrm.Utility.alertDialog("При закрытии расчета произошла ошибки. Обратитесь к администратору", () => { }))
        }
        else {
            Xrm.Utility.alertDialog("Выбранный расчет не может быть закрыт как выигрышный", () => { });
        }
    }

    public GetSelectedQuoteStateCode(quoteId: string): number {
        let response = webapi.Retrieve({
            async: false,
            entityId: quoteId,
            entityName: "quote",
            queryParams: '?$select=statecode'
        });

        return response["statecode"];
    }

    public DeactivateQuoteAction(): void {
        const quotesGridControl: Xrm.Controls.GridControl = Xrm.Page.getControl("quotes_subgrid");
        const quoteid: string = quotesGridControl.getGrid().getSelectedRows().get()[0].data.entity.getId();

        Sales.DeactivateQuote(quoteid, () => quotesGridControl.refresh());
    }

    public RefreshQuotesSubgrid(): void {
        Xrm.Page.getControl<Xrm.Controls.GridControl>("quotes_subgrid").refresh();
    }

    static SetStoreOnLoad(): void {
        if (Xrm.Page.data.entity.getId() == '') {//на создании обращени¤ 
            const storeAttribute = Xrm.Page.getAttribute('lmr_storeid');
            const userId = Xrm.Page.context.getUserId();

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
    static setState(tabName: string, sectionName: string, ...args: any[]): void {
        let tab = Xrm.Page.ui.tabs.get(tabName);
        let section = null;
        if (!Common.isEmpty(tab))
            section = tab.sections.get(sectionName);
        section = !Common.isEmpty(section) ? section : false;
        !Common.isEmpty(section) ? Opportunity.oppControlState(section, args) : null;
    }
    static isEnabled(flag: boolean, args: any[]): void {
        args.forEach(val => Common.setReadonly(flag, val));
    }
    static isVisible(flag: boolean, section: any): void {
        section ? section.setVisible(flag) : null;
    }
    static argsIsEnabled(flag: boolean, args: any[]): void {
        if (flag) {
            Opportunity.isEnabled(false, args.slice(2, 3));
            Opportunity.isEnabled(flag, args.slice(0, 2));
        }
        else {
            Opportunity.isEnabled(true, args.slice(2, 3));
            Opportunity.isEnabled(flag, args.slice(0, 2));
        }
    }
    static oppControlState(section: any, args: any[]): void {
        let activeStage: Xrm.ProcessFlow.Stage = Xrm.Page.data.process.getActiveStage();
        let _args: any[] = [];
        if (!Common.isEmpty(activeStage)) {
            let measure = Xrm.Page.getControl("header_process_lmr_addressofmeasure");
            const measuringControl = Xrm.Page.getControl<Xrm.Page.OptionSetControl>('lmr_measuring');
            let measuringCtrlValue = null; //176670000
            if (!Common.isEmpty(measuringControl))
                measuringCtrlValue = measuringControl.getAttribute().getValue();
            if (args.length == 0) {
                _args = [Xrm.Page.getControl("header_process_lmr_addressofmeasure_1"),
                Xrm.Page.getControl("header_process_lmr_flat_1"),
                Xrm.Page.getControl("header_process_lmr_addressofdelivery")
                ];
            }

            if (activeStage.getName() == "Замер") {
                if (measuringCtrlValue == 176670000) {
                    if (measure)
                        Common.setReadonly(true, measure);
                    Opportunity.isVisible(true, section);
                }
                else
                    Opportunity.isVisible(false, section);
                if (_args.length > 0)
                    Opportunity.argsIsEnabled(true, _args);
                Opportunity.argsIsEnabled(true, args);
            }
            else if (activeStage.getName() == "Договор") {
                if (measuringCtrlValue == 176670001)
                    Opportunity.isVisible(true, section);
                else
                    Opportunity.isVisible(false, section);
                if (_args.length > 0)
                    Opportunity.argsIsEnabled(false, _args);
                Opportunity.argsIsEnabled(false, args);
            }

            else if (activeStage.getName() == "Оплата") {
                if (measuringCtrlValue == 176670001)
                    Opportunity.isVisible(true, section);
                else
                    Opportunity.isVisible(false, section);
                if (_args.length > 0)
                    Opportunity.argsIsEnabled(true, _args);
                Opportunity.argsIsEnabled(true, args);
            }
            else {
                if (measure)
                    Common.setReadonly(false, measure);
                if (_args.length > 0)
                    Opportunity.isEnabled(false, _args);
                Opportunity.isEnabled(false, args.slice(0, 3));
                Opportunity.isVisible(false, section);
            }
        }
    }
    static CheckWindows() {
        const WINDOWS = '{355f44df-fec2-e811-9101-005056b1c5f1}';
        const KITCHEN = '{f817e605-44c2-e811-9101-005056b1c5f1}';
        const SALES = '{9e686eab-90ed-e811-9107-005056b1c5f1}';
        Opportunity.setState('tab_6', 'section_yandex_map');
        if (Xrm.Page.ui.getFormType() === XrmEnum.FormType.Update) {
            let lmr_projecttypeid = Xrm.Page.getAttribute('lmr_projecttypeid');
            if (!Common.isEmpty(lmr_projecttypeid)) {
                if (!Common.isEmpty(lmr_projecttypeid.getValue())) {
                    let lmr_projecttypeidValue = lmr_projecttypeid.getValue()[0].id;
                    if (!Common.isEmpty(lmr_projecttypeidValue)) {
                        if (lmr_projecttypeidValue.toLowerCase() === WINDOWS ||
                            lmr_projecttypeidValue.toLowerCase() == KITCHEN ||
                            lmr_projecttypeidValue.toLowerCase() === SALES) {
                            let addressAtr: Xrm.Controls.StandardControl =
                                Xrm.Page.getControl("header_process_lmr_addressofmeasure_1");
                            let addressAtr2: Xrm.Controls.StandardControl =
                                Xrm.Page.getControl("header_process_lmr_addressofdelivery");
                            let apartmentAtr: Xrm.Controls.StandardControl =
                                Xrm.Page.getControl("header_process_lmr_flat_1");
                            Xrm.Page.data.process.addOnStageChange(this.setState
                                .bind(null, 'tab_6', 'section_yandex_map', addressAtr, apartmentAtr, addressAtr2));
                            Xrm.Page.data.refresh(true);
                        }
                    }
                }
            }
        }
    }
    static ValidateContactInProcess(eventContext: any): void {
        //debugger;
        //const formContext: Xrm.FormContext = eventContext.getFormContext();

        //let direction: any = eventContext.$R_0.$4b_2;

        //if (direction != 'Next')
        //    return;

        //var parentContact = Xrm.Page.getAttribute('parentcontactid');

        //if (parentContact != null && !Opportunity.isContactHasApprove) {
        //    formContext.data.process.movePrevious();
        //    //eventContext.getEventArgs().preventDefault();
        //    Opportunity.SetErrorMessage(Opportunity.ContactErrorMessage);
        //}
    }

    private static SetErrorMessage(message: string): void {
        Xrm.Page.ui.setFormNotification(message, 'WARNING', 'pleasewait');
        window.setTimeout(Opportunity.SetTimeout, 30000);
    }

    private static SetTimeout(): void {
        Xrm.Page.ui.clearFormNotification('pleasewait');
    }

    static SubscribeOnLoad(eventContext: Xrm.Events.EventContext): void {
        if (Xrm.Page.data.entity.getId() != '' && Xrm.Page.getAttribute('statecode').getValue() == 0) {
            var parentContact = Xrm.Page.getAttribute('parentcontactid');
            if (parentContact != null) {
                parentContact.addOnChange(Opportunity.OnParentContactIdChanged);
                Opportunity.ValidatePersonalDataApproved();
                if (!Opportunity.isContactHasApprove)
                    Opportunity.SetErrorMessage(Opportunity.ContactErrorMessage);
            }
        }
    }

    public static ValidatePersonalDataApproved(): boolean {
        Opportunity.isContactHasApprove = false;
        let result = false;
        let contactId = null;         

        const contactHeaderControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('header_process_parentcontactid');

        if (contactHeaderControl == null)
            return false;

        let lookUpAttribute: Xrm.Attributes.LookupAttribute = contactHeaderControl.getAttribute();

        if (lookUpAttribute) {
            let lookupValue = lookUpAttribute.getValue();
            if (lookupValue && lookupValue.length) {
                contactId = lookupValue[0].id;
            }
        }
        
        if (contactId == null)
            return result;

        var query: string = `?$select=lmr_communicationconsent,lmr_personaldataprocessingconsent&$top=1`;

        var response = webapi.Retrieve({
            async: false,
            entityName: "contact",
            entityId: contactId,
            queryParams: query
        });

         
        if (response.lmr_communicationconsent != undefined && response.lmr_personaldataprocessingconsent != undefined) {
            if (response.lmr_communicationconsent == true && response.lmr_personaldataprocessingconsent == true)
                result = true;
        }
        Opportunity.isContactHasApprove = result;
        return result;
    }

    static OnParentContactIdChanged(eventContext: Xrm.Events.EventContext) {
        Opportunity.isContactHasApprove = Opportunity.ValidatePersonalDataApproved();
    }

    static SetHookOnLoad(eventContext: Xrm.Events.EventContext): void {
        if (Xrm.Page.data.entity.getId() != '') {
            if (Xrm.Page.data.process != null) {
                Xrm.Page.data.process.addOnStageChange(Opportunity.ValidateContactInProcess);
                Opportunity.ValidatePersonalDataApproved();
            }
        }
    }

    CloseOppAsLostAction(): void {
        OpenWebResourceInDialog('lmr_/html/opportunity_close_as_lost.html' , Xrm.Page.data.entity.getId(), { width: 350, height: 350 }, (value) => {
            //debugger;
            if (value === '') {
                Opportunity.SetErrorMessage('Действие было отменено');
                return;
            }
            Xrm.Page.data.refresh(false); 
        });
    }
}

window.LMR = window.LMR || {};
window.LMR.Opportunity = new Opportunity();
