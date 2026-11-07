import type z from "zod";
import { useEffect } from "react";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { ShellTypeEnum } from "@/shared/enums/backend/shell/shell-type-enum";
import { ShellCommandModeEnum } from "@/shared/enums/backend/shell/shell-command-mode-enum";
import { ShellResultSourceEnum } from "@/shared/enums/backend/shell/shell-result-source-enum";
import type { ShellCommandModeDto } from "@/shared/models/database/shell-command-mode-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { backendApiService } from "@/shared/services/backend-api-service";
import FlowStepResultExtractFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepResultExtractFieldComponent";
import { FlowStepSystemCommandSchema } from "@/features/flow-step/components/forms/system-command/flow-step-system-command.zod";

type SystemCommandForm = z.infer<typeof FlowStepSystemCommandSchema>;

interface EnumOption {
  label: string;
  value: string;
}

interface IdOption {
  label: string;
  value: number;
}

interface Props {
  modes: ShellCommandModeDto[];
  flowId: number | undefined;
  isDisabled?: boolean;
}

// A launch waits for a window, and a monitor or a region is always there.
const WINDOW_AREA_TYPES: string[] = [FlowAreaTypeEnum.APPLICATION, FlowAreaTypeEnum.BROWSER_TAB];

const SHELL_RESULT_SOURCE_OPTIONS: EnumOption[] = [
  { label: "Output", value: ShellResultSourceEnum.STDOUT },
  { label: "Errors", value: ShellResultSourceEnum.STDERR },
  { label: "Both", value: ShellResultSourceEnum.COMBINED },
  { label: "Exit code", value: ShellResultSourceEnum.EXIT_CODE },
];

