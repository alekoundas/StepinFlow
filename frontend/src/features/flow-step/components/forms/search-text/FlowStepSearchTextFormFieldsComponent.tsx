import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import {
  conditionOptions,
  needsSecondValue,
  needsValue,
  SEARCH_TEXT_CONDITION_TYPES,
} from "@/features/flow-step/components/forms/shared/condition-types";
import {
  isWaitingMode,
  SEARCH_TEXT_MODES,
} from "@/features/flow-step/components/forms/shared/search-modes";
import FlowStepResultExtractFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepResultExtractFieldComponent";
import FlowStepSearchAreaFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepSearchAreaFieldComponent";
import { FlowStepSearchTextSchema } from "@/features/flow-step/components/forms/search-text/flow-step-search-text.zod";
import { useOcrLanguages } from "@/features/settings/hooks/use-ocr-languages";

type SearchTextForm = z.infer<typeof FlowStepSearchTextSchema>;

interface Option {
  label: string;
  value: string;
}

interface Props {
  flowId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepSearchTextFormFieldsComponent({
  flowId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const mode = useWatch({ control, name: "searchMode" });
  const conditionType = useWatch({ control, name: "conditionType" });
  const { data: languages = [] } = useOcrLanguages();

  const isWaiting = isWaitingMode(mode);
  const isPattern = conditionType === ConditionTypeEnum.MATCHES_REGEX;

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
        options={SEARCH_TEXT_MODES.map((x) => ({
          label: x.label,
          value: x.value,
        }))}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={SEARCH_TEXT_MODES.find((x) => x.value === mode)?.description}
      />

      <FlowStepSearchAreaFieldComponent
        flowId={flowId}
        labelText="Where to read"
        hintText="Reading a small area is both faster and far more accurate than reading a screen."
        isDisabled={isDisabled}
      />

      <FormDropdownComponent<SearchTextForm, Option>
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

      {/* Before the condition, because the condition is tested against what this leaves. */}
      <FlowStepResultExtractFieldComponent
        resultDescription="The condition below is tested against what is left, and later steps read it."
        isDisabled={isDisabled}
      />

      {/* Every mode decides, so this is never hidden - only the polling fields below are. */}
      <div className="flex gap-3">
        <FormDropdownComponent<SearchTextForm, Option>
          fieldName="conditionType"
          labelText="Condition"
          mode="local"
          options={conditionOptions(SEARCH_TEXT_CONDITION_TYPES)}
          optionLabel="label"
          optionValue="value"
          isRequired={true}
          isDisabled={isDisabled}
          classNameContainer="flex-1"
        />

        {needsValue(conditionType) && (
          <FormInputTextComponent
            fieldName="conditionText"
            label={isPattern ? "Pattern to match" : "Text to compare"}
            placeholderText={isPattern ? "total: ([0-9]+)" : "Done"}
            isRequired={true}
            isDisabled={isDisabled}
            className="flex-1"
          />
        )}

        {needsSecondValue(conditionType) && (
          <FormInputTextComponent
            fieldName="conditionTextEnd"
            label="And"
            isRequired={true}
            isDisabled={isDisabled}
            className="flex-1"
          />
        )}
      </div>

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
    </>
  );
}
