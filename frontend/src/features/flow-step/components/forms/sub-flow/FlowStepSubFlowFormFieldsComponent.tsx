import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepSubFlowSchema } from "@/features/flow-step/components/forms/sub-flow/flow-step-sub-flow.zod";
import SubFlowPreviewComponent from "@/features/flow-step/components/forms/sub-flow/SubFlowPreviewComponent";

type SubFlowForm = z.infer<typeof FlowStepSubFlowSchema>;

interface FlowOption {
  label: string;
  value: number;
  description?: string;
}

interface Props {
  isDisabled?: boolean;
}

export default function FlowStepSubFlowFormFieldsComponent({ isDisabled = false }: Props) {
  const { control } = useFormContext();
  const subFlowId = useWatch({ control, name: "subFlowId" });

  const loadSubFlows = (filter?: string): Promise<FlowOption[]> =>
    backendApiService.Lookup.subFlow({ searchText: filter }).then((res) =>
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

      <FormDropdownComponent<SubFlowForm, FlowOption>
        fieldName="subFlowId"
        labelText="Flow to run"
        mode="remote"
        queryKey={["lookup", "subFlow"]}
        queryFn={loadSubFlows}
        optionLabel="label"
        optionValue="value"
        placeholderText="Select a sub-flow..."
        hintText="Only flows promoted to sub-flows can be run this way."
        isRequired={true}
        isDisabled={isDisabled}
      />

      {subFlowId && <SubFlowPreviewComponent flowId={subFlowId} />}
    </>
  );
}
