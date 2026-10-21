import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";
import { useEffect } from "react";
import { SelectButton } from "primereact/selectbutton";

import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepWindowSchema } from "@/features/flow-step/components/forms/window/flow-step-window.zod";
import {
  WINDOW_MODES,
  type WindowMode,
} from "@/features/flow-step/components/forms/window/window-modes";
import FlowStepWindowFormFieldsComponent from "@/features/flow-step/components/forms/window/FlowStepWindowFormFieldsComponent";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepWindowFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepWindowSchema>>({
    resolver: zodResolver(FlowStepWindowSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    control,
    formState: { isValid, isDirty },
    setValue,
    trigger,
  } = form;

  const flowStepType = useWatch({ control, name: "flowStepType" });
  const activeMode =
    WINDOW_MODES.find((x) => x.flowStepType === flowStepType) ?? WINDOW_MODES[0];

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const handleModeChange = (mode: WindowMode) => {
    if (formMode === "VIEW" || mode.flowStepType === flowStepType) return;

    setValue("flowStepType", mode.flowStepType, {
      shouldValidate: true,
      shouldDirty: true,
    });
  };

  // Fields belonging to the other modes are cleared so a focus step never carries a stale size.
  // The script line under the form is printed from the same step.
  const toDto = (data: z.infer<typeof FlowStepWindowSchema>): FlowStepDto => {
    const isResize = data.flowStepType === FlowStepTypeEnum.WINDOW_RESIZE;
    const isRelocate = data.flowStepType === FlowStepTypeEnum.WINDOW_RELOCATE;

    return new FlowStepDto({
      ...defaultValues,
      codeComment: data.codeComment ?? "",
      flowStepType: data.flowStepType,

      processName: data.processName,
      titlePattern: data.titlePattern,
      titleMatchMode: data.titleMatchMode,

      windowWidth: isResize ? data.windowWidth : 0,
      windowHeight: isResize ? data.windowHeight : 0,

      flowPointId: isRelocate ? (data.flowPointId ?? undefined) : undefined,
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
          toDto={(values) => toDto(values as z.infer<typeof FlowStepWindowSchema>)}
        >
          {/* Each mode is its own FlowStepType, the form just switches between them. */}
          <div className="field">
            <LabelComponent
              text="Mode"
              weight="bold"
            />
            <SelectButton
              value={activeMode.flowStepType}
              options={WINDOW_MODES.map((x) => ({ label: x.label, value: x.flowStepType }))}
              disabled={formMode === "VIEW"}
              onChange={(e) => {
                const mode = WINDOW_MODES.find((x) => x.flowStepType === e.value);
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

          <FlowStepWindowFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            isDisabled={formMode === "VIEW"}
          />
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
