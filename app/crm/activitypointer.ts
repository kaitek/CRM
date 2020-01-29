import { FormBase } from "./formBase";
import { CrmConfigHelper } from "../shared/crmConfigHelper";
import webapi from "xrm-webapi-client";
import { TaskStateCode, TaskStateOpen, TaskStateFinished, TaskStateCanceled, TaskStatePlanned } from "../shared/Enums";

export class ActivityPointerView {
    private TimeIsOveringValue: string | undefined;

    constructor() {
        ActivityPointerView.prototype.TimeIsOveringValue = CrmConfigHelper.GetValue("TimeIsOveringValue");
    }

    public async MarkCriticalTasksHandler(rowDataJson: string): Promise<void> {
        if (rowDataJson) {
            let rowData: any = JSON.parse(rowDataJson);

            if (rowData.statecode) {

                let res: boolean = false;

                if (rowData.statecode === TaskStateOpen) {
                    res = true;
                    ActivityPointerView.prototype.StateMarkCriticalTasks(rowData, TaskStateCode.open);
                }

                if (rowData.statecode === TaskStateFinished) {
                    res = true;
                    ActivityPointerView.prototype.StateMarkCriticalTasks(rowData, TaskStateCode.finished);
                }

                if (rowData.statecode === TaskStateCanceled) {
                    res = true;
                    ActivityPointerView.prototype.StateMarkCriticalTasks(rowData, TaskStateCode.canceled);
                }

                if (rowData.statecode === TaskStatePlanned) {
                    res = true;
                    ActivityPointerView.prototype.StateMarkCriticalTasks(rowData, TaskStateCode.planned);
                }

                if (res)
                    return;
            }


            if (rowData.scheduledend_Value) {
                await webapi.Retrieve({
                    async: true,
                    entityName: 'activitypointer',
                    entityId: rowData.RowId,
                    queryParams: '?$select=statecode'
                })
                    .then((response: any) => ActivityPointerView.prototype.MarkCriticalTasks(rowData, response))
                    .catch((exp: any) => console.log(exp));
            }
        }
    };

    private MarkCriticalTasks(rowData: any, response: any): void {
        const defaultTimeIsOveringValue = 7;
        const timeIsOveringValueFromDB = parseInt(ActivityPointerView.prototype.TimeIsOveringValue as string);
        const timeIsOveringValue = isNaN(timeIsOveringValueFromDB) ? defaultTimeIsOveringValue : timeIsOveringValueFromDB;

        let currentDate = new Date();
        let scheduledendDate = new Date(rowData.scheduledend_Value)
        let difference = new Date(
            (new Date(scheduledendDate))
                .setDate(scheduledendDate.getDate() - timeIsOveringValue)
        );

        if (response.statecode && response.statecode == TaskStateCode.finished)
            ActivityPointerView.prototype.SetCriticalMarkerInScheduledendCell(rowData, 'rgb(75, 168, 66)');
        else if (scheduledendDate < currentDate)
            ActivityPointerView.prototype.SetCriticalMarkerInScheduledendCell(rowData, 'red');
        else if (difference <= currentDate)
            ActivityPointerView.prototype.SetCriticalMarkerInScheduledendCell(rowData, 'orange');
    }

    private StateMarkCriticalTasks(rowData: any, statecode: TaskStateCode): void {
        const defaultTimeIsOveringValue = 7;
        const timeIsOveringValueFromDB = parseInt(ActivityPointerView.prototype.TimeIsOveringValue as string);
        const timeIsOveringValue = isNaN(timeIsOveringValueFromDB) ? defaultTimeIsOveringValue : timeIsOveringValueFromDB;

        let currentDate = new Date();
        let scheduledendDate = new Date(rowData.scheduledend_Value)
        let difference = new Date(
            (new Date(scheduledendDate))
                .setDate(scheduledendDate.getDate() - timeIsOveringValue)
        );

        if (statecode == TaskStateCode.finished)
            ActivityPointerView.prototype.SetCriticalMarkerInScheduledendCell(rowData, 'rgb(75, 168, 66)');
        else if (scheduledendDate < currentDate)
            ActivityPointerView.prototype.SetCriticalMarkerInScheduledendCell(rowData, 'red');
        else if (difference <= currentDate)
            ActivityPointerView.prototype.SetCriticalMarkerInScheduledendCell(rowData, 'orange');
    }

    private SetCriticalMarkerInScheduledendCell(rowData: any, color: string): void {
        const xrmUI = Xrm.Page.ui;
        let row: HTMLHtmlElement | null = document.querySelector(`tr[oid="${rowData.RowId}"]`);

        //в случае запуска скрипта на форме - так как для скриптов выделяется отдельный frame
        if (!row && xrmUI && xrmUI.getFormType())
            row = parent.document.querySelector(`tr[oid="${rowData.RowId}"]`);

        if (row) {
            const cell: HTMLHtmlElement | null = row.querySelector('td[colname="scheduledend"]');

            if (cell) {
                const markerDiv: HTMLHtmlElement | null = cell.querySelector('div[class="ms-crm-Grid-DataColumn-ImgItem"]')
                const spanDiv: HTMLHtmlElement | null = cell.querySelector(`span[aria-label="${rowData.scheduledend}"]`);

                if (markerDiv) {
                    markerDiv.style.top = "4px";
                    markerDiv.style.width = "9px";
                    markerDiv.style.height = "9px";
                    markerDiv.style.marginLeft = "5px";
                    markerDiv.style.marginRight = "5px";
                    markerDiv.style.borderRadius = "50%";
                    markerDiv.style.backgroundColor = color;
                }

                if (spanDiv) {
                    spanDiv.style.color = color;
                }
            }
        }
    };
}



window.LMR = window.LMR || {};
window.LMR.ActivityPointerView = new ActivityPointerView(); 