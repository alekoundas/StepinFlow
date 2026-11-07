import type { FormMode } from "@/shared/enums/form-mode-enum";
import type z from "zod";
import { FlowStepDto } from "@/shared/models/database/flow-step-dto";

import { zodResolver } from "@hookform/resolvers/zod";
import { FormProvider, useForm, useWatch } from "react-hook-form";
import { useEffect, useState } from "react";
import { Button } from "primereact/button";

import LabelComponent from "@/shared/components/LabelComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { ShellCommandResultDto } from "@/shared/models/database/shell-command-result-dto";
import { useShellCommandModes } from "@/features/flow-step/hooks/use-shell-command-modes";
import { FlowStepSystemCommandSchema } from "@/features/flow-step/components/forms/system-command/flow-step-system-command.zod";
import { ShellCommandModeEnum } from "@/shared/enums/backend/shell/shell-command-mode-enum";
import { ShellTypeEnum } from "@/shared/enums/backend/shell/shell-type-enum";
import { humanizeMilliseconds } from "@/shared/utils/humanize-milliseconds";
import { FlowStepFormLayoutComponent } from "@/features/flow-step/components/forms/shared/FlowStepFormLayoutComponent";
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

  const { data: modes = [] } = useShellCommandModes();
  const { openConfirm, close } = useDialogStore();

  const [isTesting, setIsTesting] = useState(false);
  const [testResult, setTestResult] = useState<ShellCommandResultDto | null>(null);

  const values = useWatch({ control });
  const mode = values.shellCommandMode;
  const timeoutMilliseconds = values.timeoutMilliseconds as number | undefined;
  const activeMode = modes.find((x) => x.mode === mode);

  // Testing would start the application, and a launch has no output to show.
  const isLaunch = mode === ShellCommandModeEnum.LAUNCH_APP;

  useEffect(() => {
    const timer = setTimeout(() => {
      trigger();
    }, 0);
    return () => clearTimeout(timer);
  }, [trigger]);

  const executeTest = async () => {
    setIsTesting(true);
    try {
      const values = form.getValues();
      const dto = new FlowStepDto({
        ...defaultValues,
        ...values,
        flowAreaId: values.flowAreaId ?? undefined,
      });
      setTestResult(await backendApiService.FlowStep.testShellCommand(dto));
    } catch (err) {
      console.error(err);
    } finally {
      setIsTesting(false);
    }
  };

  // There is no way to preview a command without running it, so the ones that do something the
  // user would not want done twice ask first.
  const handleTest = () => {
    if (!activeMode?.isConfirmationRequired) {
      void executeTest();
      return;
    }

    openConfirm(CONFIRM_ID, {
      headerText: "Run this for real?",
      confirmLabel: "Run it",
      confirmSeverity: "warning",
      children: (
        <LabelComponent
          text={`Testing "${activeMode.label}" actually runs it on this machine.`}
          size="sm"
        />
      ),
      onConfirm: async () => {
        close(CONFIRM_ID);
        await executeTest();
      },
    });
  };

  // A field hidden for what the step is goes empty, so it cannot come back unseen.
  const handleSubmit = (data: z.infer<typeof FlowStepSystemCommandSchema>) => {
    const isLaunchOfArea = data.shellCommandMode === ShellCommandModeEnum.LAUNCH_APP && !!data.flowAreaId;

    onSubmit(
      new FlowStepDto({
        ...defaultValues,
        ...data,
        flowAreaId: data.shellCommandMode === ShellCommandModeEnum.LAUNCH_APP ? (data.flowAreaId ?? undefined) : undefined,
        shellCommandValue: isLaunchOfArea ? "" : data.shellCommandValue,
        shellArguments: isLaunchOfArea ? data.shellArguments : "",
      }),
    );
  };

  // What this one will do, in its own words: the command as it will be executed, or the program
  // started.
  const summaryOf = (): string | undefined => {
    if (!activeMode) return undefined;

    const value = (values.shellCommandValue as string | undefined) ?? "";
    const codes = (values.shellSuccessExitCodes as string | undefined) || "0";

    if (isLaunch) {
      if (!values.flowAreaId) return `Starts “${value}” and carries on without waiting for it to close.`;

      const args = ((values.shellArguments as string | undefined) ?? "").trim();
      const after = args ? `, with “${args}” after it` : "";
      return `Starts what the app is started by${after}, when it is not open, and waits up to ${humanizeMilliseconds(timeoutMilliseconds || 30000)} for it to load.`;
    }

    const command =
      mode === ShellCommandModeEnum.CUSTOM
        ? value
        : activeMode.commandTemplate.replace("{0}", value);
    const shell = values.shellType === ShellTypeEnum.POWERSHELL ? "PowerShell" : "Command Prompt";

    return `Executes “${command}” in ${shell}. Exit code ${codes} goes on to Success, any other to Failure.`;
  };

  return (
    <FormProvider {...form}>
      <form onSubmit={form.handleSubmit(handleSubmit)}>
        <FlowStepFormLayoutComponent
          formMode={formMode}
          isValid={isValid}
          isDirty={isDirty}
          onEdit={onEdit}
          onCancel={onCancel}
          baseValues={defaultValues}
          mode={activeMode?.label}
          summary={summaryOf()}
          testAction={
            !isLaunch && (
              <Button
                type="button"
                label={isTesting ? "Testing..." : "Test now"}
                icon="pi pi-play"
                loading={isTesting}
                disabled={!isValid || isTesting}
                onClick={handleTest}
                outlined
                tooltip="Executes the command now so you can see what it returns"
                tooltipOptions={{ position: "top" }}
              />
            )
          }
        >
          <FlowStepSystemCommandFormFieldsComponent
            modes={modes}
            flowId={defaultValues.flowId ?? defaultValues.rootId}
            isDisabled={formMode === "VIEW"}
          />

          {!isLaunch && timeoutMilliseconds === 0 && (
            <LabelComponent
              text="Give up after is 0, so a test of a command that never finishes keeps going."
              color="secondary"
              size="sm"
            />
          )}

          {!isLaunch && testResult && <FlowStepSystemCommandTestPanelComponent result={testResult} />}
        </FlowStepFormLayoutComponent>
      </form>
    </FormProvider>
  );
}
