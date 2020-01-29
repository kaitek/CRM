import * as React from 'react'
import { Rate } from 'antd'
export class IncidentRating extends React.Component<any, any> {
    constructor(props: any) {
        super(props)
        this.state = {
            value: 0
        }
    }
    componentDidMount() {
        this.setState({value: this.getClientsRatingAttribute().getValue()})
    }
    render() {
        return <Rate onChange={this.onRatingChanged.bind(this)} style={{marginTop: -5}} value={this.state.value} />
    }
    onRatingChanged(value: number) {
        this.getClientsRatingAttribute().setValue(value > 0 ? value : null);
        this.setState({value: value})
    }
    private getClientsRatingAttribute(): any {
        return parent.Xrm.Page.getAttribute('lmr_clientsrating')
    }
}