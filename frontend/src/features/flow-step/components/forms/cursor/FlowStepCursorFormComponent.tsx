import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";
import { useEffect } from "react";
import { SelectButton } from "primereact/selectbutton";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepCursorSchema } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";
import {
  CURSOR_MODES,
  type CursorMode,
} from "@/features/flow-step/components/forms/cursor/cursor-modes";
import FlowStepCursorFormFieldsComponent from "@/features/flow-step/components/forms/cursor/FlowStepCursorFormFieldsComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepCursorFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepCursorSchema>>({
    resolver: zodResolver(FlowStepCursorSchema),
    mode: "onChange",
    // Form state only: the saved step says which source it uses by which id is set. A new step
    // starts on a step's result, the usual source: move onto what a search above found.
    defaultValues: {
      ...defaultValues,
      isPointCustom: defaultValues.flowPointId != null,
      isPointEndCustom: defaultValues.flowPointEndId != null,
    } as any,
  });

  const {
    control,
    formState: { isValid, isDirty },
    setValue,
    trigger,
  } = form;

  const flowStepType = useWatch({ control, name: "flowStepType" });
  const activeMode =
    CURSOR_MODES.find((x) => x.flowStepType === flowStepType) ??
    CURSOR_MODES[0];

  // Force full validation on mount so that isValid + errors are in sync
  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const handleModeChange = (mode: CursorMode) => {
    if (formMode === "VIEW" || mode.flowStepType === flowStepType) return;

    setValue("flowStepType", mode.flowStepType, {
      shouldValidate: true,
      shouldDirty: true,
    });

    // The last mode's button and action go, and this mode's own start in their place: a click is
    // a single left click, a drag holds the left button, a move and a scroll use neither.
    setValue("cursorButtonActionType", mode.defaults.cursorButtonActionType ?? null, { shouldValidate: true });
    setValue("cursorButtonType", mode.defaults.cursorButtonType ?? null, { shouldValidate: true });

    // In ADD mode nothing is persisted yet, so the name should follow the mode instead of
    // leaving "Cursor Click" on a scroll step. An edited name is left alone.
    if (formMode === "ADD") {
      const isUntouchedName = CURSOR_MODES.some(
        (x) => x.defaultName === form.getValues("name"),
      );
      if (isUntouchedName)
        setValue("name", mode.defaultName, { shouldValidate: true });
    }
  };

  // Fields belonging to the other modes are cleared here rather than left as stale rows in the
  // database, so a step only ever carries the values its type actually uses. The script line under
  // the form is printed from the same step.
  const toDto = (data: z.infer<typeof FlowStepCursorSchema>): FlowStepDto => {
    const isClick = data.flowStepType === FlowStepTypeEnum.CURSOR_CLICK;
    const isDrag = data.flowStepType === FlowStepTypeEnum.CURSOR_DRAG;
    const isScroll = data.flowStepType === FlowStepTypeEnum.CURSOR_SCROLL;
    const hasStartPoint = !isScroll;

    return new FlowStepDto({
      ...defaultValues,
      name: data.name,
      codeComment: data.codeComment ?? "",
      flowStepType: data.flowStepType,

      flowPointId:
        hasStartPoint && data.isPointCustom
          ? (data.flowPointId ?? undefined)
          : undefined,
      flowStepReferenceId:
        hasStartPoint && !data.isPointCustom
          ? (data.flowStepReferenceId ?? undefined)
          : undefined,

      flowPointEndId:
        isDrag && data.isPointEndCustom
          ? (data.flowPointEndId ?? undefined)
          : undefined,
      flowStepReferenceEndId:
        isDrag && !data.isPointEndCustom
          ? (data.flowStepReferenceEndId ?? undefined)
          : undefined,

      cursorButtonActionType: isClick
        ? (data.cursorButtonActionType ?? undefined)
        : undefined,
      cursorButtonType:
        isClick || isDrag ? (data.cursorButtonType ?? undefined) : undefined,

      cursorScrollDirectionType: isScroll
        ? (data.cursorScrollDirectionType ?? undefined)
        : undefined,
      loopCount: isScroll ? data.loopCount : 0,
    });
  };

  return (
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit((data) => onSubmit(toDto(data)))}>
        <FlowStepFormLayoutComponent
          formMode={formMode}
          isValid={isValid}
          isDirty={isDirty}
          onEdit={onEdit}
          onCancel={onCancel}
          baseValues={defaultValues}
          toDto={(values) => toDto(values as z.infer<typeof FlowStepCursorSchema>)}
        >
          <FormInputTextComponent
            fieldName="name"
            label="Name"
            isRequired={true}
            isDisabled={formMode === "VIEW"}
          />

          {/* Each mode is its own FlowStepType, the form just switches between them. */}
          <div className="field">
            <LabelComponent
              text="Mode"
              weight="bold"
            />
            <SelectButton
              value={activeMode.flowStepType}
              options={CURSOR_MODES.map((x) => ({ label: x.label, value: x.flowStepType }))}
              disabled={formMode === "VIEW"}
              onChange={(e) => {
                const mode = CURSOR_MODES.find((x) => x.flowStepType === e.value);
                if (mode) handleModeChange(mode);
              }}
            />
            <LabelComponent
              text={activeMode.description}
              size="xs"
              weight="bold"
              className="mt-1"
            />
          </div>

          <FlowStepCursorFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            parentFlowStepId={defaultValues.parentFlowStepId}
            isDisabled={formMode === "VIEW"}
          />
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