export default function FlowStepSystemCommandFormFieldsComponent({
  modes,
  flowId,
  isDisabled = false,
}: Props) {
  const { control, setValue, trigger } = useFormContext();

  const mode = useWatch({ control, name: "shellCommandMode" });
  const commandValue = useWatch({ control, name: "shellCommandValue" });
  const flowAreaId = useWatch({ control, name: "flowAreaId" }) as number | null | undefined;

  const activeMode = modes.find((x) => x.mode === mode);
  const isCustom = mode === ShellCommandModeEnum.CUSTOM;
  const isLaunch = mode === ShellCommandModeEnum.LAUNCH_APP;

  const loadWindowAreas = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.flowArea({ searchText: filter, flowId }).then((res) =>
      res.data
        .filter((item) => WINDOW_AREA_TYPES.includes(item.description ?? ""))
        .map((item) => ({ label: item.label, value: Number(item.value) })),
    );

  // A mode owns its shell, so picking one has to bring its shell with it. Its default parameter
  // comes along too, otherwise the previewed command shows a hole where the value belongs.
  useEffect(() => {
    if (!activeMode || isCustom) return;

    setValue("shellType", activeMode.shellType, { shouldValidate: true });

    if (activeMode.hasParameter && !commandValue)
      setValue("shellCommandValue", activeMode.parameterDefault, {
        shouldValidate: true,
      });

    if (!activeMode.hasParameter && commandValue)
      setValue("shellCommandValue", "", { shouldValidate: true });
  }, [activeMode, isCustom, commandValue, setValue]);

  const resolvedCommand = !activeMode
    ? ""
    : isCustom
      ? commandValue
      : activeMode.hasParameter
        ? activeMode.commandTemplate.replace("{0}", commandValue ?? "")
        : activeMode.commandTemplate;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormSelectButtonComponent<SystemCommandForm, string>
        fieldName="shellCommandMode"
        labelText="Command"
        options={modes.map((x) => ({ label: x.label, value: x.mode }))}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={activeMode?.description}
      />

      {isLaunch && (
        <FormDropdownComponent<FlowStepDto, IdOption>
          fieldName="flowAreaId"
          labelText="Opens"
          mode="remote"
          queryKey={["lookup", "flowArea", "windows", flowId]}
          queryFn={loadWindowAreas}
          optionLabel="label"
          optionValue="value"
          placeholderText="Select an app..."
          isDisabled={isDisabled}
          hintText="Launches it only when it is not open, then waits for it to load."
          // Whether the program is needed depends on this, and the form checks only the field that changed.
          onChanged={() => void trigger("shellCommandValue")}
        />
      )}

      {isLaunch && flowAreaId && (
        <FormInputTextComponent
          fieldName="shellArguments"
          label="Arguments"
          placeholderText="--profile test"
          isDisabled={isDisabled}
          hintText="Optional. They go after what the app is started by on the Areas tab: a fresh browser profile or a window size, say."
        />
      )}

      {isLaunch && !flowAreaId && activeMode && (
        <FormInputTextComponent
          fieldName="shellCommandValue"
          label={activeMode.parameterLabel}
          placeholderText={activeMode.parameterPlaceholder}
          isRequired={true}
          isDisabled={isDisabled}
        />
      )}

      {isCustom ? (
        <>
          <FormSelectButtonComponent
            fieldName="shellType"
            labelText="Shell"
            options={[
              { label: "Command Prompt", value: ShellTypeEnum.CMD },
              { label: "PowerShell", value: ShellTypeEnum.POWERSHELL },
            ]}
            isDisabled={isDisabled}
          />

          <FormInputTextComponent
            fieldName="shellCommandValue"
            label="Command"
            placeholderText="ipconfig /flushdns"
            isRequired={true}
            isDisabled={isDisabled}
            hintText="One line. Chain several with & in Command Prompt or ; in PowerShell."
          />
        </>
      ) : (
        !isLaunch &&
        activeMode?.hasParameter && (
          <FormInputTextComponent
            fieldName="shellCommandValue"
            label={activeMode.parameterLabel}
            placeholderText={activeMode.parameterPlaceholder}
            isRequired={true}
            isDisabled={isDisabled}
          />
        )
      )}

      {!isCustom && !isLaunch && resolvedCommand.length > 0 && (
        <div className="mb-3">
          <LabelComponent
            text="Executes"
            weight="bold"
            size="sm"
          />
          <pre className="m-0 mt-1 p-2 surface-100 border-round text-sm overflow-auto white-space-pre-wrap">
            {resolvedCommand}
          </pre>
        </div>
      )}

      <FormInputTextComponent
        fieldName="shellWorkingDirectory"
        label="Start in folder"
        placeholderText="C:\\Projects"
        isDisabled={isDisabled}
        hintText="Optional. The command starts here instead of wherever the app happens to be."
      />

      {isLaunch && (
        <FormInputNumberComponent
          fieldName="timeoutMilliseconds"
          label="Wait for its window up to (ms)"
          min={0}
          max={2147483647}
          isDisabled={isDisabled}
          hintText="0 waits 30 seconds."
        />
      )}

      {!isLaunch && (
        <>
          <div className="flex gap-3">
            <FormInputTextComponent
              fieldName="shellSuccessExitCodes"
              label="Succeeds on exit code"
              isRequired={true}
              isDisabled={isDisabled}
              className="flex-1"
              hintText="Anything else runs the Failure children."
            />

            <FormInputNumberComponent
              fieldName="timeoutMilliseconds"
              label="Give up after (ms)"
              min={0}
              max={2147483647}
              isDisabled={isDisabled}
              className="flex-1"
              hintText="0 waits forever."
            />
          </div>

          <FormDropdownComponent<SystemCommandForm, EnumOption>
            fieldName="shellResultSource"
            labelText="Result comes from"
            mode="local"
            options={SHELL_RESULT_SOURCE_OPTIONS}
            optionLabel="label"
            optionValue="value"
            isRequired={true}
            isDisabled={isDisabled}
          />

          <FlowStepResultExtractFieldComponent
            resultDescription="Narrows the output this step hands to a Check Value step."
            isDisabled={isDisabled}
          />
        </>
      )}
    </>
  );
}
