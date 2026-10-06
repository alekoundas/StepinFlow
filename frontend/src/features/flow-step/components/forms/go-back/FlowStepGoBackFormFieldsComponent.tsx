import { useQuery } from "@tanstack/react-query";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

interface IdOption {
  label: string;
  value: number;
  description?: string;
}

interface Props {
  rootId: number;
  /** Undefined at the root of the flow. */
  parentFlowStepId: number | undefined;
  /** In ADD mode there is no step row yet, so its position stands in for it. */
  orderNumber: number;
  isDisabled?: boolean;
}

export default function FlowStepGoBackFormFieldsComponent({
  rootId,
  parentFlowStepId,
  orderNumber,
  isDisabled = false,
}: Props) {
  const { data: targets = [], isFetched } = useQuery({
    queryKey: ["lookup", "goBack", rootId, parentFlowStepId, orderNumber],
    queryFn: () =>
      backendApiService.Lookup.goBack({
        flowId: rootId,
        flowStepId: parentFlowStepId,
        orderNumber: orderNumber,
      }).then((res) =>
        res.data.map((item) => ({
          label: item.label,
          value: Number(item.value),
          description: item.description,
        })),
      ),
    enabled: rootId > 0,
  });

  return (
    <div className="flex flex-column gap-2 mt-4">
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        hintText="What this step is called in the tree."
        isRequired={true}
        isDisabled={isDisabled}
      />

      {isFetched && targets.length === 0 ? (
        <Message
          severity="info"
          className="justify-content-start"
          text="Nothing runs before this step, so there is nowhere to go back to. Put a step above it first."
        />
      ) : (
        <FormDropdownComponent<FlowStepDto, IdOption>
          fieldName="flowStepReferenceId"
          labelText="Go back to"
          mode="local"
          options={targets}
          optionLabel="label"
          optionValue="value"
          placeholderText="Select the step..."
          isRequired={true}
          isDisabled={isDisabled}
          hintText="Only steps this one passes on the way here, nearest first: the ones above it, the step it sits under, and the ones above that. Never the other branch of a check, and never inside a loop or check that already finished."
          itemTemplate={(item) => (
            <div className="flex flex-column">
              <LabelComponent text={item.label} />
              {item.description && (
                <LabelComponent
                  text={item.description}
                  size="xs"
                  color="secondary"
                />
              )}
            </div>
          )}
        />
      )}
    </div>
  );
}
