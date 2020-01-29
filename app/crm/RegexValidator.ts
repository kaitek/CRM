
type TextControl = Xrm.Controls.AutoLookupControl;

export class RegexValidator {
    private plainAttributeRegexMap: { [attributeName: string]: { regex: RegExp, errorText: string } } = {}
    constructor(attributeRegexMap: { attributes: string[], regex: string | RegExp, errorText: string }[]) {
        attributeRegexMap.forEach(configItem =>
            configItem.attributes.forEach(attributeName =>
                this.plainAttributeRegexMap[attributeName] = { regex: typeof configItem.regex === 'string' ? new RegExp(configItem.regex) : configItem.regex, errorText: configItem.errorText }
            )
        )
    }

    public register(): void {
        Object.keys(this.plainAttributeRegexMap).forEach(attributeName => {
            const attribute = Xrm.Page.getAttribute(attributeName);
            (<TextControl>attribute.controls.get(0)).addOnKeyPress(this.KeyPressHandler.bind(this));
            attribute.addOnChange(this.AttributeChangeHandler.bind(this));
        });
    }
    private KeyPressHandler(context: Xrm.Events.EventContext) {
        const control = <TextControl>context.getEventSource();
        const controlValue: string = control.getValue();
        const config = this.plainAttributeRegexMap[control.getAttribute().getName()];
        this.ValidateControlValue(control, controlValue, config.regex, config.errorText);
    }
    private AttributeChangeHandler(context: Xrm.Events.EventContext) {
        const attribute = <Xrm.Attributes.Attribute>context.getEventSource();
        const config = this.plainAttributeRegexMap[attribute.getName()];
        this.ValidateControlValue(<TextControl>(attribute.controls.get(0)), attribute.getValue(), config.regex, config.errorText);
    }
    private ValidateControlValue(control: TextControl, value: string, regex: RegExp, errorText: string): void {
        const attribute = control.getAttribute<Xrm.Attributes.StringAttribute>();
        const requiredLevel = attribute.getRequiredLevel();
        const maxLength = attribute.getMaxLength();

        if (!value) {
            if (requiredLevel == "none")
                control.clearNotification(control.getName());
        }
        //данная проверка необходима, так как  getValue() контрола 
        //считывает на один press - символ больше, чем максимальное значение
        else if (value.length <= maxLength) {
            if (!regex.test(value))
                control.setNotification(errorText, control.getName());
            else
                control.clearNotification(control.getName());
        }
    }
}