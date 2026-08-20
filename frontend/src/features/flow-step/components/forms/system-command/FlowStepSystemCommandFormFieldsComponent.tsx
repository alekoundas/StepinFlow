import type z from "zod";
import { useEffect } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { Card } from "primereact/card";
import { classNames } from "primereact/utils";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { RunCommandShellEnum } from "@/shared/enums/backend/command/run-command-shell-enum";
import { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import { ResultSourceEnum } from "@/shared/enums/backend/command/result-source-enum";
import type { CommandPresetDto } from "@/shared/models/database/command-preset-dto";
import FlowStepResultExtractFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepResultExtractFieldComponent";
import { FlowStepSystemCommandSchema } from "@/features/flow-step/components/forms/system-command/flow-step-system-command.zod";

type SystemCommandForm = z.infer<typeof FlowStepSystemCommandSchema>;

interface EnumOption {
  label: string;
  value: string;
}

interface Props {
  presets: CommandPresetDto[];
  isDisabled?: boolean;
}

const RESULT_SOURCE_OPTIONS: EnumOption[] = [
  { label: "Output", value: ResultSourceEnum.STDOUT },
  { label: "Errors", value: ResultSourceEnum.STDERR },
  { label: "Both", value: ResultSourceEnum.COMBINED },
  { label: "Exit code", value: ResultSourceEnum.EXIT_CODE },
];

export default function FlowStepSystemCommandFormFieldsComponent({
  presets,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  const preset = useWatch({ control, name: "runCommandPreset" });
  const presetValue = useWatch({ control, name: "runCommandPresetValue" });
  const runCommand = useWatch({ control, name: "runCommand" });

  const activePreset = presets.find((x) => x.preset === preset);
  const isCustom = preset === RunCommandPresetEnum.CUSTOM;

  // A preset owns its shell, so picking one has to bring its shell with it. Its default parameter
  // comes along too, otherwise the previewed command shows a hole where the value belongs.
  useEffect(() => {
    if (!activePreset || isCustom) return;

    setValue("runCommandShell", activePreset.shell, { shouldValidate: true });

    if (activePreset.hasParameter && !presetValue)
      setValue("runCommandPresetValue", activePreset.parameterDefault, {
        shouldValidate: true,
      });

    if (!activePreset.hasParameter && presetValue)
      setValue("runCommandPresetValue", "", { shouldValidate: true });
  }, [activePreset, isCustom, presetValue, setValue]);

  const resolvedCommand = !activePreset
    ? ""
    : isCustom
      ? runCommand
      : activePreset.hasParameter
        ? activePreset.commandTemplate.replace("{0}", presetValue ?? "")
        : activePreset.commandTemplate;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      <LabelComponent
        text="Command"
        weight="bold"
      />
      <div className="flex flex-wrap gap-2 mb-3">
        {presets.map((item) => (
          <Card
            key={item.preset}
            className={classNames(
              "cursor-pointer border-round-2xl transition-all",
              item.preset === preset ? "shadow-4 border-primary border-2" : "shadow-1",
              { "opacity-60": isDisabled },
            )}
            onClick={() =>
              !isDisabled &&
              setValue("runCommandPreset", item.preset, {
                shouldValidate: true,
                shouldDirty: true,
              })
            }
          >
            <LabelComponent
              text={item.label}
              weight="semibold"
              size="sm"
              wrap={false}
            />
          </Card>
        ))}
      </div>

      {activePreset && (
        <LabelComponent
          text={activePreset.description}
          color="secondary"
          size="sm"
          className="mb-3"
        />
      )}

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
            fieldName="runCommand"
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
            fieldName="runCommandPresetValue"
            label={activePreset.parameterLabel}
            placeholderText={activePreset.parameterPlaceholder}
            isRequired={true}
            isDisabled={isDisabled}
          />
        )
      )}

      {!isCustom && resolvedCommand.length > 0 && (
        <div className="mb-3">
          <LabelComponent
            text="Runs"
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
        label="Run from folder"
        placeholderText="C:\\Projects"
        isDisabled={isDisabled}
        hintText="Optional. The command runs here instead of wherever the app happens to be."
      />

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
  );
}
