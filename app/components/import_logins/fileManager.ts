import * as $ from 'jquery'
import * as base64 from 'base64-js'

export function ReadFile(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
        const fileReader = new FileReader();
        fileReader.onload = () => {
            resolve(base64.fromByteArray(new Uint8Array(fileReader.result as ArrayBuffer)))
        }
        fileReader.onerror = () => reject(fileReader.error)
        fileReader.readAsArrayBuffer(file);
    })
}
export function SaveFile(fileName: string, mimeType: string, data: string): void {
    const blob = new Blob([base64.toByteArray(data)], { type: mimeType });
    if (navigator.msSaveBlob)
        navigator.msSaveBlob(blob, fileName);
    else {
        const blobUrl = URL.createObjectURL(blob);
        $('<a>').attr('download', fileName).attr('href', blobUrl).get(0).click();
        URL.revokeObjectURL(blobUrl);
    }
}