import * as React from 'react';
import { List, Icon, Divider, Button,Popconfirm, message } from 'antd';
import { RunAction } from '../../shared/actionRunner';
import * as fileManager from './fileManager';

interface FileEntity {
    lmr_fileid: string,
    lmr_mimetype: string,
    lmr_name: string,
    lmr_size: number,
    _ownerid_value: string,
    isDelete: boolean
}
interface DownloadFileResult {
    fileName: string;
    mimeType: string;
    documentBody: string;
}
interface FItem {
    fItem: FileEntity,
    setData: Function
}
const ListItem = List.Item;

const getSizeString = (size: number) => {
    const mesuarments = ["Б", "кБ", "МБ", "ГБ"];
    let i = 0;
    for (; size/1024 > 1; size /= 1024, i++)
        ;

    return `${Math.floor(size) === size ? size : (size).toFixed(1)} ${mesuarments[i]}`;
}
const getIconNameByFileType = (mimeType: string): string =>{
    const defaultIcon = 'file';
    if (!mimeType)
        return defaultIcon;
    const typeIconMap: {[type: string]: string} = {
        'text/plain': 'file-text',
        'application/pdf': 'file-pdf',
        'application/msword': 'file-word',
        'application/vnd.openxmlformats-officedocument.wordprocessingml.document': 'file-word',
        'application/vnd.ms-excel': 'file-excel',
        'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': 'file-excel',
        'application/vnd.ms-powerpoint': 'file-ppt',
        'application/vnd.openxmlformats-officedocument.presentationml.presentation': 'file-ppt',
        'text/markdown': 'file-markdown',
        'image': 'picture',
        'video': 'video-camera',
        'audio': 'sound'
    }
    const key = Object.keys(typeIconMap).filter(k => mimeType.startsWith(k))[0]
    return key && typeIconMap[key] || defaultIcon;
}
const downloadFile = async(id: string) => {
    try {
        const result = await RunAction<DownloadFileResult>('lmr_downloadAttachment', null, { id });
        fileManager.SaveFile(result.fileName, result.mimeType, result.documentBody);
    } catch (e) {
        message.error('Не удалось загрузить файл');
        console.error(e);
    }
}
const deactivateFile = (id: string, setData: Function) => {   
    try {   
        Xrm.WebApi.updateRecord('lmr_file',id, {['statecode']: 1,['statuscode']: 2})
        setData(id);
    } catch (e) {
        console.error(e);
    }        
}
const actions = (fItem: FileEntity, setData: Function) => {
    let arr = [ <Button icon="download" onClick={() => downloadFile(fItem.lmr_fileid)} />];
    if (fItem.isDelete) {
        arr.push(<Popconfirm
                    title="Уверены?" onConfirm={() => deactivateFile(fItem.lmr_fileid, setData)}
                    cancelText="нет" okText="да" placement="leftTop">
                    <Button icon='delete' href="javascript:;"/>
                 </Popconfirm>);
    }
    return arr;
}
const FileItem = (props: FItem) => {
    const { fItem, setData } = props;
    return (<ListItem 
                actions={actions(fItem,setData)} key={fItem.lmr_fileid}>
                    <div className="item" >
                        <Icon className="icon" type={getIconNameByFileType(fItem.lmr_mimetype)} />
                        <Divider type="vertical" className="divider"/>
                        <div>
                            <div className="file-name">{fItem.lmr_name}</div>
                            <div className="file-size">{getSizeString(fItem.lmr_size)}</div>
                        </div>
                    </div> 
            </ListItem>)
}

export default FileItem;