import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepCursorSchema } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";
import FlowStepCursorLocationFieldsComponent from "@/features/flow-step/components/forms/cursor/FlowStepCursorLocationFieldsComponent";
import { cursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";

type CursorForm = z.infer<typeof FlowStepCursorSchema>;

interface Props {
  flowId: number | undefined;
  parentFlowStepId: number | undefined;
  isDisabled?: boolean;
}

const ACTION_OPTIONS = [
  { label: "Single", value: cursorButtonActionTypeEnum.SINGLE_CLICK },
  { label: "Double", value: cursorButtonActionTypeEnum.DOUBLE_CLICK },
  { label: "Hold", value: cursorButtonActionTypeEnum.HOLD_CLICK },
  { label: "Release", value: cursorButtonActionTypeEnum.RELEASE_CLICK },
];

const BUTTON_OPTIONS = [
  { label: "Left", value: CursorButtonTypeEnum.LEFT_BUTTON },
  { label: "Right", value: CursorButtonTypeEnum.RIGHT_BUTTON },
  { label: "Middle", value: CursorButtonTypeEnum.MIDDLE_BUTTON },
];

const DIRECTION_OPTIONS = [
  { label: "Up", value: CursorScrollDirectionTypeEnum.UP },
  { label: "Down", value: CursorScrollDirectionTypeEnum.DOWN },
  { label: "Left", value: CursorScrollDirectionTypeEnum.LEFT },
  { label: "Right", value: CursorScrollDirectionTypeEnum.RIGHT },
];

export default function FlowStepCursorFormFieldsComponent({
  flowId,
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const flowStepType = useWatch({ control, name: "flowStepType" });

  return (
    <>
      {flowStepType === FlowStepTypeEnum.CURSOR_CLICK && (
        <>
          <FormSelectButtonComponent<CursorForm, string>
            fieldName="cursorButtonActionType"
            labelText="Action"
            options={ACTION_OPTIONS}
            isRequired={true}
            isDisabled={isDisabled}
          />
          <FormSelectButtonComponent<CursorForm, string>
            fieldName="cursorButtonType"
            labelText="Button"
            options={BUTTON_OPTIONS}
            isRequired={true}
            isDisabled={isDisabled}
            hintText="A single left click is what the script leaves unsaid."
          />
        </>
      )}

      {flowStepType === FlowStepTypeEnum.CURSOR_DRAG && (
        <FormSelectButtonComponent<CursorForm, string>
          fieldName="cursorButtonType"
          labelText="Button"
          options={BUTTON_OPTIONS}
          isRequired={true}
          isDisabled={isDisabled}
        />
      )}

      {flowStepType === FlowStepTypeEnum.CURSOR_SCROLL && (
        <>
          <FormSelectButtonComponent<CursorForm, string>
            fieldName="cursorScrollDirectionType"
            labelText="Direction"
            options={DIRECTION_OPTIONS}
            isRequired={true}
            isDisabled={isDisabled}
          />
          <FormInputNumberComponent
            fieldName="loopCount"
            label="Notches"
            min={1}
            max={2147483647}
            isRequired={true}
            isDisabled={isDisabled}
            hintText="Wheel notches, where the cursor is."
          />
        </>
      )}

      {(flowStepType === FlowStepTypeEnum.CURSOR_RELOCATE ||
        flowStepType === FlowStepTypeEnum.CURSOR_DRAG) && (
        <FlowStepCursorLocationFieldsComponent
          title={
            flowStepType === FlowStepTypeEnum.CURSOR_DRAG
              ? "Grab point"
              : "Location"
          }
          flowId={flowId}
          parentFlowStepId={parentFlowStepId}
          isDisabled={isDisabled}
        />
      )}

      {flowStepType === FlowStepTypeEnum.CURSOR_DRAG && (
        <FlowStepCursorLocationFieldsComponent
          isEndPoint={true}
          title="Drop point"
          flowId={flowId}
          parentFlowStepId={parentFlowStepId}
          isDisabled={isDisabled}
        />
      )}
    </>
  );
}
