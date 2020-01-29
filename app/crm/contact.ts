import webapi from 'xrm-webapi-client'
import { RegexValidator } from './RegexValidator'
import { FormBase } from './formBase'
import { isEmpty } from '../shared/common';
import { FetchXML } from './fetchXML';
import { CustomerTypes } from "../shared/Enums";

export class Contact extends FormBase {     

    OnLoad(context: Xrm.Events.EventContext): void {        
        const formContext = context.getFormContext();        

        Contact.SetCustomerTypetByUserRole();
        Contact.B2BUserRoleAction(context);

        new RegexValidator(this.AttributeRegexMap).register();

        formContext.getAttribute("birthdate").addOnChange(this.CheckLegalCustomerAdultHood);
        formContext.data.entity.addOnSave(this.OnSave);
    }

    OnSave(context: Xrm.Events.EventContext): void {        
        if (Xrm.Page.data.entity.getId() == '') {
            let cnt: Xrm.Controls.OptionSetControl;
            cnt = context.getFormContext().ui.controls.get<Xrm.Controls.OptionSetControl>("lmr_customertype");
            if (cnt != null) {
                console.log('Contact.setDisabled');
                cnt.setDisabled(true);
            }
        }
    }
   

    private AttributeRegexMap = [
        { attributes: ['firstname', 'lastname', 'middlename'], regex: "^[-A-Za-zéëêèîïàâäôöçüùÉЁÊÈÎÏÇÀÔÖÄÜÛØøё' А-я]{1,50}$", errorText: "Введите имя, используя только буквы, символы -' и пробел" },
        { attributes: ['mobilephone', 'telephone2'], regex: "^(\\+|00)7\\s?\\d{10}$", errorText: 'Введите телефон в формате +7ХХХХХХХХХХ' },
        { attributes: ['emailaddress1'], regex: "^[a-zA-Z0-9!#$%&'*+\\=?^_`{|}~-]+(?:\\.[a-zA-Z0-9!#$%&'*+\\=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$", errorText: 'Неверный формат адреса электронной почты' },
        { attributes: ['address1_postalcode', 'address2_postalcode'], regex: "^\\d{6}$|^$", errorText: 'Индекс должен состоять из 6 цифр' }
    ]

    private CheckLegalCustomerAdultHood(eventContext: Xrm.Events.EventContext): void {
        const formContext: Xrm.FormContext = eventContext.getFormContext();

        const customerType: number = formContext.getAttribute("lmr_customertype").getValue();

        if (customerType === CustomerTypes.Legal) {
            const adultHoodValue: number = 18;
            const notification: Xrm.Controls.AddControlNotificationOptions =
            {
                messages: ["Вы пытаетесь создать Контакт моложе 18 лет"],
                notificationLevel: "ERROR",
                uniqueId: "InvalidBirthdateId"
            };

            var birhDateControl: Xrm.Controls.DateControl = formContext.getControl("birthdate");
            var birhDateTime: number = birhDateControl.getAttribute().getValue().getTime();
            var difference: number = new Date(Date.now() - birhDateTime).getFullYear() - 1970;


            if (difference < adultHoodValue)
                birhDateControl.addNotification(notification);
            else
                birhDateControl.clearNotification(notification.uniqueId);
        }
    }

    private static B2BUserRoleAction(context: Xrm.Events.EventContext): void {

        if (Xrm.Page.data.entity.getId() == '') {

            let cnt: Xrm.Controls.OptionSetControl;
            cnt = context.getFormContext().ui.controls.get<Xrm.Controls.OptionSetControl>("lmr_customertype");
            if (cnt == null)
                return;

            var managerRoleConfig: any = webapi.Retrieve({
                async: false,
                entityName: "lmr_config",
                queryParams: "?$select=lmr_value&$filter=lmr_name eq 'B2B_Manager_Role'&$top=1"
            });            
            if (managerRoleConfig.value.length > 0) {
                if (Contact.CheckUserRoleExistence(managerRoleConfig.value[0].lmr_value))
                    cnt.setDisabled(false);
            }
            else {
                alert("Отсутствует параметр конфигурации: B2B_Manager_Role");
            }
        }
    }

    private static SetCustomerTypetByUserRole(): void {
        if (Xrm.Page.data.entity.getId() == '') {
            var managerRoleConfig: any = webapi.Retrieve({
                async: false,
                entityName: "lmr_config",
                queryParams: "?$select=lmr_value&$filter=lmr_name eq 'B2B_Manager_Role'&$top=1"
            });

            if (managerRoleConfig.value.length > 0) {
                if (Contact.CheckUserRoleExistence(managerRoleConfig.value[0].lmr_value))
                    Xrm.Page.getAttribute("lmr_customertype").setValue(CustomerTypes.Legal);
            }
            else {
                alert("Отсутствует параметр конфигурации: B2B_Manager_Role");
            }
        }

        if (Xrm.Page.getAttribute("lmr_customertype").getValue() === CustomerTypes.Legal) {
            Xrm.Page.getAttribute("mobilephone").setRequiredLevel("none");
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

    ShowDeactivateIfLegal(): boolean {
        const type = Xrm.Page.getAttribute<Xrm.Page.Attribute>("lmr_customertype");
        return !isEmpty(type) && type.getValue() == CustomerTypes.Legal || !isEmpty(type) && !type.getValue() ?
            true : false;
    }
}



window.LMR = window.LMR || {}
window.LMR.Contact = new Contact();