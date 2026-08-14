import { useFormContext } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";

import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { useFlowAreaMutations } from "@/features/flow-area/hooks/use-flow-area";
import FlowAreaFormComponent from "@/features/flow-area/components/forms/FlowAreaFormComponent";

const FORM_ID = "search-area-form";

interface IdOption {
  label: string;
  value: number;
}

interface Props {
  flowId: number | undefined;
  labelText: string;
  hintText?: string;
  isDisabled?: boolean;
}

/**
 * Picks the area a step works in, with a way to make one without leaving the step. Shared so
 * every step that searches the screen offers the same thing.
 */
export default function FlowStepSearchAreaFieldComponent({
  flowId,
  labelText,
  hintText,
  isDisabled = false,
}: Props) {
  const { setValue } = useFormContext();
  const { openForm, closeAll } = useDialogStore();
  const { createFlowAreaMutation } = useFlowAreaMutations();

  const loadAreas = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.flowArea({ searchText: filter, flowId }).then(
      (res) =>
        res.data.map((item) => ({
          label: item.label,
          value: Number(item.value),
        })),
    );

  // Areas a new region could sit inside. Only id and name are used by the picker.
  const { data: parentOptions = [] } = useQuery({
    queryKey: ["lookup", "flowArea", "parents", flowId],
    queryFn: () =>
      backendApiService.Lookup.flowArea({ flowId }).then((res) =>
        res.data.map(
          (item) =>
            new FlowAreaDto({
              id: Number(item.value),
              name: item.label,
              flowId: flowId ?? 0,
            }),
        ),
      ),
    enabled: !!flowId,
  });

  // Saved straight away so the dropdown has a real id to bind to, same as the cursor step does
  // for locations.
  const openAddArea = () => {
    openForm(FORM_ID, {
      headerText: "Add Area",
      formId: FORM_ID,
      children: (
        <FlowAreaFormComponent
          defaultValues={new FlowAreaDto({ flowId: flowId ?? 0 })}
          formId={FORM_ID}
          isFormInDialog={true}
          formMode="ADD"
          parentOptions={parentOptions}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();
            const newId = await createFlowAreaMutation.mutateAsync({
              ...data,
              id: 0,
              flowId: flowId ?? 0,
            });
            setValue("flowAreaId", newId, {
              shouldValidate: true,
              shouldDirty: true,
            });
          }}
        />
      ),
    });
  };

  return (
    <div className="flex gap-3 align-items-end">
      <div className="flex-1">
        <FormDropdownComponent<FlowStepDto, IdOption>
          fieldName="flowAreaId"
          labelText={labelText}
          mode="remote"
          queryKey={["lookup", "flowArea", flowId]}
          queryFn={loadAreas}
          optionLabel="label"
          optionValue="value"
          placeholderText="Select an area..."
          isRequired={true}
          isDisabled={isDisabled}
          hintText={hintText}
        />
      </div>

      <Button
        type="button"
        icon="pi pi-plus"
        label="New"
        onClick={openAddArea}
        disabled={isDisabled || !flowId}
        className="p-button-outlined mb-3"
        tooltip="Create a search area and use it here"
        tooltipOptions={{ position: "top" }}
      />
    </div>
  );
}
