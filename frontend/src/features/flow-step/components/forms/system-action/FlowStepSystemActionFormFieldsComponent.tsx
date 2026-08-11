import { useWatch, useFormContext } from "react-hook-form";
import { Message } from "primereact/message";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";
import { SYSTEM_ACTIONS } from "@/features/flow-step/components/forms/system-action/system-actions";

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepSystemActionFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const systemActionType = useWatch({ control, name: "systemActionType" });

  const activeAction = SYSTEM_ACTIONS.find(
    (x) => x.systemActionType === systemActionType,
  );

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      <FormSelectButtonComponent
        fieldName="systemActionType"
        labelText="Action"
        options={SYSTEM_ACTIONS.map((x) => ({
          label: x.label,
          value: x.systemActionType,
        }))}
        isRequired={true}
        isDisabled={isDisabled}
      />

      {activeAction && (
        <LabelComponent
          text={activeAction.description}
          color="secondary"
          size="sm"
        />
      )}

      {systemActionType === SystemActionTypeEnum.MONITOR_OFF && (
        <Message
          severity="info"
          className="w-full justify-content-start mt-3"
          content={
            <LabelComponent
              size="sm"
              text="Any mouse or keyboard input wakes the screens again, including input this flow sends. Put this step last."
            />
          }
        />
      )}
    </>
  );
}
