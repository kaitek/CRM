

import * as React from 'react';
import { Select, Input, Button, Form, Row, Col, message } from 'antd';
import { SelectValue } from 'antd/lib/select';

import { DialogLayout } from '../../common/dialog-layout';
import { RunAction } from '../../../shared/actionRunner';
import { CloseThisDialog } from '../../../shared/dialogHelper';
import { GetDataParameter, GetParameter, GetUserId } from '../../../shared/webResourceHelper';
import { OptionSet, GetOptionSetValues } from '../../../shared/xrmApiHelper';

import { DatePicker } from 'antd';
import * as moment from 'moment';
import locale from 'antd/lib/date-picker/locale/ru_RU';

import { OpportunityStatusCode, } from '../../../shared/Enums';
import './closeaslostview.scss';
import { string } from 'prop-types';

const { Option } = Select;
const { TextArea } = Input;
const { Item: FormItem } = Form;

interface OpportunityLostOptionSets {
    lostReasons: OptionSet[]
}

interface OpportunityLostFormContentState {  
    lostReasonValue: number,   
    lostReasonText: string,   
    lostComment: string,
    lostDate: string
}

class OpportunityLostFormContent extends React.Component<OpportunityLostOptionSets, OpportunityLostFormContentState>  {
    constructor(props: OpportunityLostOptionSets) {
        super(props);

        this.state = {                    
            lostReasonValue: OpportunityStatusCode.Other,
            lostReasonText: 'Другое',
            lostComment: '',           
            lostDate: this.getCurrentDate()
        };
    }

    onChangeLostReason = (value: SelectValue) => this.setState({ lostReasonValue: value as number});
    onChangeLostComment = (e: React.ChangeEvent<HTMLTextAreaElement>) => this.setState({ lostComment: e.target.value });
    onChangeLostDate = (date: moment.Moment, dateString: string) => this.setState({ lostDate: date.format('YYYY-MM-DD') });
    
    getCurrentDate = () => {
        const d = new Date(), mm = d.getMonth() + 1, dd = d.getDate();
        return [d.getFullYear(), (mm > 9 ? '' : '0') + mm, (dd > 9 ? '' : '0') + dd].join('');
    };

    onLostButtonClick = () => {
        
        let opportunityid = GetDataParameter();   
        let userid = GetUserId();
        
        let status: number = this.state.lostReasonValue;
        if (status === OpportunityStatusCode.Other && this.state.lostComment === "") {

            message.warning('Укажите комментарий', 5);
            return;
        }

        RunAction('lmr_CloseAsLostOpportunity', null, {
            opportunityid: opportunityid,   
            status: status,
            closedate: this.state.lostDate,
            userid: userid,
            description: this.state.lostComment
        }, false)
            .then(CloseThisDialog(opportunityid))
            .catch(CloseThisDialog(opportunityid));

    }   

    render() {
        const deactivationReasonsOptions: JSX.Element[] = this.props.lostReasons
            .map((el => { return <Option key={el.attributeValue} value={el.attributeValue}>{el.value}</Option> }));

        return (
            <Form > 
                
                <FormItem className="control" >
                    <Select                        
                        onChange={this.onChangeLostReason}
                        value={this.state.lostReasonValue}
                    >
                        {deactivationReasonsOptions}
                    </Select>
                </FormItem>
                <FormItem  className="control" >
                    <DatePicker 
                        defaultValue={moment(this.getCurrentDate(), 'YYYY-MM-DD')}
                        locale={locale}
                        onChange={this.onChangeLostDate}
                        value={moment(this.state.lostDate)}
                       >
                    </DatePicker>
                </FormItem>
                <FormItem className="control" >
                    <TextArea
                        placeholder="Введите комментарий"
                        autosize={{ maxRows: 3, minRows: 3 }}
                        onChange={this.onChangeLostComment}
                        value={this.state.lostComment}
                    />
                </FormItem>
                <FormItem className="control" >
                <Row className="btn-control" >  
                
                        <Col className='col-left' xs={2} sm={4} md={6} lg={8} xl={10} >
                            <Button className="btn-control-left" type='primary' onClick={() => CloseThisDialog('')} block>
                                Отмена
                            </Button>
                        </Col>
                        <Col className="col-right" xs={2} sm={4} md={6} lg={8} xl={10}>
                            <Button className="btn-control-right" type='primary' onClick={this.onLostButtonClick} block>
                                OK
                            </Button>
                        </Col>  
                    </Row>
                </FormItem>
            </Form>
        );
    }
}

export class Dialog extends React.Component<any, any> {

    constructor(props: any) {
        super(props);       
    } 


    render() {
        //let lostReasons: OptionSet[] = GetOptionSetValues('quote', 'lmr_reasonoffail')
        //    .filter(el => el.attributeValue != ReasonOfFail.recalculation);

        let lostReasons: OptionSet[] = GetOptionSetValues('opportunity', 'statuscode')
            .filter(el => el.attributeValue != OpportunityStatusCode.InProcess
                && el.attributeValue != OpportunityStatusCode.Canceled
                && el.attributeValue != OpportunityStatusCode.Contract
                && el.attributeValue != OpportunityStatusCode.Delay
                && el.attributeValue != OpportunityStatusCode.Delivery
                && el.attributeValue != OpportunityStatusCode.InProcess
                && el.attributeValue != OpportunityStatusCode.Installation
                && el.attributeValue != OpportunityStatusCode.Lead
                && el.attributeValue != OpportunityStatusCode.Measure
                && el.attributeValue != OpportunityStatusCode.Won
                && el.attributeValue != OpportunityStatusCode.Payment
                && el.attributeValue != OpportunityStatusCode.Contract);
       
        return (
            <DialogLayout
                header='Аннулировать продажу'
                top={<OpportunityLostFormContent lostReasons={lostReasons}  />}
            >
            </DialogLayout>
        );
    }
}