import * as React from 'react';
import { Select, Input, Button, Form } from 'antd';
import { SelectValue } from 'antd/lib/select';

import { DialogLayout } from '../common/dialog-layout';
import { RunAction } from '../../shared/actionRunner';
import { CloseThisDialog } from '../../shared/dialogHelper';
import { GetDataParameter } from '../../shared/webResourceHelper';
import { OptionSet, GetOptionSetValues } from '../../shared/xrmApiHelper';

import './quote-deactivation-form.scss';
import { ReasonOfFail, StatusCode } from 'app/shared/Enums';

const { Option } = Select;
const { TextArea } = Input;
const { Item: FormItem } = Form;

interface QuoteDeactivationOptionSets {
    deactivationReasons: OptionSet[],
    deactivationTypes: OptionSet[]
}

interface QuoteDeactivationFormContentState {
    deactivationReasonVisibility: 'block' | 'none',
    deactivationReasonValue: number[],
    deactivationTypeValue: number[],
    deactivationComment: string
}

class QuoteDeactivationFormContent extends React.Component<QuoteDeactivationOptionSets, QuoteDeactivationFormContentState>  {
    constructor(props: QuoteDeactivationOptionSets) {
        super(props);
        
        this.state = {
            deactivationReasonVisibility: 'none',
            deactivationTypeValue: [StatusCode.recalculation],
            deactivationReasonValue: [],
            deactivationComment: ''
        };
    }

    onChangeDeactivationReason = (value: SelectValue) => this.setState({ deactivationReasonValue: [value as number] });
    onChangeDeactivationComment = (e: React.ChangeEvent<HTMLTextAreaElement>) => this.setState({ deactivationComment: e.target.value });

    onDeactivateButtonClick = () => {
        let quoteid = GetDataParameter();
        let arg1 = this.state.deactivationTypeValue.length == 0 ? null : this.state.deactivationTypeValue[0].toString();
        let arg2 = this.state.deactivationReasonValue.length == 0 ? null : this.state.deactivationReasonValue[0].toString();

        RunAction('lmr_Quote_Action', null, {
            quoteid: quoteid,
            actionid: '2',
            arg1: arg1,
            arg2: arg2,
            comment: this.state.deactivationComment
        }, true)
            .then(CloseThisDialog)
            .catch(CloseThisDialog);
    }

    onChangeDeactivationType = (value: SelectValue) => {
        let inlineDialog = parent.document.getElementById("InlineDialog");
        if (inlineDialog)
            inlineDialog.style.height = value === StatusCode.lost ? '320px' : '190px'

        this.setState({
            deactivationReasonVisibility: value === StatusCode.lost ? 'block' : 'none',
            deactivationTypeValue: [value as number],
            deactivationReasonValue: [],
            deactivationComment: ''
        });
    };

    render() {
        const deactivationReasonsOptions: JSX.Element[] = this.props.deactivationReasons
            .map((el => { return <Option value={el.attributeValue}>{el.value}</Option> }));
        const deactivationTypesOptions: JSX.Element[] = this.props.deactivationTypes
            .map((el => { return <Option value={el.attributeValue}>{el.value}</Option> }));

        return (
            <Form>
                <FormItem className="control">
                    <Select
                        onChange={this.onChangeDeactivationType}
                        placeholder="Выберите тип деактивации"
                        value={this.state.deactivationTypeValue}
                    >
                        {deactivationTypesOptions}
                    </Select>
                </FormItem>
                <FormItem className="control" style={{ display: this.state.deactivationReasonVisibility }}>
                    <Select
                        placeholder="Выберите причину деактивации"
                        onChange={this.onChangeDeactivationReason}
                        value={this.state.deactivationReasonValue}
                    >
                        {deactivationReasonsOptions}
                    </Select>
                </FormItem>
                <FormItem className="control" style={{ display: this.state.deactivationReasonVisibility }}>
                    <TextArea
                        placeholder="Введите комментарий"
                        autosize={{ maxRows: 3, minRows: 3 }}
                        onChange={this.onChangeDeactivationComment}
                        value={this.state.deactivationComment}
                    />
                </FormItem>
                <FormItem className="btn-control" >
                    <Button type='primary' onClick={this.onDeactivateButtonClick} block>
                        Деактивировать
                    </Button>
                </FormItem>
            </Form>
        );
    }
}

export default class QuoteDeactivationForm extends React.Component {
    render() {
        let deactivationTypes: OptionSet[] = GetOptionSetValues('quote', 'statusCode')
            .filter(el => el.attributeValue == StatusCode.lost || el.attributeValue == StatusCode.recalculation);
        let deactivationReasons: OptionSet[] = GetOptionSetValues('quote', 'lmr_reasonoffail')
            .filter(el => el.attributeValue != ReasonOfFail.recalculation);

        return (
            <DialogLayout
                header='Причина деактивации'
                top={<QuoteDeactivationFormContent deactivationReasons={deactivationReasons} deactivationTypes={deactivationTypes} />}
            >
            </DialogLayout>
        );
    }
}