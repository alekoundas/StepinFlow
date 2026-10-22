import { useEffect } from "react";
import { useFormContext } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { getFlowStepCatalogEntry } from "@/shared/models/flow-step-catalog";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";

interface TargetOption {
  label: string;
  value: number;
  type: FlowStepTypeEnum;
  description?: string;
}

interface Props {
  rootId: number;
  /** Undefined at the root of the flow, where there is nothing to leave. */
  parentFlowStepId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepBreakFormFieldsComponent({
  rootId,
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { getValues, setValue } = useFormContext();

  const { data: targets, isFetched } = useQuery({
    queryKey: ["lookup", "break", rootId, parentFlowStepId],
    queryFn: () =>
      backendApiService.Lookup.break({
        flowId: rootId,
        flowStepId: parentFlowStepId,
      }).then((res) =>
        res.data.map((item): TargetOption => {
          const type = item.description as FlowStepTypeEnum;

          return {
            label: item.label,
            value: Number(item.value),
            type,
            description: getFlowStepCatalogEntry(type)?.label ?? item.description,
          };
        }),
      ),
    enabled: rootId > 0,
  });

  // A Break is nearly always the way out of a loop, so one with nothing picked starts on the nearest.
  useEffect(() => {
    if (isDisabled || !targets || getValues("flowStepReferenceId")) return;

    const loop = targets.find((x) => x.type === FlowStepTypeEnum.LOOP);
    if (loop) setValue("flowStepReferenceId", loop.value, { shouldValidate: true, shouldDirty: true });
  }, [targets, isDisabled, getValues, setValue]);

  return (
    <div className="flex flex-column gap-2 mt-4">
      {isFetched && targets?.length === 0 ? (
        <Message
          severity="info"
          className="justify-content-start"
          text="A Break leaves a loop or a check it sits inside. This one is at the top of the flow, so there is nothing to leave: move it inside a loop or a check."
        />
      ) : (
        <FormDropdownComponent<FlowStepDto, TargetOption>
          fieldName="flowStepReferenceId"
          labelText="Break out of"
          mode="local"
          options={targets ?? []}
          optionLabel="label"
          optionValue="value"
          placeholderText="Select the step..."
          isRequired={true}
          isDisabled={isDisabled}
          hintText="The steps it sits inside, nearest first. Out of a loop, no more passes; out of an Every match search, no more hits; out of a check, the rest of its branch is skipped. The execution carries on after the step it leaves."
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
