import type z from "zod";
import { useEffect } from "react";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { RunCommandShellEnum } from "@/shared/enums/backend/command/run-command-shell-enum";
import { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import { ResultSourceEnum } from "@/shared/enums/backend/command/result-source-enum";
import type { CommandPresetDto } from "@/shared/models/database/command-preset-dto";
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
  presets: CommandPresetDto[];
  flowId: number | undefined;
  isDisabled?: boolean;
}

// A launch waits for a window, and a monitor or a region is always there.
const WINDOW_AREA_TYPES: string[] = [FlowAreaTypeEnum.APPLICATION, FlowAreaTypeEnum.BROWSER_TAB];

const RESULT_SOURCE_OPTIONS: EnumOption[] = [
  { label: "Output", value: ResultSourceEnum.STDOUT },
  { label: "Errors", value: ResultSourceEnum.STDERR },
  { label: "Both", value: ResultSourceEnum.COMBINED },
  { label: "Exit code", value: ResultSourceEnum.EXIT_CODE },
];

export default function FlowStepSystemCommandFormFieldsComponent({
  presets,
  flowId,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  const preset = useWatch({ control, name: "runCommandPreset" });
  const commandValue = useWatch({ control, name: "runCommandValue" });

  const activePreset = presets.find((x) => x.preset === preset);
  const isCustom = preset === RunCommandPresetEnum.CUSTOM;
  const isLaunch = preset === RunCommandPresetEnum.LAUNCH_APP;

  const loadWindowAreas = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.flowArea({ searchText: filter, flowId }).then((res) =>
      res.data
        .filter((item) => WINDOW_AREA_TYPES.includes(item.description ?? ""))
        .map((item) => ({ label: item.label, value: Number(item.value) })),
    );

  // A preset owns its shell, so picking one has to bring its shell with it. Its default parameter
  // comes along too, otherwise the previewed command shows a hole where the value belongs.
  useEffect(() => {
    if (!activePreset || isCustom) return;

    setValue("runCommandShell", activePreset.shell, { shouldValidate: true });

    if (activePreset.hasParameter && !commandValue)
      setValue("runCommandValue", activePreset.parameterDefault, {
        shouldValidate: true,
      });

    if (!activePreset.hasParameter && commandValue)
      setValue("runCommandValue", "", { shouldValidate: true });
  }, [activePreset, isCustom, commandValue, setValue]);

  const resolvedCommand = !activePreset
    ? ""
    : isCustom
      ? commandValue
      : activePreset.hasParameter
        ? activePreset.commandTemplate.replace("{0}", commandValue ?? "")
        : activePreset.commandTemplate;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormSelectButtonComponent<SystemCommandForm, string>
        fieldName="runCommandPreset"
        labelText="Command"
        options={presets.map((x) => ({ label: x.label, value: x.preset }))}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={activePreset?.description}
      />

      {isCustom ? (
        <>
          <FormSelectButtonComponent
            fieldName="runCommandShell"
            labelText="Shell"
            options={[
              { label: "Command Prompt", value: RunCommandShellEnum.CMD },
              { label: "PowerShell", value: RunCommandShellEnum.POWERSHELL },
            ]}
            isDisabled={isDisabled}
          />

          <FormInputTextComponent
            fieldName="runCommandValue"
            label="Command"
            placeholderText="ipconfig /flushdns"
            isRequired={true}
            isDisabled={isDisabled}
            hintText="One line. Chain several with & in Command Prompt or ; in PowerShell."
          />
        </>
      ) : (
        activePreset?.hasParameter && (
          <FormInputTextComponent
            fieldName="runCommandValue"
            label={activePreset.parameterLabel}
            placeholderText={activePreset.parameterPlaceholder}
            isRequired={true}
            isDisabled={isDisabled}
          />
        )
      )}

      {isLaunch && (
        <FormDropdownComponent<FlowStepDto, IdOption>
          fieldName="flowAreaId"
          labelText="Its window"
          mode="remote"
          queryKey={["lookup", "flowArea", "windows", flowId]}
          queryFn={loadWindowAreas}
          optionLabel="label"
          optionValue="value"
          placeholderText="Select an area..."
          isDisabled={isDisabled}
          hintText="Optional. Left alone when it is already open, and waited for when it is not, so the next step finds it loaded."
        />
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
        fieldName="runCommandWorkingDirectory"
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
              fieldName="successExitCodes"
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
            fieldName="resultSource"
            labelText="Result comes from"
            mode="local"
            options={RESULT_SOURCE_OPTIONS}
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
