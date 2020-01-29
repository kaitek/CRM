import * as React from 'react';
import Column from 'antd/lib/table/Column';
import { Table, Spin } from 'antd';

import SolutionForm from './solutionForm';
import { DialogLayout } from '../common/dialog-layout';
import { TaskCloseFormManager, OpenedTask } from './taskCloseFormManager'
import { GetDataParameter } from '../../shared/webResourceHelper';
import { SetEmptyCrossCallback, CloseThisDialog } from '../../shared/dialogHelper';

import './task-close-form.scss';

const { Component } = React;

interface TasksCloseFormState {
    isLoading: boolean,
    openedTasks: OpenedTask[]
}

export default class TasksCloseForm extends Component<any, TasksCloseFormState> {
    state = {
        isLoading: true,
        openedTasks: []
    }

    componentDidMount() {
        this.loadOpenedTasks();
        SetEmptyCrossCallback();
    };

    componentDidUpdate() {
        if (this.state.isLoading)
            this.loadOpenedTasks();
        else if (this.state.openedTasks.length == 0)
            CloseThisDialog();
    }

    reloadOpenedTasks = () => (this.setState({ isLoading: true }));
    
    loadOpenedTasks = () => {
        const opportunityId: string | null = GetDataParameter();

        TaskCloseFormManager.GetOpenedTasksAsync(opportunityId)
            .then((openedTasks: OpenedTask[]) => {
                this.setState(({ isLoading: false, openedTasks: openedTasks }))
            })
            .catch((exp) => {
                this.setState({ isLoading: false });
                alert(exp);
            });
    };

    private renderTaskTitle = (text: string, row: OpenedTask): React.ReactNode => {
        let linkStyle = {
            color: "#004F1C",
            textDecoration: "none"
        };
        const newData: OpenedTask[] = [...this.state.openedTasks];
        const index = newData.findIndex((item: OpenedTask) => row.key === item.key);
        const item: OpenedTask = newData[index];

        if (item.hoveringMouseOnLink)
            linkStyle = {
                color: "#004F1C",
                textDecoration: "underline"
            };

        return (
            <a style={linkStyle} href="#"
                onClick={() => Xrm.Utility.openEntityForm("task", row.key, {}, { openInNewWindow: true })}
                onMouseEnter={() => {
                    item.hoveringMouseOnLink = true;
                    newData.splice(index, 1, {
                        ...item,
                        ...row,
                    });
                    this.setState({ openedTasks: newData });
                }}
                onMouseLeave={() => {
                    item.hoveringMouseOnLink = false;
                    newData.splice(index, 1, {
                        ...item,
                        ...row,
                    });
                    this.setState({ openedTasks: newData });
                }}
            >
                {text}
            </a>
        );
    }

    render() {
        return (
            <DialogLayout
                header="Открытые задачи"
                top={
                    <Spin spinning={this.state.isLoading}>
                        <Table
                            size="small"
                            dataSource={this.state.openedTasks}
                            bodyStyle={{ height: "315px", maxHeight: "315px", overflowY: "auto" }}
                            pagination={{ hideOnSinglePage: true }}
                        >
                            <Column title='Тема' render={this.renderTaskTitle} dataIndex='subject' />
                            <Column title='Срок' width='150px' dataIndex='scheduledend' />
                            <Column title='Решение' dataIndex='solution' width='400px' render={
                                (text: any, record: any) => {
                                    return <SolutionForm record={record} initialText={text} reloadOpenedTasks={this.reloadOpenedTasks} />;
                                }
                            }
                            />
                        </Table>
                    </Spin>
                }
            />
        );
    }
}