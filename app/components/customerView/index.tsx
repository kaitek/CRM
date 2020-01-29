import * as React from "react";
import { Table, Button, Input } from 'antd';
import { RunAction } from '../../shared/actionRunner'
import { CommonSorter, CommonFilter, TextBoxDropdown } from '../common/antd/utils'
import { OpenLink } from '../common/usd'
import { GetId, SubscribeOnFirstSave } from '../../shared/webResourceHelper'
import * as moment from 'Moment'

export class CustomerView extends React.Component<any, CustomerBoardComponentState> {
    constructor(props: any) {
        super(props);
        this.state = { orders: null }
    }

    async componentDidMount() {
        SubscribeOnFirstSave(() => this.load())
        await this.load();
    }
    async load() {
        try {
            const recordId = parent.Xrm.Page.data.entity.getId()
            if (!recordId) {
                return;
            }
            const result = await RunAction('lmr_GetCustomerOrders', {
                entityName: 'contact',
                id: recordId
            })
            const orders = JSON.parse(result['result'] as string).documents;
            this.setState({ orders: orders })
        } catch (e) {
            console.error(e)
        }
    }
    render() {

        const data: any = this.state.orders;

        return <Table dataSource={data}
            size="middle" pagination={{ pageSize: 5 }}
            rowKey={(rec: any) => rec.orderId || rec.receiptId}
            rowClassName={() => "order-row"}
            loading={!this.state.orders}
            className="order-table"
            locale={{ emptyText: 'Нет заказов' }}

            expandedRowRender={(record: any) =>
                <Table dataSource={record.items} pagination={false} size="small" rowKey={(record: any) => record.id || record.code} className="item-table" rowClassName={() => "item-row"} >
                    <Table.Column title="Название" dataIndex="name" />
                    <Table.Column title="Артикул" dataIndex="code" render={(text, item: any) => window.IsUSD ? <a href="#" onClick={() => this.onArticleClick(item.article, record.storeId)}>{text}</a> : text} />
                    <Table.Column title="Количество" dataIndex="quantity" />
                    <Table.Column title="Цена" dataIndex="price" />
                </Table>}>
            <Table.Column title="Номер заказа" dataIndex="orderId" sorter={CommonSorter('orderId')} filterDropdown={TextBoxDropdown} onFilter={CommonFilter('orderId')} />
            <Table.Column title="Номер чека" dataIndex="receiptId" sorter={CommonSorter('receiptId')} />
            <Table.Column title="Магазин" dataIndex="storeName" sorter={CommonSorter('storeName')} />
            <Table.Column title="Канал" dataIndex="channel" sorter={CommonSorter('channel')} />
            <Table.Column title="Дата создания" dataIndex="created" sorter={this.DateSorter('created')} defaultSortOrder="ascend" />
            <Table.Column title="Статус" dataIndex="orderStatus" />
            <Table.Column title="Способ оплаты" dataIndex="paymentMethod" />
            <Table.Column title="Способ доставки" dataIndex="deliveryMode" />
            <Table.Column title="Сумма" dataIndex="totalIncludingVAT" />
            <Table.Column key="button" render={(_, record: any) => this.onlyInUSD(record)} />
        </Table>
    }
    private onlyInUSD(data: any): any {
        return (window.IsUSD && data.orderLink) ? <Button size="small" icon="eye" shape="circle" onClick={() => this.onGoToButtonBlick(data)} /> : null;
    }
    private onGoToButtonBlick(record: any) {
        window.open(`http://event/?eventname=openOrderFromCustomerboard&orderLink=${encodeURIComponent(record.orderLink)}&orderSource=${record.channel}`);
    }
    private onArticleClick(article: string, storeId: string) {
        OpenLink('openArticleFromCustomerBoard', { article, store: storeId })
    }
    private DateSorter(dataIndex: string): (a: any, b: any) => number {
        return (a,b) => (moment(a[dataIndex]).isBefore(moment(b[dataIndex]))) ? -1 : 1;
    }
}
interface CustomerBoardComponentState {
    orders: any;
}