import * as React from 'react'
import * as _ from 'lodash'
import { DialogLayout } from '../common/dialog-layout';
import { CloseThisDialog } from '../../shared/dialogHelper';
import * as wrHelper from '../../shared/webResourceHelper';
import { RunAction } from '../../shared/actionRunner'
import { WrappedLoginPasswordForm, SubmitEventArgs } from './form'

import { Tabs, Divider, Spin, message } from 'antd';

import { SsoServices } from '../common/sso'
interface ComponentState {
    loading: boolean;
}
export class UserSettingsForm extends React.Component<any, ComponentState> {
    constructor(props: any) {
        super(props);
        this.state = {
            loading: false
        }
    }
    async handleSubmit(e: SubmitEventArgs) {
        const systemUserId = wrHelper.GetDataParameter()!.replace(/{|}/g, '').toUpperCase();
        
        try {
            this.setState({loading: true});
            await RunAction('lmr_CreateUserSettings', null, {
                SystemUser: {
                    "@odata.type": "Microsoft.Dynamics.CRM.systemuser",
                    systemuserid: systemUserId
                },
                ServiceCode: e.context,
                Login: e.values.login,
                Password: e.values.password,
            });
            CloseThisDialog();
        }
        catch (error) {
            message.error("Произошла ошибка при сохранении учетных данных");
            console.error(error);
        }
        this.setState({loading: false})
    }
    render() {
        return <DialogLayout header="Учётные данные USD"> 
                <Tabs tabPosition="left">
                    {SsoServices.map(service => <Tabs.TabPane key={service.code} tab={service.friendlyName}>
                            <Spin spinning={this.state.loading}>
                                <h2>{service.friendlyName}</h2>
                                <Divider />
                                <WrappedLoginPasswordForm context={service.code} onSubmit={this.handleSubmit.bind(this)}/>
                            </Spin>
                        </Tabs.TabPane> 
                    )}
                </Tabs>
            </DialogLayout>
    }
}