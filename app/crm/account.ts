import { FormBase } from "./formBase";
import { RunAction } from "../shared/actionRunner";
import { OpenWebResourceInDialog } from '../shared/dialogHelper';
import { RegexValidator } from './RegexValidator';
import webapi from 'xrm-webapi-client'
import * as Common from '../shared/common';
import { CustomerTypes, AccountType, AddressTypeCode } from "app/shared/Enums";
import { number } from "prop-types";

export class Account extends FormBase {
    private AttributeRegexMap = [
        { attributes: ['lmr_iec'], regex: '^\\d*$', errorText: 'Поле "КПП" должно состоять только из цифр.' },
        { attributes: ['lmr_itn'], regex: '^\\d*$', errorText: 'Поле "ИНН" должно состоять только из цифр.' },
        { attributes: ['lmr_bic'], regex: '^(\\d{9}){0,1}$', errorText: 'Поле "БИК" должно состоять только из 9 цифр.' },
        { attributes: ['lmr_currentaccount'], regex: '^(\\d{20}){0,1}$', errorText: 'Поле "Р\\с банка" должно состоять только из 20 цифр.' },
        { attributes: ['lmr_correspondentaccount'], regex: '^(\\d{20}){0,1}$', errorText: 'Поле "К\\с банка" должно состоять только из 20 цифр.' }
    ]

    OnLoad(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        if (formType == XrmEnum.FormType.Create) {
            Account.SetStoreOnLoad(formContext);
            formContext.getAttribute('lmr_itn').addOnChange(this.OnItnFieldChange);
        }

        //check KPP both on create new child org or on edit an existing one
        formContext.getAttribute('lmr_iec').addOnChange(this.OnIecFieldChange);
        //large tax payer KPP validation
        formContext.getAttribute('lmr_ieclargetaxpayer').addOnChange(this.OnIecLargeTaxtPayerFieldChange);

        new RegexValidator(this.AttributeRegexMap).register();
        this.SetFieldsRequirements(eventContext);
    };

    SetFieldsRequirements(eventContext: Xrm.Events.EventContext) {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        formContext.getAttribute('lmr_accounttype').addOnChange(this.OnAccountTypeFieldChange);
        this.OnAccountTypeFieldChange(eventContext);
    }

    OnRetrieveBalanceButtonClick() {
        const customerNumber = Xrm.Page.getAttribute('accountnumber').getValue();
        OpenWebResourceInDialog('lmr_/html/balance_dialog.html', customerNumber, { width: 300, height: 200 })
    };

    OnIecFieldChange(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const iecStr: string = formContext.getAttribute('lmr_iec').getValue();
        if (iecStr) {
            Account.prototype.IsValidAccountIec(formContext);
        }
    }

    OnIecLargeTaxtPayerFieldChange(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const iecStr: string = formContext.getAttribute('lmr_ieclargetaxpayer').getValue();
        const iecLargeTaxCtrl = formContext.getControl<Xrm.Controls.StandardControl>('lmr_ieclargetaxpayer');

        iecLargeTaxCtrl.clearNotification('iecLargeTaxUniqueId');

        if (iecStr) {
            const iecPargeTaxRegex: RegExp = /\d{9}/;
            const res = iecPargeTaxRegex.test(iecStr);
            const errorMessage: string = 'КПП крупного налогоплательщика должен состоять из 9 цифр.';

            if (!res) {
                iecLargeTaxCtrl.setNotification(errorMessage, 'iecLargeTaxUniqueId');
                Xrm.Page.ui.setFormNotification(errorMessage, 'ERROR', 'pleasewait');
                window.setTimeout(Account.SetTimeout, 5000);
            }
        }
    }

    //lmr_ieclargetaxpayer КПП крупного налогоплательщика

