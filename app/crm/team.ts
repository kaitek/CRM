import { FormBase } from "./formBase";


export class Team extends FormBase {
    OnLoad(): void {
        //Team.AddDepartmentCustomLookupFilter();
    };
    OnStoreChange() {
        //Team.AddDepartmentCustomLookupFilter();
    };

    //т.к. удалили связь рабочей группы с отделом - данный код теряет смысл. оставлю на всякий случай, если понадобится добавить это поле через связь с магазином

    //public static AddDepartmentCustomLookupFilter() {     
    //    if (Xrm.Page.getControl("lmr_storeid") != null && Xrm.Page.getControl("lmr_storeid") != undefined) {
    //        var control = Xrm.Page.getControl("lmr_departmentid") as Xrm.Controls.LookupControl;
    //        var storeId = Xrm.Page.getAttribute("lmr_storeid").getValue();
    //        if (storeId) {
    //            var viewId = "{4f810f75-0a9a-4528-8866-92471bb3735b}";
    //            var fetchXml = '<fetch version="1.0" output-format="xml-platform" mapping="logical" distinct="false" ><entity name="lmr_department" ><attribute name="lmr_name" /><order attribute="lmr_name" descending="false" /><link-entity name="lmr_lmr_store_lmr_department" from="lmr_departmentid" to="lmr_departmentid" visible="false" intersect="true" ><link-entity name="lmr_store" from="lmr_storeid" to="lmr_storeid" alias="ab" ><filter type="and" ><condition attribute="lmr_storeid" operator="eq" value="' +
    //                storeId[0].id + '" /></filter></link-entity></link-entity></entity></fetch>';
    //            var layoutXml =
    //                '<grid name="resultset" object="10147" jump="lmr_name" select="1" preview="0" icon="1">'
    //                + '<row name="result" id="lmr_departmentid">'
    //                + '<cell name="lmr_name" width="300" />'
    //                + '</row>'
    //                + '</grid>';

    //            control.addCustomView(viewId, "lmr_department", 'Доступные отделы', fetchXml, layoutXml, true);
    //        }
    //    }
    //};
}

window.LMR = window.LMR || {};
window.LMR.Team = new Team();

//добавь в crm.ts следующее export * from './crm/team';