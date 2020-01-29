import { FormBase } from "./formBase";
import { RunAction } from "../shared/actionRunner"; 
import { OpenWebResourceInDialog } from '../shared/dialogHelper';
import { RegexValidator } from './RegexValidator'

interface ContactResponse {
    value: any;
}


export class Lead extends FormBase {

    OnLoad(eventContext: Xrm.Events.EventContext): void {
        new RegexValidator(this.AttributeRegexMap).register();
    }

    private AttributeRegexMap = [
        { attributes: ['firstname', 'lastname', 'middlename'], regex: "^[-A-Za-zéëêèîïàâäôöçüùÉЁÊÈÎÏÇÀÔÖÄÜÛØøё' А-я]{1,50}$", errorText: "Введите имя, используя только буквы, символы -' и пробел" },
        { attributes: ['mobilephone'], regex: "^(\\+|00)7\\s?\\d{10}$", errorText: 'Введите телефон в формате +7ХХХХХХХХХХ' },
        { attributes: ['emailaddress1'], regex: "^[a-zA-Z0-9!#$%&'*+\\=?^_`{|}~-]+(?:\\.[a-zA-Z0-9!#$%&'*+\\=?^_`{|}~-]+)*@(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?\\.)+[a-zA-Z0-9](?:[a-zA-Z0-9-]*[a-zA-Z0-9])?$", errorText: 'Неверный формат адреса электронной почты' }       
    ]

    private static SetMessage(message: string): void {
        Xrm.Page.ui.setFormNotification(message, 'WARNING', 'pleasewait');
        window.setTimeout(Lead.SetTimeout, 10000);
    }

    private static SetTimeout(): void {
        Xrm.Page.ui.clearFormNotification('pleasewait');
    }

    public static ValidateData(): boolean {    
        
        let firstname: string = Xrm.Page.getAttribute('firstname').getValue();
        let mobilephone: string = Xrm.Page.getAttribute('mobilephone').getValue();
        let emailaddress: string = Xrm.Page.getAttribute('emailaddress1').getValue();            

        if (firstname === "" || firstname === null) {
            Lead.SetMessage("Для обработки данных не хватает контактных данных (имя)");
            return false;
        }       

        if ((mobilephone === "" || mobilephone === null) && (emailaddress === "" || emailaddress === null)) {
            Lead.SetMessage("Для обработки данных не хватает контактных данных (телефон или почта)");
            return false;
        }       
        return true;
    } 

    private static async ProcessData() {

        if (!Lead.ValidateData())
            return;

        let lastname: string = Xrm.Page.getAttribute('lastname').getValue();
        let middlename: string = Xrm.Page.getAttribute('middlename').getValue();
        let firstname: string = Xrm.Page.getAttribute('firstname').getValue();
        let mobilephone: string = Xrm.Page.getAttribute('mobilephone').getValue();
        let emailaddress: string = Xrm.Page.getAttribute('emailaddress1').getValue();

        const response = await RunAction<ContactResponse>('lmr_SearchContacts', null, {
            firstname: firstname,
            middlename: middlename,
            lastname: lastname,
            mobilephone: mobilephone,
            emailaddress: emailaddress,
        })        

        if (response.contacts === '[]' || response.contacts.lenght === 0) {
           
            Lead.SetMessage('Создан новый контакт');
            const record = RunAction('lmr_QualifyLead', null, {
                contactid: '',
                leadid: Xrm.Page.data.entity.getId(),
                userid: Xrm.Page.context.getUserId()
            }, false);

            Lead.PostAction(record);

        }
        else {
            OpenWebResourceInDialog('lmr_/html/lead_qualify_dialog.html', response.contacts, { width: 700, height: 370 }, (value) => {

                if (value === 'underfined') {
                    Lead.SetMessage('Действие было отменено');
                    return;
                }

                const record = RunAction('lmr_QualifyLead', null, {
                    contactid: value,
                    leadid: Xrm.Page.data.entity.getId(),
                    userid: Xrm.Page.context.getUserId()
                }, false);

                Lead.PostAction(record);
            });
        }        
                   
    }

    private static PostAction(record: any) {        
        if (record.Error === '') {           
            Xrm.Page.data.refresh(false);            
            Xrm.Utility.openEntityForm('opportunity', record.opportunityid, {}, { openInNewWindow: true })
        }
        else {
            Lead.SetMessage(record.Error);
        }
    }
   
    private static QualifyByDefault() {
       
        if (!Lead.ValidateData())
            return;

        const record = RunAction('lmr_QualifyLead', null, {
            contactid: '',
            leadid: Xrm.Page.data.entity.getId(),
            userid: Xrm.Page.context.getUserId()
        }, false);
       
        Lead.PostAction(record);
    }

    private static CancelAction() {
        Lead.SetMessage('Действие было отменено');
    }

    LeadQualify(): void {
        const contactControl = Xrm.Page.getControl<Xrm.Page.LookupControl>('parentcontactid');
        
        if (contactControl) {
            let lookUpAttribute: Xrm.Attributes.LookupAttribute = contactControl.getAttribute();
            let lookupValue = lookUpAttribute.getValue();
            if (lookupValue && lookupValue.length) {
                var parentcontactId = lookupValue[0].id;

                const record = RunAction('lmr_QualifyLead', null, {
                    contactid: parentcontactId,
                    leadid: Xrm.Page.data.entity.getId(),
                    userid: Xrm.Page.context.getUserId()
                }, false);
                 
                Lead.PostAction(record);               
            }
            else {
                Lead.ProcessData();
            }
        }
        else {
            Lead.ProcessData();          
        }    
    };
}

window.LMR = window.LMR || {};
window.LMR.Lead = new Lead();