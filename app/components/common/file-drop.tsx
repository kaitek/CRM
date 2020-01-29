import * as React from 'react';
import { CloseThisDialog } from '../../shared/dialogHelper';
import './file-drop.scss'

interface FileDropAreaProps {
    onDrop: (files: FileList) => void;
}
interface FileDropAreaState {
    dragging: boolean;
}
export class FileDropArea extends React.Component<FileDropAreaProps, FileDropAreaState> {
    constructor(props: FileDropAreaProps) {
        super(props);
        this.state = {
            dragging: false
        }
    }
    render() {
        return <div onDrop={this.onDrop.bind(this)} onDragEnter={this.onDragEnter.bind(this)} onDragLeave={this.onDragLeave.bind(this)} onDragOver={e => e.preventDefault()} className={this.state.dragging ? 'dragging' : 'normal'}>
            {this.props.children}
        </div>
    }
    onDragEnter(e: React.DragEvent<HTMLDivElement>) {
        this.setState({ dragging: true });
        e.preventDefault();
    }
    onDragLeave(e: React.DragEvent<HTMLDivElement>) {
        this.setState({ dragging: false });
        e.preventDefault();
    }
    onDrop(e: React.DragEvent<HTMLDivElement>) {
        this.props.onDrop(e.dataTransfer.files);
        this.setState({ dragging: false });
        e.preventDefault();
    }
}