import * as React from 'react';
import { Button, Spin, message } from 'antd';
import { RunAction } from '../../shared/actionRunner'
import { GetDataParameter } from '../../shared/webResourceHelper'
import { CloseThisDialog } from '../../shared/dialogHelper';
import { DialogLayout } from '../common/dialog-layout'

export interface State {
    balance?: number;
    loading: boolean;
    error?: string;
}
interface BalanceResponse {
    customerBalance: number;
}
export class Dialog extends React.Component<object, State> {
    constructor(props: object) {
        super(props);
        this.state = { balance: 0, loading: true };
    }

    
    render() { 
        return (
            <DialogLayout header="Текущий баланс" bottom={<Button type="primary" onClick={this.Close} block>OK</Button>}>
                <Spin spinning={this.state.loading}>
                    <div className="balance">{this.state.balance && this.state.balance.toLocaleString()}<span> ₽</span></div>
                </Spin>
               
            </DialogLayout>
            );
    }

    async componentDidMount() {
        const customerNumber = GetDataParameter();
        try {
            const result = await RunAction<BalanceResponse>('lmr_RetrieveBalanceByNumber', null, {
                customerNumber: customerNumber
            })
            this.setState({ balance: result.customerBalance, loading: false });


        } catch(error) {
            this.setState({ loading: false, balance: undefined, error: 'Ошибка при запросе баланса' });
            message.error('Ошибка при запросе баланса', 0)
            console.error(error);
        }
    }


    private Close() {
        CloseThisDialog();
    }
}

