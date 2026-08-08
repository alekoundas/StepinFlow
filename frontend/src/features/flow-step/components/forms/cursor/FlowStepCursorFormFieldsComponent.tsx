import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { CursorButtonTypeEnum } from "@/shared/enums/backend/cursor-button-type-enum";
import { CursorScrollDirectionTypeEnum } from "@/shared/enums/backend/cursor-scroll-direction-type-enum";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepCursorSchema } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";
import FlowStepCursorLocationFieldsComponent from "@/features/flow-step/components/forms/cursor/FlowStepCursorLocationFieldsComponent";
import { cursorButtonActionTypeEnum } from "@/shared/enums/backend/cursor-button-action-type-enum";

type CursorForm = z.infer<typeof FlowStepCursorSchema>;

interface EnumOption {
  label: string;
  value: string;
}

interface Props {
  flowId: number | undefined;
  parentFlowStepId: number | undefined;
  isDisabled?: boolean;
}

const toOptions = (values: Record<string, string>): EnumOption[] =>
  Object.values(values).map((value) => ({
    label: value.replaceAll("_", " ").toLowerCase(),
    value,
  }));

export default function FlowStepCursorFormFieldsComponent({
  flowId,
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const flowStepType = useWatch({ control, name: "flowStepType" });

  const showStartPoint =
    flowStepType === FlowStepTypeEnum.CURSOR_CLICK ||
    flowStepType === FlowStepTypeEnum.CURSOR_RELOCATE ||
    flowStepType === FlowStepTypeEnum.CURSOR_DRAG;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      {/* CURSOR_CLICK */}
      {flowStepType === FlowStepTypeEnum.CURSOR_CLICK && (
        <div className="flex gap-3">
          <div className="flex-1">
            <FormDropdownComponent<CursorForm, EnumOption>
              fieldName="cursorButtonActionType"
              labelText="Click Action"
              mode="local"
              options={toOptions(cursorButtonActionTypeEnum)}
              optionLabel="label"
              optionValue="value"
              isRequired={true}
              isDisabled={isDisabled}
            />
          </div>

          <div className="flex-1">
            <FormDropdownComponent<CursorForm, EnumOption>
              fieldName="cursorButtonType"
              labelText="Mouse Button"
              mode="local"
              options={toOptions(CursorButtonTypeEnum)}
              optionLabel="label"
              optionValue="value"
              isRequired={true}
              isDisabled={isDisabled}
            />
          </div>
        </div>
      )}

      {/* CURSOR_DRAG */}
      {flowStepType === FlowStepTypeEnum.CURSOR_DRAG && (
        <FormDropdownComponent<CursorForm, EnumOption>
          fieldName="cursorButtonType"
          labelText="Mouse Button"
          mode="local"
          options={toOptions(CursorButtonTypeEnum)}
          optionLabel="label"
          optionValue="value"
          isRequired={true}
          isDisabled={isDisabled}
        />
      )}

      {/* CURSOR_SCROLL */}
      {flowStepType === FlowStepTypeEnum.CURSOR_SCROLL && (
        <div className="flex gap-3">
          <div className="flex-1">
            <FormDropdownComponent<CursorForm, EnumOption>
              fieldName="cursorScrollDirectionType"
              labelText="Direction"
              mode="local"
              options={toOptions(CursorScrollDirectionTypeEnum)}
              optionLabel="label"
              optionValue="value"
              isRequired={true}
              isDisabled={isDisabled}
            />
          </div>

          <div className="flex-1">
            <FormInputNumberComponent
              fieldName="loopCount"
              label="Scroll Amount"
              min={1}
              max={2147483647}
              isRequired={true}
              isDisabled={isDisabled}
              hintText="Number of wheel notches"
            />
          </div>
        </div>
      )}

      {/* Point pickers */}
      {showStartPoint && (
        <FlowStepCursorLocationFieldsComponent
          title={
            flowStepType === FlowStepTypeEnum.CURSOR_DRAG
              ? "Grab Point"
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
          title="Drop Point"
          flowId={flowId}
          parentFlowStepId={parentFlowStepId}
          isDisabled={isDisabled}
        />
      )}
    </>
  );
}
