import * as React from 'react'
import webapi from 'xrm-webapi-client'
import Build from 'odata-query'
import { List, Icon, Divider, Button, Spin, message } from 'antd'
import { RunAction } from '../../shared/actionRunner'
import { FileDropArea } from '../common/file-drop'
import * as $ from 'jquery'
import { GetId, GetLogicalName, SubscribeOnFirstSave } from '../../shared/webResourceHelper'
import * as _ from 'lodash';
import * as fileManager from './fileManager'
import { DialogLayout } from '../common/dialog-layout';

interface FileStoreState {
    data: FileEntity[],
    loading: boolean;
    ready: boolean;
    maxSize: number;
    allowedExtensions: string[]
}
interface FileEntity {
    lmr_fileid: string,
    lmr_mimetype: string,
    lmr_name: string,
    lmr_size: number
}
interface OrganizationEntity {
    maxuploadfilesize: number,
    blockedattachments: string
}
interface DownloadFileResult {
    fileName: string;
    mimeType: string;
    documentBody: string;
}

export class ImportLogins extends React.Component<{}, FileStoreState> {
    constructor(props: any) {
        super(props)

        this.state = {
            data: [],
            loading: false,
            maxSize: 0,
            ready: true,
            allowedExtensions: []
        }
    }
    async componentDidMount() {
        //SubscribeOnFirstSave(this.refresh.bind(this))
        await this.refresh();
        const organization = (await webapi.Retrieve({ entityName: 'organization', queryParams: Build({ select: ['maxuploadfilesize', 'blockedattachments'] }) })).value[0] as OrganizationEntity;
        this.setState({ maxSize: organization.maxuploadfilesize, allowedExtensions: ["xlsx"] });
    }
    render() {
        if (!this.state.ready)
            return <div>Запись еще не сохранена</div>
        return <DialogLayout header="Учётные данные USD"> 
            <Spin spinning={this.state.loading}>
                <div hidden={false} className="importLogins">
                    <div className="importLoginsLabel">Загрузите файл логинов и паролей с расширением .xlsx</div>
                    <input type="file" style={{ visibility: 'hidden', float: 'left', height: 0 }} id="input-file" onChange={e => this.uploadFiles(e.target.files!)} />
                    <div className="controls">
                        <Button onClick={() => $('#input-file').click()} icon="upload">Выберите файлы</Button>
                        <FileDropArea onDrop={this.uploadFiles.bind(this)}>...или перетащите сюда</FileDropArea>
                    </div>
                </div>
            </Spin>     
       </DialogLayout>
    }
    async refresh() {
        this.setState({ loading: true, ready: true })
        try {
            const fileEntities = (await webapi.Retrieve({
                entityName: 'lmr_file', queryParams: Build({
                    select: ['lmr_fileid', 'lmr_mimetype', 'lmr_name', 'lmr_size'],
                    filter: { "lmr_entitytype": "importLogins", 'lmr_entityid': '00000000-0000-0000-0000-000000000000' }
                })
            })).value as FileEntity[];
            this.setState({ data: fileEntities, loading: false })
        } catch (e) {
            message.error("Не удалось обновить список файлов");
            console.error(e);
        }
    }
    
    

    getSizeString(size: number) {
        const mesuarments = ["Б", "кБ", "МБ", "ГБ"];
        let i = 0;
        for (; size / 1024 > 1; size /= 1024, i++)
            ;

        return `${Math.floor(size) === size ? size : (size).toFixed(1)} ${mesuarments[i]}`;
    }
    uploadFiles(files: FileList) {
        for (let i = 0; i < files.length; i++)
            this.uploadFile(files[i])
    }
    async uploadFile(file: File) {
        if (!this.validateFileSize(file) || !this.validateFileExtension(file)) {
            this.setState({ loading: false })
            return;
        }
        this.setState({ loading: true });
        try {
            const data = await fileManager.ReadFile(file);
            await RunAction('lmr_ImportLogins', null, {                
                documentBody: data
            })
            message.info(`Данные успешно загружены`)
        } catch (e) {
            message.error(`Не удалось загрузить файл ${file.name}`)
        }  
        this.setState({ loading: false });
        
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
        if (!_.includes(this.state.allowedExtensions, fileExtension)) {
            message.error(`Файл с расширением .${fileExtension} запрещен для загрузки`)
            return false;
        }
        return true;
    }
    
}