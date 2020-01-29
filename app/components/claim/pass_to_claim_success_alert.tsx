import * as React from 'react';
import { Button } from 'antd';
import { CloseThisDialog } from '../../shared/dialogHelper';
import { DialogLayout } from '../common/dialog-layout'


export class Dialog extends React.Component<object, {}> {
    constructor(props: object) {
        super(props);
    }

    render() {
        return (
            <DialogLayout
                header="Ваше обращение передано в отдел Claim"
                bottom={<Button type="primary" onClick={this.Close} block>OK</Button>}>
            </DialogLayout>
        );
    }

    private Close() {
        CloseThisDialog();
    }
}

