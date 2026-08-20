import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import {
  isWaitingMode,
  SEARCH_MODES,
} from "@/features/flow-step/components/forms/image-search/search-modes";
import FlowStepResultFieldsComponent from "@/features/flow-step/components/forms/shared/FlowStepResultFieldsComponent";
import FlowStepSearchAreaFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepSearchAreaFieldComponent";
import {
  FlowStepTextSearchSchema,
  TEXT_SEARCH_CONDITION_TYPES,
} from "@/features/flow-step/components/forms/text-search/flow-step-text-search.zod";

type TextSearchForm = z.infer<typeof FlowStepTextSearchSchema>;

interface EnumOption {
  label: string;
  value: string;
}

interface Props {
  flowId: number | undefined;
  isDisabled?: boolean;
}

const MATCH_OPTIONS: Record<(typeof TEXT_SEARCH_CONDITION_TYPES)[number], string> = {
  [ConditionTypeEnum.CONTAINS]: "Contains",
  [ConditionTypeEnum.EQUALS]: "Is exactly",
  [ConditionTypeEnum.MATCHES_REGEX]: "Matches pattern",
};

export default function FlowStepTextSearchFormFieldsComponent({
  flowId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const mode = useWatch({ control, name: "searchMode" });

  const isWaiting = isWaitingMode(mode);
  const isFindAll = mode === SearchModeEnum.FIND_ALL;

  const matchOptions: EnumOption[] = TEXT_SEARCH_CONDITION_TYPES.map((value) => ({
    label: MATCH_OPTIONS[value],
    value,
  }));

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
        options={SEARCH_MODES.map((x) => ({
          label: x.label,
          value: x.value,
        }))}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={SEARCH_MODES.find((x) => x.value === mode)?.description}
      />

      <FlowStepSearchAreaFieldComponent
        flowId={flowId}
        labelText="Where to read"
        hintText="Reading a small area is both faster and far more accurate than reading a screen."
        isDisabled={isDisabled}
      />

      <div className="flex gap-3">
        <FormDropdownComponent<TextSearchForm, EnumOption>
          fieldName="conditionType"
          labelText="Match"
          mode="local"
          options={matchOptions}
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

      <FormInputTextComponent
        fieldName="ocrLanguage"
        label="Language"
        placeholderText="en-US"
        isRequired={true}
        isDisabled={isDisabled}
        hintText="Windows reads text with the language packs it has installed."
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

      {isFindAll && (
        <FormInputNumberComponent
          fieldName="maxMatches"
          label="Stop after"
          min={1}
          max={2147483647}
          isRequired={true}
          isDisabled={isDisabled}
          hintText="Safety cap, so a loose threshold cannot fire hundreds of times."
        />
      )}

      <FlowStepResultFieldsComponent
        resultDescription="Holds the text that was read."
        isDisabled={isDisabled}
      />
    </>
  );
}