    OnItnFieldChange(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const parentCompany = formContext.getAttribute('parentaccountid').getValue();
        const itnControl = formContext.getControl<Xrm.Controls.StandardControl>('lmr_itn');

        itnControl.clearNotification('itnErrorUniqueId');

        //организация является дочерней
        if (parentCompany)
            return;
        //инн незаполнен
        if (!itnControl.getAttribute().getValue())
            return;

        try {
            var response = RunAction('lmr_GetAccountInfoByItn', null,
                {
                    itn: itnControl.getAttribute().getValue()
                },
                false
            );

            if (response.isSuccess) {
                var accountData = JSON.parse(response.accountFieldsJSON)[0];

                //сохраняем JSON-данные и KonturForcus в служебное поле
                formContext.getAttribute('lmr_kfdata').setValue(response.accountFieldsJSON);

                let dissolvedResult: boolean = Account.CheckAccountDissolved(formContext, accountData);

                if (dissolvedResult)
                    return;

                if (accountData.UL != null)
                    Account.SetUlData(formContext, accountData);
                else if (accountData.IP != null)
                    Account.SetIpData(formContext, accountData);
                else
                    Account.SetFOData(formContext, accountData);

                Account.CheckPotentialAccountOnDuplicate(formContext,
                    itnControl.getAttribute().getValue(),
                    formContext.getAttribute('lmr_iec').getValue()
                );

            }
            else {
                itnControl.setNotification(response.invalidMessage, 'itnErrorUniqueId');
                Xrm.Page.ui.setFormNotification(response.invalidMessage, 'ERROR', 'pleasewait');
                window.setTimeout(Account.SetTimeout, 5000);
            }
        }
        catch (e) {
            alert(e);
        }
    }

