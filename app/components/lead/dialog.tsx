import * as React from 'react';
import { Button, Table, Radio, Row, Col } from 'antd';
import { DialogLayout } from '../common/dialog-layout';
import { GetDataParameter } from '../../shared/webResourceHelper';
import Column from 'antd/lib/table/Column';
import { CloseThisDialog } from '../../shared/dialogHelper';

import './lead-qualify-form.scss';

interface Contact {
    contactid: string;
    firstname: string;
    emailaddress: string;
    lastname: string;
    mobilephone: string;
    middlename: string;
    fullname: string;
    customertype: number;    
}

interface StateContactProps {
    contactid?: string;   
    onSelect?: (contactid: string) => void;
    setId?: (contactid: string) => void;    
}

interface State extends StateContactProps {
    overflow: boolean;
    keys: string[];   
    Contacts: Contact[];   
}

export default class ContactsList extends React.Component<
    StateContactProps,
    State
    > {

    constructor(props: StateContactProps) {
        super(props);
        
        const contactsJson: any = GetDataParameter();

        const contacts = JSON.parse(contactsJson).map(function (
            contact: any,
            index: number
        ) {
            var formattedContacts: Contact = {
                contactid: contact.contactid,
                firstname: contact.firstname,
                lastname: contact.lastname,
                emailaddress: contact.emailaddress,
                mobilephone: contact.mobilephone,
                middlename: contact.middlename,
                fullname: contact.fullname,
                customertype: contact.customertype                
            };

            return formattedContacts;
        });        
        
        this.state = {            
            overflow: contacts.length > 1,
            Contacts: contacts,
            onSelect: props.onSelect,                    
            keys: []
        }; 
    }     

    public onSelect(contactid: string): void {         
        const { setId } = this.props;       
        setId!(contactid);        
    }
   

    onRowKeysChange = (keys: any) => {       
        this.setState({ keys });
        this.onSelect(keys[0]);
    };

    render() {        

        return (
            <Table
                bodyStyle={{ height: '180px', overflowY: 'auto' }}
                dataSource={this.state.Contacts}               
                pagination={{ hideOnSinglePage: true }}
                rowKey={record => record.contactid}
                rowSelection={{
                    type: 'radio',
                    selectedRowKeys: this.state.keys,
                    onChange: this.onRowKeysChange
                }}
                key='contactid'
                size='small'   
                onRow={(record: Contact) => {
                    return {
                        onClick: () => {
                            this.onSelect(record.contactid);
                        },                         
                        onDoubleClick: () => {
                            Xrm.Utility.openEntityForm(
                                'contact',
                                record.contactid,
                                {},
                                { openInNewWindow: true }
                            );
                        },
                    };
                }}
            >
                
                <Column                    
                    title='ФИО'
                    dataIndex='fullname'                   
                    key='fullname'
                />
                <Column                    
                    title='Почта'
                    dataIndex='emailaddress'
                    key='emailaddress'
                />
                <Column                    
                    title='Телефон'
                    dataIndex='mobilephone'
                    key='mobilephone'
                />
            </Table>
        );
    }
}

export class Dialog extends React.Component<any, StateContactProps> {

    setId = (contactid: string) => {       
        if (!this.state.contactid || this.state.contactid !== contactid)
            this.setState({ contactid: contactid });
    };    

    constructor(props: StateContactProps) {
        super(props);       
        this.state = {
            contactid: 'underfined'
        };
    }   
     
    render() {       
        return (
            <DialogLayout
                header='Контакты'
                top={                    
                    <Row>
                        <Row className='row-top'>
                            Выберите клиента из списка и нажмите "ОК", если ни один клиент не подходит нажмите "Создать новый контакт".
                        </Row>
                        <Row className='row-bottom'>
                            <ContactsList setId={this.setId} />
                        </Row>
                    </Row>
                }
                bottom={
                    <Row>
                        <Col className='col-left' xs={2} sm={4} md={6} lg={8} xl={10} >
                            <Button className="btn-control-left" type='primary' onClick={() => CloseThisDialog('')} block>
                                Создать контакт 
                            </Button>
                        </Col>
                        <Col className="col-right" xs={2} sm={4} md={6} lg={8} xl={10}>
                            <Button className="btn-control-right" type='primary' onClick={() => CloseThisDialog(this.state.contactid)} block>
                                OK
                            </Button>
                        </Col> 
                    </Row>
                }
            />
        );
    }
}