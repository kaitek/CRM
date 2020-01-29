import * as React from 'react'
import { Rate, Tooltip } from 'antd'

interface ContactRatingState {
    rating: number;
}
export class ContactRating extends React.Component<any, ContactRatingState> {
    constructor(props: any) {
        super(props)
        this.state = {
            rating: 0
        }
    }
    componentDidMount() {
        const averageRating = parent.Xrm.Page.getAttribute('lmr_avgrating').getValue();
        this.setState({rating: averageRating});
    }
    render() {
        return <Rate disabled allowHalf value={this.calculateStartCount(this.state.rating)} style={{marginTop: -5}} />

    }
    calculateStartCount(rating: number) {
        const integer = Math.floor(rating);
        const decimal = rating - integer;
        return decimal < 0.25 ? integer : decimal <= 0.75 ? integer + 0.5 : integer + 1;
    }
}