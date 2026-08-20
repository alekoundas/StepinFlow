import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ConditionTypeEnum } from "@/shared/enums/backend/condition-type-enum";
import { StepResultKindEnum } from "@/shared/enums/backend/step-result-kind-enum";
import {
  conditionOptions,
  needsSecondValue,
  needsValue,
} from "@/features/flow-step/components/forms/shared/condition-types";
import { FlowStepCheckValueSchema } from "@/features/flow-step/components/forms/check-value/flow-step-check-value.zod";

type CheckValueForm = z.infer<typeof FlowStepCheckValueSchema>;

interface StepOption {
  label: string;
  value: number;
  description?: string;
}

interface ConditionOption {
  label: string;
  value: string;
}

interface Props {
  parentFlowStepId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepCheckValueFormFieldsComponent({
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const conditionType = useWatch({ control, name: "conditionType" });

  const loadStepReferences = (filter?: string): Promise<StepOption[]> =>
    backendApiService.Lookup.flowStep({
      searchText: filter,
      flowStepId: parentFlowStepId,
      resultKind: StepResultKindEnum.VALUE,
    }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: Number(item.value),
        description: item.description,
      })),
    );

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      <FormDropdownComponent<CheckValueForm, StepOption>
        fieldName="flowStepReferenceId"
        labelText="Check the result of"
        mode="remote"
        queryKey={["lookup", "flowStep", "value", parentFlowStepId]}
        queryFn={loadStepReferences}
        optionLabel="label"
        optionValue="value"
        placeholderText="Select a step..."
        hintText="Only Read Text / System Command steps this step runs under can be used."
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormDropdownComponent<CheckValueForm, ConditionOption>
        fieldName="conditionType"
        labelText="Condition"
        mode="local"
        options={conditionOptions(Object.values(ConditionTypeEnum))}
        optionLabel="label"
        optionValue="value"
        isRequired={true}
        isDisabled={isDisabled}
      />

      {needsValue(conditionType) && (
        <div className="flex gap-3">
          <FormInputTextComponent
            fieldName="conditionText"
            label={needsSecondValue(conditionType) ? "From" : "Value"}
            isRequired={true}
            isDisabled={isDisabled}
            className="flex-1"
          />

          {needsSecondValue(conditionType) && (
            <FormInputTextComponent
              fieldName="conditionTextEnd"
              label="To"
              isRequired={true}
              isDisabled={isDisabled}
              className="flex-1"
            />
          )}
        </div>
      )}
    </>
  );
}
