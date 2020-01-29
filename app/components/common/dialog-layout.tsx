import * as React from 'react';
import './dialog-layout.scss'
interface DialogLayoutState {
    header?: string;
    
    top?: JSX.Element;
    bottom?: JSX.Element;
}
export class DialogLayout extends React.Component<DialogLayoutState, {}> {
    render() {
        return <div className="dialog-layout-main">
            <div className="fixed-wrapper-top header"><h1>{this.props.header}</h1></div>
            {this.props.top && <div className="fixed-wrapper">{this.props.top}</div>}

            {this.props.top && <div className="fake-top">{this.props.top}</div>}
            <div className="content">{this.props.children}</div>
            {this.props.bottom && <div className="fake-bottom">{this.props.bottom}</div>}

            {this.props.bottom && <div className="fixed-wrapper-bottom">{this.props.bottom}</div>}
        </div>
    }
}