    static SetStoreOnLoad(formContext: Xrm.FormContext): void {
        const storeAttribute = formContext.getAttribute('lmr_storeid');
        if (Xrm.Page.getControl('lmr_storeid') === null)
            return;
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

    private IsValidAccountIec(formContext: Xrm.FormContext): void {
        const accountType: number = formContext.getAttribute('lmr_accounttype').getValue();
        const parentAccount: Xrm.Attributes.LookupAttribute = formContext.getAttribute('parentaccountid').getValue();
        const iecControl: Xrm.Controls.StandardControl = formContext.getControl('lmr_iec');
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        iecControl.clearNotification();
        const editExisting: boolean = formType == XrmEnum.FormType.Update && accountType == AccountType.Legal;
        const createAffiliated: boolean = formType == XrmEnum.FormType.Create

        if ((editExisting || createAffiliated) && !parentAccount) {
            const itn: string = formContext.getAttribute('lmr_itn').getValue();

            let parentAccountKfDataJson: string = formContext.getAttribute("lmr_kfdata").getValue();
            //if (!parentAccountKfDataJson) {
            const response = RunAction('lmr_GetAccountInfoByItn', null, { itn: itn }, false);

            if (!response.isSuccess) {
                iecControl.setNotification('Произошла непредвиденная ошибка при проверке КПП', 'itnErrorUniqueId');
                Xrm.Page.ui.setFormNotification('Произошла непредвиденная ошибка при проверке КПП', 'ERROR', 'pleasewait');
                window.setTimeout(Account.SetTimeout, 5000);
            }
            else {
                parentAccountKfDataJson = response.accountFieldsJSON
            }
            //}

            const parentAccountKfData: any = JSON.parse(parentAccountKfDataJson)[0];
            if (parentAccountKfData.UL != null) {
                if (parentAccountKfData.UL.kpp != iecControl.getAttribute().getValue()) {
                    iecControl.setNotification('КПП не принадлежит головной организации', 'iecErrorUniqueId');
                    Xrm.Page.ui.setFormNotification('КПП не принадлежит головной организации', 'ERROR', 'pleasewait');
                    window.setTimeout(Account.SetTimeout, 5000);
                }
            }
        }
    }

    private static CheckPotentialAccountOnDuplicate(formContext: Xrm.FormContext, itn: string, iec: string): void {
        var baseQuery: string = `?$select=name,lmr_itn,lmr_iec,accountid&$filter=statecode eq 0 and _parentaccountid_value eq null  `;
        var accountId = formContext.data.entity.getId();

        if (accountId != "")
            baseQuery = baseQuery.concat(`and accountid ne '${accountId}' `);
        if (itn)
            baseQuery = baseQuery.concat(`and lmr_itn eq '${itn}' `);
        if (iec)
            baseQuery = baseQuery.concat(`and lmr_iec eq '${iec}' `);

        var result: any = webapi.Retrieve({
            async: false,
            entityName: "account",
            queryParams: baseQuery
        });

        if (result.value.length != 0)
            OpenWebResourceInDialog('lmr_/html/duplicate_accounts_dialog.html', JSON.stringify(result.value), { width: 700, height: 340 })
    }

    private static CheckAccountDissolved(formContext: Xrm.FormContext, accountData: any): boolean {

        let dissolvedResult: boolean = false;

        if (accountData.UL != null && accountData.UL.status != null && accountData.UL.status.dissolved) {
            dissolvedResult = true;
        }

        if (accountData.IP != null && accountData.IP.status != null && accountData.IP.status.dissolved) {
            dissolvedResult = true;
        }

        if (dissolvedResult) {
            Xrm.Page.ui.setFormNotification('Ликвидированная организация не регистрируется', 'ERROR', 'pleasewait');
            window.setTimeout(Account.SetTimeout, 5000);
            formContext.getAttribute('lmr_itn').setValue(null);
        }
        return dissolvedResult;
    }

    private static SetIpData(formContext: Xrm.FormContext, accountData: any): void {
        formContext.getAttribute('lmr_accounttype').setValue(AccountType.IP);
        formContext.getAttribute('lmr_psrn').setValue(accountData.ogrn);
        formContext.getAttribute('name').setValue(accountData.IP.fio);
        formContext.getAttribute('lmr_rnnbo').setValue(accountData.IP.okpo);
        formContext.getAttribute('lmr_iec').setValue(null);
        if (accountData.IP.registrationDate)
            formContext.getAttribute('lmr_registeredon').setValue(new Date(accountData.IP.registrationDate));

        formContext.getAttribute('address1_addresstypecode').setValue(AddressTypeCode.Legal);

        var ip = accountData.IP;
        var legalAddress = ip.legalAddress ? ip.legalAddress : null;
        var addressRF = legalAddress ? legalAddress.parsedAddressRF : null;


        if (addressRF != null) {
            var addressLine1: string = Account.GetIpAddressLine(accountData);
            formContext.getAttribute('address1_line1').setValue(addressLine1);

            //необходимо для обновления адресной строки в real time
            (window.parent.document.getElementById('address1_composite') as HTMLElement).click();
            (window.parent.document.getElementById('address1_composite_compositionLinkControl_flyoutLoadingArea-confirm') as HTMLElement).click();
        }

        formContext.getAttribute('address2_addresstypecode').setValue(AddressTypeCode.Actual);
    };

    private static SetFOData(formContext: Xrm.FormContext, accountData: any): void {
        formContext.getAttribute('lmr_accounttype').setValue(AccountType.Legal);
        formContext.getAttribute('lmr_psrn').setValue(accountData.ogrn);
        formContext.getAttribute('name').setValue(accountData.fullName);
        formContext.getAttribute('lmr_iec').setValue(null);
        if (accountData.accreditation.startDate)
            formContext.getAttribute('lmr_registeredon').setValue(new Date(accountData.accreditation.startDate));

        formContext.getAttribute('address1_addresstypecode').setValue(AddressTypeCode.Legal);
        formContext.getAttribute('address1_postalcode').setValue(null);
        formContext.getAttribute('address1_stateorprovince').setValue(null);
        formContext.getAttribute('address1_line1').setValue(null);
        formContext.getAttribute('address1_city').setValue(null);
        formContext.getAttribute('address1_composite').setValue(null);
    };

    private static SetUlData(formContext: Xrm.FormContext, accountData: any): void {
        var ul = accountData.UL;
        var legalName = ul.legalName ? accountData.UL.legalName : null;
        var legalAddress = ul.legalAddress ? ul.legalAddress : null;
        var addressRF = legalAddress ? legalAddress.parsedAddressRF : null;

        if (legalName) {
            formContext.getAttribute('name').setValue(legalName.hasOwnProperty('short') ? legalName.short : legalName.full);
        }

        formContext.getAttribute('lmr_accounttype').setValue(AccountType.Legal);
        formContext.getAttribute('lmr_psrn').setValue(accountData.ogrn);
        formContext.getAttribute('lmr_iec').setValue(accountData.UL.kpp);
        formContext.getAttribute('lmr_rnnbo').setValue(accountData.UL.okpo);
        if (ul.registrationDate)
            formContext.getAttribute('lmr_registeredon').setValue(new Date(ul.registrationDate));

        formContext.getAttribute('address1_addresstypecode').setValue(AddressTypeCode.Legal);
        if (addressRF != null) {
            var addressLine1: string = Account.GetUlAddressLine(accountData);

            formContext.getAttribute('address1_line1').setValue(addressLine1);

            if (addressRF.settlement)
                formContext.getAttribute('lmr_settlement').setValue(`${addressRF.settlement.topoFullName} ${addressRF.settlement.topoValue}`);

            //необходимо для обновления адресной строки в real time
            (window.parent.document.getElementById('address1_composite') as HTMLElement).click();
            (window.parent.document.getElementById('address1_composite_compositionLinkControl_flyoutLoadingArea-confirm') as HTMLElement).click();
        }
    };

    private static GetUlAddressLine(accountData: any): string {
        var ul = accountData.UL;
        var legalAddress = ul.legalAddress ? ul.legalAddress : null;
        var addressRF = legalAddress ? legalAddress.parsedAddressRF : null;

        var line: string = '';
        var zipCode: string = '';
        var addressStateOrProvince: string = '';
        var addressCity: string = '';
        var addressLine: string = '';

        var street: string = '';

        if (addressRF.street)
            street = Account.GetEmptyString(addressRF.street.topoFullName);

        if (addressRF.regionName)
            addressStateOrProvince = `${addressRF.regionName.topoValue} ${addressRF.regionName.topoFullName}`

        if (addressRF.city)
            addressCity = `${addressRF.city.topoFullName} ${addressRF.city.topoValue}`

        if (addressRF.settlement)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.settlement.topoFullName)} ${Account.GetEmptyString(addressRF.settlement.topoValue)} `)

        if (addressRF.street && (addressRF.street == 'ПЕРЕУЛОК' || addressRF.street == 'ТУПИК' || addressRF.street == 'СКВЕР'
            || addressRF.street == 'ПРОСЕК' || addressRF.street == 'ШОССЕ' || addressRF.street == 'ПРОЕЗД' || addressRF.street == 'НАБЕРЕЖНАЯ')) {
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.street.topoValue)} ${Account.GetEmptyString(addressRF.street.topoFullName)} `)
        }
        else {
            if (addressRF.street)
                addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.street.topoFullName)} ${Account.GetEmptyString(addressRF.street.topoValue)} `)
        }


        if (addressRF.house)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.house.topoFullName)} ${Account.GetEmptyString(addressRF.house.topoValue)} `)
        if (addressRF.bulk)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.bulk.topoFullName)} ${Account.GetEmptyString(addressRF.bulk.topoValue)} `)
        if (addressRF.flat)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.flat.topoFullName)} ${Account.GetEmptyString(addressRF.flat.topoValue)}`)

        if (addressRF.zipCode)
            zipCode = addressRF.zipCode;

        addressStateOrProvince += ', ';
        addressCity += ', ';
        zipCode += ', ';

        line = zipCode + addressStateOrProvince + addressCity + addressLine;

        return line.toUpperCase();
    }

    private static GetIpAddressLine(accountData: any): string {
        var ip = accountData.IP;
        var legalAddress = ip.legalAddress ? ip.legalAddress : null;
        var addressRF = legalAddress ? legalAddress.parsedAddressRF : null;

        var line: string = '';
        var zipCode: string = '';
        var addressStateOrProvince: string = '';
        var addressCity: string = '';
        var addressLine: string = '';

        if (addressRF.regionName)
            addressStateOrProvince = `${addressRF.regionName.topoValue} ${addressRF.regionName.topoFullName}`

        if (addressRF.city)
            addressCity = `${addressRF.city.topoFullName} ${addressRF.city.topoValue}`

        if (addressRF.settlement)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.settlement.topoFullName)} ${Account.GetEmptyString(addressRF.settlement.topoValue)} `)
        if (addressRF.street)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.street.topoFullName)} ${Account.GetEmptyString(addressRF.street.topoValue)} `)
        if (addressRF.house)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.house.topoFullName)} ${Account.GetEmptyString(addressRF.house.topoValue)} `)
        if (addressRF.bulk)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.bulk.topoFullName)} ${Account.GetEmptyString(addressRF.bulk.topoValue)} `)
        if (addressRF.flat)
            addressLine = addressLine.concat(`${Account.GetEmptyString(addressRF.flat.topoFullName)} ${Account.GetEmptyString(addressRF.flat.topoValue)}`)

        if (addressRF.zipCode)
            zipCode = addressRF.zipCode;

        addressStateOrProvince += ', ';
        addressCity += ', ';
        zipCode += ', ';

        line = zipCode + addressStateOrProvince + addressCity + addressLine;

        return line.toUpperCase();
    }

    private static GetEmptyString(str?: string): string {
        if (str === null || str === undefined || str === '')
            return '';
        return str;
    }

    OnAccountTypeFieldChange(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const accountType: number = formContext.getAttribute('lmr_accounttype').getValue();
        const iecControl: Xrm.Controls.StandardControl = formContext.getControl('lmr_iec');

        if (accountType == AccountType.Legal) {
            formContext.getAttribute('lmr_iec').setRequiredLevel('required');
        }

        if (accountType == AccountType.IP) {
            formContext.getAttribute('lmr_iec').setRequiredLevel("none");
        }
    }

    public OnSave(eventContext: Xrm.Events.SaveEventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();
        const formType: XrmEnum.FormType = formContext.ui.getFormType();

        if (formType == XrmEnum.FormType.Create && eventContext.getEventArgs().getSaveMode() != XrmEnum.SaveMode.AutoSave) {

            let address1: string = formContext.getAttribute('address1_line1').getValue();
            let address2: string = formContext.getAttribute('address2_line1').getValue();
            var addressTypeCode1: number = Xrm.Page.getAttribute('address1_addresstypecode').getValue();
            var addressTypeCode2: number = Xrm.Page.getAttribute('address2_addresstypecode').getValue();

            var incorrectAddressType: boolean = true;

            if ((addressTypeCode1 == AddressTypeCode.Legal || addressTypeCode1 == AddressTypeCode.Actual)
                && (addressTypeCode2 == AddressTypeCode.Legal || addressTypeCode2 == AddressTypeCode.Actual)) {
                incorrectAddressType = false;
            }

            if (incorrectAddressType) {

                Xrm.Page.ui.setFormNotification('Не указан юридический и(или) фактический адрес организации (ИП)', 'ERROR', 'pleasewait');
                eventContext.getEventArgs().preventDefault();
                window.setTimeout(Account.SetTimeout, 5000);
            }
            if (!incorrectAddressType) {
                if (Account.GetEmptyString(address1).length == 0 || Account.GetEmptyString(address2).length == 0) {
                    Xrm.Page.ui.setFormNotification('Не заполнен юридический и(или) фактический адрес организации (ИП)', 'ERROR', 'pleasewait');

                    eventContext.getEventArgs().preventDefault();
                    window.setTimeout(Account.SetTimeout, 5000);
                }
            }
        }

        if (formType == XrmEnum.FormType.Update)
        {
            let cnt: Xrm.Controls.StandardControl;
            cnt = Xrm.Page.ui.controls.get<Xrm.Controls.StandardControl>("name");
            cnt.setDisabled(true);
        }
    }

    private static SetTimeout(): void {
        Xrm.Page.ui.clearFormNotification('pleasewait');
    }

    OnChangeNameButtonClick() : void {
        const itnNumber = Xrm.Page.getAttribute('lmr_itn').getValue();

        try {
            var response = RunAction('lmr_GetAccountInfoByItn', null,
                {
                    itn: itnNumber
                },
                false
            );

            if (response.isSuccess) {
                
                var accountData = JSON.parse(response.accountFieldsJSON)[0];
               
                let cnt: Xrm.Controls.StandardControl;
                cnt = Xrm.Page.ui.controls.get<Xrm.Controls.StandardControl>("name");                

                cnt.setDisabled(false);
                if (accountData.UL != null)
                    Account.SetUlNameData(accountData);
                else if (accountData.IP != null)
                    Account.SetIpNameData(accountData);
                else
                    Account.SetFONameData(accountData);

            }
            else {
                Xrm.Page.ui.setFormNotification('Ошибка в Контур Фокус', 'ERROR', 'pleasewait');                
                window.setTimeout(Account.SetTimeout, 5000);
            }
        }
        catch (e) {
            console.log(e);
            Xrm.Page.ui.setFormNotification('Ошибка в Контур Фокус', 'ERROR', 'pleasewait');
            window.setTimeout(Account.SetTimeout, 5000);
        }
    };

    private static SetUlNameData(accountData: any): void {
        var ul = accountData.UL;
        var legalName = ul.legalName ? accountData.UL.legalName : null;            

        if (legalName) {           
            Xrm.Page.getAttribute('name').setValue(legalName.hasOwnProperty('short') ? legalName.short : legalName.full);
        }
    }

    private static SetIpNameData(accountData: any): void {      
        Xrm.Page.getAttribute('name').setValue(accountData.IP.fio);
    }

    private static SetFONameData(accountData: any): void {       
        Xrm.Page.getAttribute('name').setValue(accountData.fullName);
    }
};

window.LMR = window.LMR || {};
window.LMR.Account = new Account();

