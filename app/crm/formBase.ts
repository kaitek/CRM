export abstract class FormBase {
    public abstract OnLoad(context: Xrm.Events.EventContext): void;
    protected IsExists(): boolean {
        return !!Xrm.Page.data.entity.getId();
    }
    protected IsNew(): boolean {
        return !this.IsExists();
    }
    protected GetId(): string {
        return Xrm.Page.data.entity.getId()
    }
}