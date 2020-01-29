import * as React from 'react'
import webapi from 'xrm-webapi-client'
import Build from 'odata-query'
import { List, Button, message } from 'antd'
import { RunAction } from '../../shared/actionRunner'
import { FileDropArea } from '../common/file-drop'
import * as $ from 'jquery'
import  { GetId, GetLogicalName, SubscribeOnFirstSave} from '../../shared/webResourceHelper'
import * as _ from 'lodash';
import * as fileManager from './fileManager';
import { toTrimBrackets, isEmpty } from '../../shared/common';
import FileItem from './fileItem';

interface FileStoreState {
    data: FileEntity[],
    loading: boolean;
    ready: boolean;
    maxSize: number;
    blockedExtensions: string[],
    userId: string
}
interface FileEntity {
    lmr_fileid: string,
    lmr_mimetype: string,
    lmr_name: string,
    lmr_size: number,
    _ownerid_value: any,
    isDelete: boolean
}
interface OrganizationEntity {
    maxuploadfilesize: number,
    blockedattachments: string
}
interface AccessRight {
    AccessRights: string
}
export class FileStore extends React.Component<{}, FileStoreState> {
    constructor(props: any) {
        super(props)
        
        this.state = {
            data: [],
            loading: false,
            maxSize: 0,
            ready: true,
            blockedExtensions: [],
            userId: toTrimBrackets(parent.Xrm.Page.context.userSettings.userId)
        }
    }
    async componentDidMount() {
        SubscribeOnFirstSave(this.refresh.bind(this))        
        await this.refresh();
        const organization = (await webapi.Retrieve({ entityName: 'organization', queryParams: Build({ select: ['maxuploadfilesize', 'blockedattachments']}) })).value[0] as OrganizationEntity;
        this.setState({maxSize:  organization.maxuploadfilesize, blockedExtensions: organization.blockedattachments.split(';')});
    }
    setData = (id: string) => {
        const { data } = this.state;
        this.setState({
            data: [...data].filter(f => f.lmr_fileid != id)
        });
    }
    async refresh() {
        this.setState({loading: true, ready: !!GetId()});        
        try {
            let fileEntities = (await webapi.Retrieve({ entityName: 'lmr_file', queryParams: Build({
                select: ['lmr_fileid', 'lmr_mimetype', 'lmr_name', 'lmr_size', '_ownerid_value'],
                filter: { "lmr_entitytype": GetLogicalName(), 'lmr_entityid': GetId()!, 'statuscode': 1 }
            })})).value as FileEntity[];
            let promises = fileEntities.map(async (file: FileEntity) => {
                return this.checkUserAccesses(this.state.userId, file.lmr_fileid);
            }, []);         
            Promise.all(promises).then((result: AccessRight[]) => {
                let concatRes: FileEntity[] = fileEntities.reduce((acc: FileEntity[], field: FileEntity, i: number) => {
                    field.isDelete = !isEmpty(result[i].AccessRights) && _.includes(result[i].AccessRights, 'WriteAccess') //result[i].AccessRights.includes('WriteAccess') 
                    ? true : false;
                    acc.push(field);
                    return acc;
                }, []);
                this.setState({data: concatRes, loading: false})
        });
        } catch (e) {
            message.error("Не удалось обновить список файлов");
            console.error(e);
        } 
    }
    checkUserAccesses = (userId: string, fileId: string) => {
            let request = new webapi.Requests.Request().with({
                name: 'RetrievePrincipalAccess', method: 'GET',bound: true,
                entityName: "systemuser", entityId: userId,
                urlParams: { Target: `{"@odata.id": "lmr_files(${fileId})"}`}
            });
            return webapi.Execute(request);
    }
    uploadFiles(files: FileList) {
        for (let i = 0; i < files.length; i++)
            this.uploadFile(files[i])
    }
    async uploadFile(file: File) {
        if (!this.validateFileSize(file) || !this.validateFileExtension(file)) {
            this.setState({loading: false})
            return;
        }
        this.setState({loading: true});
        try {
            const data = await fileManager.ReadFile(file);
            await RunAction('lmr_createAttachment', null, {
                fileName: file.name,
                mimeType: file.type,
                entityType: GetLogicalName(),
                entityId: GetId(),
                documentBody: data,
                ownerid: this.state.userId
            })
        } catch(e) {
            message.error(`Не удалось загрузить файл ${file.name}`)
        }
        this.refresh()
    }
    validateFileSize(file: File): boolean {
        if (file.size > this.state.maxSize) {
            message.error(`Размер файла ${file.name} превышает максимально допустимый в ${this.state.maxSize} байт`)
            return false;
        }
        return true;
    }
    validateFileExtension(file: File): boolean {
        const matchResult = (/\.([a-zA-Z]+)$/g).exec(file.name);
        if (matchResult === null)
            return true;
        const fileExtension = matchResult[1];
        if (_.includes(this.state.blockedExtensions, fileExtension))
        {
            message.error(`Файл с расширением .${fileExtension} запрещен для загрузки`)
            return false;
        }
        return true;
    }
    render() {
        const { ready, data, loading } = this.state;
        if (!ready)
            return <div>Запись еще не сохранена</div>
        return <div hidden={!GetId()}>
            <input 
                type="file" 
                style={{visibility: 'hidden', float: 'left', height: 0}} 
                id="input-file" 
                onChange={e => this.uploadFiles(e.target.files!)} 
                multiple 
            />
            <div className="controls">
                <Button onClick={() => $('#input-file').click() } icon="upload">Выберите файлы</Button>
                <FileDropArea onDrop={this.uploadFiles.bind(this)}>
                    ...или перетащите сюда
                </FileDropArea>
            </div>
            <List bordered 
                renderItem={
                    (item: FileEntity) => <FileItem fItem={item} setData={this.setData} />
                } 
                loading={loading} 
                dataSource={data} 
                size="default" 
                rowKey="lmr_fileid" 
                locale={{emptyText: "Нет файлов"}}/>
        </div>
    }  
}