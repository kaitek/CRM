import * as React from "react";
import { Button, Table } from 'antd';
import { DialogLayout } from "../common/dialog-layout";
import { GetDataParameter } from '../../shared/webResourceHelper'
import Column from "antd/lib/table/Column";
import { CloseThisDialog } from "../../shared/dialogHelper";


interface DuplicateAccount {
    accountid: string,
    name: string,
    itn: string,
    iec: string
}

interface State {
    overflow: boolean,
    hoveringMouseOnLink: boolean,
    duplicateAccounts: DuplicateAccount[]
}

class DuplicateAccountsList extends React.Component<object, State>
{
    constructor(props: object) {
        super(props);

        const duplicateAccountsJson: any = GetDataParameter();
        const duplicateAccounts = JSON.parse(duplicateAccountsJson)
            .map(function (account: any, index: number) {
                var formattedAccount: DuplicateAccount = {
                    accountid: account.accountid,
                    name: account.name,
                    itn: account.lmr_itn,
                    iec: account.lmr_iec
                }

                return formattedAccount;
            });

        this.state = {
            hoveringMouseOnLink: false,
            overflow: duplicateAccounts.length > 1,
            duplicateAccounts: duplicateAccounts
        };
        this.renderAccountName = this.renderAccountName.bind(this);
    }

    renderAccountName(text: string, row: DuplicateAccount): React.ReactNode {
        var linkStyle = {
            color: "#004F1C",
            textDecoration: "none"
        };

        if (this.state.hoveringMouseOnLink)
            linkStyle = {
                color: "#004F1C",
                textDecoration: "underline"
            };

        return (
            <a style={linkStyle} href="#"
                onClick={() => {
                    Xrm.Utility.openEntityForm("account", row.accountid, {}, { openInNewWindow: true })
                }}
                onMouseEnter={() => {
                    this.setState({ hoveringMouseOnLink: !this.state.hoveringMouseOnLink });
                }}
                onMouseLeave={() => {
                    this.setState({ hoveringMouseOnLink: !this.state.hoveringMouseOnLink })
                }}
            >
                {text}
            </a>
        );
    }

    render() {
        return (
            <Table bodyStyle={{ height: "200px", overflowY: "auto" }} dataSource={this.state.duplicateAccounts} pagination={{ hideOnSinglePage: true }}
                size="small"
                onRow={(record) => {
                    return {
                        onDoubleClick: () => {
                            Xrm.Utility.openEntityForm("account", record.accountid, {}, { openInNewWindow: true })
                        }
                    }
                }}
            >
                <Column render={this.renderAccountName} title="Название организации" dataIndex="name" key="name" />
                <Column title="ИНН" dataIndex="itn" key="itn" width="120px" />
                <Column title="КПП" dataIndex="iec" key="iec" width="120px" />
            </Table>
        );
    }
}

export class Dialog extends React.Component {
    private close() {
        CloseThisDialog();
    }

    render() {
        return (
            <DialogLayout header="Найдены записи с эквивалентным ИНН"
                top={<DuplicateAccountsList />}
                bottom={<Button type="primary" onClick={this.close} block>ОК</ Button>} />
        );
    }
}