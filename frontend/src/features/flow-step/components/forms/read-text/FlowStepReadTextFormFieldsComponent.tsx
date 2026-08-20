import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import {
  conditionOptions,
  READ_TEXT_CONDITION_TYPES,
} from "@/features/flow-step/components/forms/shared/condition-types";
import {
  isWaitingMode,
  READ_TEXT_MODES,
} from "@/features/flow-step/components/forms/shared/search-modes";
import FlowStepResultExtractFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepResultExtractFieldComponent";
import FlowStepSearchAreaFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepSearchAreaFieldComponent";
import { FlowStepReadTextSchema } from "@/features/flow-step/components/forms/read-text/flow-step-read-text.zod";
import { useOcrLanguages } from "@/features/settings/hooks/use-ocr-languages";

type ReadTextForm = z.infer<typeof FlowStepReadTextSchema>;

interface Option {
  label: string;
  value: string;
}

interface Props {
  flowId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepReadTextFormFieldsComponent({
  flowId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const mode = useWatch({ control, name: "searchMode" });
  const { data: languages = [] } = useOcrLanguages();

  const isWaiting = isWaitingMode(mode);

  // Only an installed pack can be read, so an uninstalled one is not offered here. Settings is
  // where that gets fixed.
  const languageOptions: Option[] = languages
    .filter((x) => x.isInstalled)
    .map((x) => ({ label: x.displayName, value: x.tag }));

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      <FormSelectButtonComponent
        fieldName="searchMode"
        labelText="Mode"
        options={READ_TEXT_MODES.map((x) => ({
          label: x.label,
          value: x.value,
        }))}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={READ_TEXT_MODES.find((x) => x.value === mode)?.description}
      />

      <FlowStepSearchAreaFieldComponent
        flowId={flowId}
        labelText="Where to read"
        hintText="Reading a small area is both faster and far more accurate than reading a screen."
        isDisabled={isDisabled}
      />

      <div className="flex gap-3">
        <FormDropdownComponent<ReadTextForm, Option>
          fieldName="conditionType"
          labelText="Match"
          mode="local"
          options={conditionOptions(READ_TEXT_CONDITION_TYPES)}
          optionLabel="label"
          optionValue="value"
          isRequired={true}
          isDisabled={isDisabled}
          classNameContainer="flex-1"
        />

        <FormInputTextComponent
          fieldName="conditionText"
          label="Text to find"
          isRequired={true}
          isDisabled={isDisabled}
          className="flex-1"
        />
      </div>

      <FormDropdownComponent<ReadTextForm, Option>
        fieldName="ocrLanguage"
        labelText="Language"
        mode="local"
        options={languageOptions}
        optionLabel="label"
        optionValue="value"
        placeholderText="Select a language..."
        isRequired={true}
        isDisabled={isDisabled}
        hintText="Windows reads text with the language packs it has installed. Add more in Settings."
      />

      {isWaiting && (
        <div className="flex gap-3">
          <FormInputNumberComponent
            fieldName="pollIntervalMilliseconds"
            label="Check every (ms)"
            min={50}
            max={2147483647}
            isRequired={true}
            isDisabled={isDisabled}
            className="flex-1"
          />
          <FormInputNumberComponent
            fieldName="timeoutMilliseconds"
            label="Give up after (ms)"
            min={0}
            max={2147483647}
            isDisabled={isDisabled}
            className="flex-1"
            hintText="0 = wait forever"
          />
        </div>
      )}

      <FlowStepResultExtractFieldComponent
        resultDescription="Narrows the text this step hands to a Check Value step."
        isDisabled={isDisabled}
      />
    </>
  );
}
