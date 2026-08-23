import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";
import { useEffect } from "react";
import { Card } from "primereact/card";
import { classNames } from "primereact/utils";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepWindowSchema } from "@/features/flow-step/components/forms/window/flow-step-window.zod";
import {
  WINDOW_MODES,
  type WindowMode,
} from "@/features/flow-step/components/forms/window/window-modes";
import FlowStepWindowFormFieldsComponent from "@/features/flow-step/components/forms/window/FlowStepWindowFormFieldsComponent";

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

    if (formMode === "ADD") {
      const isUntouchedName = WINDOW_MODES.some(
        (x) => x.defaultName === form.getValues("name"),
      );
      if (isUntouchedName)
        setValue("name", mode.defaultName, { shouldValidate: true });
    }
  };

  // Fields belonging to the other modes are cleared so a focus step never carries a stale size.
  const handleSubmit = (data: z.infer<typeof FlowStepWindowSchema>) => {
    const isResize = data.flowStepType === FlowStepTypeEnum.WINDOW_RESIZE;
    const isRelocate = data.flowStepType === FlowStepTypeEnum.WINDOW_RELOCATE;

    onSubmit({
      ...defaultValues,
      name: data.name,
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
    <>
      <FormHeaderComponent
        title="Window Step Configuration"
        description={activeMode.description}
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <div className="flex gap-3 mt-4">
            {WINDOW_MODES.map((mode) => (
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

          <FlowStepWindowFormFieldsComponent
            flowId={defaultValues.flowId ?? defaultValues.rootId}
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
