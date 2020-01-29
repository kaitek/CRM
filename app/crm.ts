///<reference types="xrm"/>
///<reference types="jquery"/>
declare global {
    namespace Xrm {
        interface XrmStatic {
            Internal: any;
            DialogOptions: any;
        }
    }
    namespace Mscrm.Utilities {
        function setReturnValue(value: any): void;
    }
}
export * from './crm/account';
export * from './crm/incident';
export * from './crm/contact';
export * from './crm/systemUser';
export * from './crm/news';
export * from './crm/team';
export * from './crm/powerOfAttorney';
export * from './crm/opportunity';
export * from './crm/contract';
export * from './crm/quote';
export * from './crm/task';
export * from './crm/lead';
export * from './crm/activitypointer';
export * from './crm/measuretask';
