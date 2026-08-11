import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";
import { useEffect, useState } from "react";
import { Button } from "primereact/button";

import { FormFooterComponent } from "@/shared/components/form/FormFooterComponent";
import { FormHeaderComponent } from "@/shared/components/form/FormHeaderComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { RunCommandTestResultDto } from "@/shared/models/database/run-command-test-result-dto";
import { useCommandPresets } from "@/features/flow-step/hooks/use-command-presets";
import { FlowStepSystemCommandSchema } from "@/features/flow-step/components/forms/system-command/flow-step-system-command.zod";
import FlowStepSystemCommandFormFieldsComponent from "@/features/flow-step/components/forms/system-command/FlowStepSystemCommandFormFieldsComponent";
import FlowStepSystemCommandTestPanelComponent from "@/features/flow-step/components/forms/system-command/FlowStepSystemCommandTestPanelComponent";

const CONFIRM_ID = "system-command-test-confirm";

interface Props {
  formMode: FormMode;
  defaultValues: FlowStepDto;
  onSubmit: (formValues: FlowStepDto) => void;
  onCancel: () => void;
  onEdit: () => void;
}

export default function FlowStepSystemCommandFormComponent({
  formMode,
  defaultValues,
  onSubmit,
  onCancel,
  onEdit,
}: Props) {
  const form = useForm<z.infer<typeof FlowStepSystemCommandSchema>>({
    resolver: zodResolver(FlowStepSystemCommandSchema),
    mode: "onChange",
    defaultValues: { ...defaultValues } as never,
  });

  const {
    control,
    formState: { isValid, isDirty },
    trigger,
  } = form;

  const { data: presets = [] } = useCommandPresets();
  const { openConfirm, close } = useDialogStore();

  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<RunCommandTestResultDto | null>(null);

  const preset = useWatch({ control, name: "runCommandPreset" });
  const timeoutMilliseconds = useWatch({ control, name: "timeoutMilliseconds" });
  const activePreset = presets.find((x) => x.preset === preset);

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const runTest = async () => {
    setIsTesting(true);
    try {
      const dto = new FlowStepDto({ ...defaultValues, ...form.getValues() });
      setTestResult(await backendApiService.FlowStep.testRunCommand(dto));
    } catch (err) {
      console.error(err);
    } finally {
      setIsTesting(false);
    }
  };

  // There is no way to preview a command without running it, so the ones that do something the
  // user would not want done twice ask first.
  const handleTest = () => {
    if (!activePreset?.isConfirmationRequired) {
      void runTest();
      return;
    }

    openConfirm(CONFIRM_ID, {
      headerText: "Run this for real?",
      confirmLabel: "Run it",
      confirmSeverity: "warning",
      children: (
        <LabelComponent
          text={`Testing "${activePreset.label}" actually runs it on this machine.`}
          size="sm"
        />
      ),
      onConfirm: async () => {
        close(CONFIRM_ID);
        await runTest();
      },
    });
  };

  const handleSubmit = (data: z.infer<typeof FlowStepSystemCommandSchema>) =>
    onSubmit(new FlowStepDto({ ...defaultValues, ...data }));

  return (
    <>
      <FormHeaderComponent
        title="System Command Step Configuration"
        description="Run a command on this machine, then branch on whether it worked and use what it printed."
        formMode={formMode}
        onEdit={onEdit}
      />

      <FormProvider {...form}>
        <form
          onSubmit={form.handleSubmit(handleSubmit)}
          className="flex flex-column h-full"
        >
          <FlowStepSystemCommandFormFieldsComponent
            presets={presets}
            isDisabled={formMode === "VIEW"}
          />

          <div className="flex align-items-center gap-3">
            <Button
              type="button"
              label={isTesting ? "Running..." : "Test"}
              icon="pi pi-play"
              loading={isTesting}
              disabled={!isValid || isTesting}
              onClick={handleTest}
              className="p-button-outlined"
              tooltip="Runs the command now so you can see what it returns"
              tooltipOptions={{ position: "top" }}
            />

            {timeoutMilliseconds === 0 && (
              <LabelComponent
                text="Give up after is 0, so a command that never finishes keeps this running."
                color="secondary"
                size="sm"
              />
            )}
          </div>

          {testResult && <FlowStepSystemCommandTestPanelComponent result={testResult} />}

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
