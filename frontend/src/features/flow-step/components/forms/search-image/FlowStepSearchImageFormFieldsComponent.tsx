import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import {
  SEARCH_IMAGE_MODES,
  isWaitingMode,
} from "@/features/flow-step/components/forms/shared/search-modes";
import { MATCH_MODES } from "@/features/flow-step/components/forms/shared/match-modes";
import FlowStepSearchAreaFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepSearchAreaFieldComponent";

interface Props {
  flowId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepSearchImageFormFieldsComponent({
  flowId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();

  const mode = useWatch({ control, name: "searchMode" });
  const matchMode = useWatch({ control, name: "templateMatchMode" });

  const isWaiting = isWaitingMode(mode);
  const isFindAll = mode === SearchModeEnum.FIND_ALL;

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormSelectButtonComponent
        fieldName="searchMode"
        labelText="Mode"
        options={SEARCH_IMAGE_MODES.map((x) => ({
          label: x.label,
          value: x.value,
        }))}
        isDisabled={isDisabled}
        isRequired={true}
        hintText={SEARCH_IMAGE_MODES.find((x) => x.value === mode)?.description}
      />

      <FlowStepSearchAreaFieldComponent
        flowId={flowId}
        labelText="Where to look"
        hintText="A smaller area is the single biggest thing that makes the search fast."
        isDisabled={isDisabled}
      />

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

      {isWaiting && (
        <div className="flex gap-3">
          <FormInputNumberComponent
            fieldName="pollIntervalMilliseconds"
            label="Check every (ms)"
            min={50}
            max={2147483647}
            isRequired={true}
            isDisabled={isDisabled}
          />
          <FormInputNumberComponent
            fieldName="timeoutMilliseconds"
            label="Give up after (ms)"
            min={0}
            max={2147483647}
            isDisabled={isDisabled}
            hintText="0 = wait forever"
          />
        </div>
      )}

      <FormSelectButtonComponent
        fieldName="templateMatchMode"
        labelText="Compare"
        options={MATCH_MODES.map((x) => ({
          label: x.label,
          value: x.value,
        }))}
        isRequired={true}
        isDisabled={isDisabled}
        hintText={MATCH_MODES.find((x) => x.value === matchMode)?.description}
      />
    </>
  );
}
