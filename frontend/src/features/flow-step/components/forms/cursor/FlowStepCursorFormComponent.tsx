import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";
import { useEffect } from "react";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepCursorSchema } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";
import {
  CURSOR_MODES,
  type CursorMode,
} from "@/features/flow-step/components/forms/cursor/cursor-modes";
import FlowStepCursorFormFieldsComponent from "@/features/flow-step/components/forms/cursor/FlowStepCursorFormFieldsComponent";
import { Card } from "primereact/card";
import { classNames } from "primereact/utils";

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
    defaultValues: { ...defaultValues } as any,
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
  // database, so a step only ever carries the values its type actually uses.
  const handleSubmit = (data: z.infer<typeof FlowStepCursorSchema>) => {
    const isClick = data.flowStepType === FlowStepTypeEnum.CURSOR_CLICK;
    const isDrag = data.flowStepType === FlowStepTypeEnum.CURSOR_DRAG;
    const isScroll = data.flowStepType === FlowStepTypeEnum.CURSOR_SCROLL;
    const hasStartPoint = !isScroll;

    onSubmit({
      ...defaultValues,
      name: data.name,
      flowStepType: data.flowStepType,

      isLocationCustom: hasStartPoint ? data.isLocationCustom : false,
      flowLocationId:
        hasStartPoint && data.isLocationCustom
          ? (data.flowLocationId ?? undefined)
          : undefined,
      flowStepReferenceId:
        hasStartPoint && !data.isLocationCustom
          ? (data.flowStepReferenceId ?? undefined)
          : undefined,

      isLocationEndCustom: isDrag ? data.isLocationEndCustom : false,
      flowLocationEndId:
        isDrag && data.isLocationEndCustom
          ? (data.flowLocationEndId ?? undefined)
          : undefined,
      flowStepReferenceEndId:
        isDrag && !data.isLocationEndCustom
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
    <>
      <FormHeaderComponent
        title="Cursor Step Configuration"
        description={activeMode.description}
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          {/* Mode picker. Each mode is its own FlowStepType, the form just switches between them. */}
          <div className="flex gap-3 mt-4">
            {CURSOR_MODES.map((mode) => (
              <Card
                key={mode.flowStepType}
                className={classNames(
                  "flex-1 text-center cursor-pointer border-round-2xl transition-all",
                  mode.flowStepType === flowStepType
                    ? "shadow-4 border-primary border-2"
                    : "shadow-1",
                  { "opacity-60": formMode === "VIEW" },
                )}
                onClick={() => handleModeChange(mode)}
              >
                <div className="flex flex-column align-items-center gap-2">
                  <IconComponent name={mode.iconName} />
                  <LabelComponent
                    text={mode.label}
                    weight="semibold"
                    size="sm"
                  />
                </div>
              </Card>
            ))}
          </div>

          <FlowStepCursorFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            parentFlowStepId={defaultValues.parentFlowStepId}
            isDisabled={formMode === "VIEW"}
          />

          <FormFooterComponent
            formMode={formMode}
            isValid={isValid}
            isDirty={isDirty}
            onCancel={onCancel}
          />
        </form>
      </FormProvider>
    </>
  );
}
