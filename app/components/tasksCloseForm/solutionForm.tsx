import * as React from 'react';
import { Input, Button, Form } from 'antd';
import { FormComponentProps } from 'antd/lib/form/Form';

import { TaskStateCode } from '../../shared/Enums';
import { OpenedTask, TaskCloseFormManager } from "./taskCloseFormManager";

const { Component } = React;
const { Item: FormItem } = Form;

interface SolutionFormProps extends FormComponentProps {
    initialText: string,
    record: OpenedTask,
    reloadOpenedTasks: Function,
}

interface SolutionFormState {
    hasFeedback: boolean,
    validateStatus: any
}

class WrappedSolutionForm extends Component<SolutionFormProps, SolutionFormState> {
    state: SolutionFormState = {
        hasFeedback: false,
        validateStatus: ""
    }

    onCompleteBtnClick = () => {
        let solution: string = this.props.form.getFieldValue('solution');

        if (!this.isInvalidSolution(solution)) {
            TaskCloseFormManager.CloseTaskAsync(this.props.record.key, solution, TaskStateCode.finished)
                .then(() => this.props.reloadOpenedTasks())
                .catch(exp => (alert(exp)));
        }
        else {
            this.setSolutionValidateStatus(true);
        }
    }

    onCancelBtnClick = () => {
        let solution: string = this.props.form.getFieldValue('solution');

        TaskCloseFormManager.CloseTaskAsync(this.props.record.key, solution, TaskStateCode.canceled)
                .then(() => this.props.reloadOpenedTasks())
                .catch(exp => (alert(exp)));
    }

    isInvalidSolution = (solution: string): boolean => !solution;

    setSolutionValidateStatus = (mustBeSetInvalidIndicator: boolean) => {
        const solution: any = this.props.form.getFieldValue('solution');

        this.setState({
            hasFeedback: this.isInvalidSolution(solution) && mustBeSetInvalidIndicator,
            validateStatus: this.isInvalidSolution(solution) && mustBeSetInvalidIndicator ? 'error' : ''
        });
    }

    render() {
        return (
            <Form className="solution-form" layout="inline">
                <FormItem className="left-solution-form-item" validateStatus={this.state.validateStatus} hasFeedback={this.state.hasFeedback}>
                    {
                        this.props.form.getFieldDecorator('solution',
                            {
                                initialValue: this.props.initialText
                            })(
                                <Input onBlur={() => this.setSolutionValidateStatus(false)} />
                            )
                    }
                </FormItem>
                <FormItem className="right-solution-form-item">
                    <Button id="complete-task-btn" onClick={this.onCompleteBtnClick} type="primary" icon="check" />
                    <Button id="cancel-task-btn" onClick={this.onCancelBtnClick} type="danger" icon="close" />
                </FormItem>
            </Form>
        );
    }
}

const SolutionForm = Form.create<SolutionFormProps>()(WrappedSolutionForm);

export default SolutionForm;