import type z from "zod";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { SelectButton } from "primereact/selectbutton";
import { Controller, useFormContext, useWatch } from "react-hook-form";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import FlowPointFormComponent from "@/features/flow-point/components/forms/FlowPointFormComponent";
import { useFlowPointMutations } from "@/features/flow-point/hooks/use-flow-point";
import { FlowStepCursorSchema } from "@/features/flow-step/components/forms/cursor/flow-step-cursor.zod";

type CursorForm = z.infer<typeof FlowStepCursorSchema>;

// The dropdowns speak numbers, the lookup endpoint speaks strings.
interface LocationOption {
  label: string;
  value: number;
  description?: string;
  x: number;
  y: number;
}

interface StepOption {
  label: string;
  value: number;
  description?: string;
}

interface Props {
  // Which of the two points this block edits.
  isEndPoint?: boolean;
  title: string;
  // Scope for the two lookups: locations belong to the flow, step results to the parent chain.
  flowId: number | undefined;
  parentFlowStepId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepCursorLocationFieldsComponent({
  isEndPoint = false,
  title,
  flowId,
  parentFlowStepId,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();
  const { openForm, closeAll } = useDialogStore();
  const { createFlowPointMutation } = useFlowPointMutations();

  // A location created from here should still be anchorable to a frame, so the flow's areas
  // are fetched rather than passed down from the flow form.
  const { data: areaOptions = [] } = useQuery({
    queryKey: ["lookup", "flowArea", flowId],
    queryFn: () =>
      backendApiService.Lookup.flowArea({ flowId }).then((res) =>
        res.data.map(
          (item) =>
            new FlowAreaDto({ id: Number(item.value), name: item.label }),
        ),
      ),
    enabled: !!flowId,
  });

  const isCustomFieldName = isEndPoint
    ? "isPointEndCustom"
    : "isPointCustom";
  const locationFieldName = isEndPoint ? "flowPointEndId" : "flowPointId";
  const referenceFieldName = isEndPoint
    ? "flowStepReferenceEndId"
    : "flowStepReferenceId";

  const isCustom = useWatch({ control, name: isCustomFieldName });
  const flowPointId = useWatch({ control, name: locationFieldName });

  const sourceOptions = [
    { label: "Saved Location", value: true },
    { label: "Step Result", value: false },
  ];

  const loadLocations = (filter?: string): Promise<LocationOption[]> =>
    backendApiService.Lookup.flowPoint({
      searchText: filter,
      flowId,
    }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: Number(item.value),
        description: item.description,
        x: item.extraData?.x ?? 0,
        y: item.extraData?.y ?? 0,
      })),
    );

  const loadStepReferences = (filter?: string): Promise<StepOption[]> =>
    backendApiService.Lookup.flowStep({
      searchText: filter,
      flowStepId: parentFlowStepId,
    }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: Number(item.value),
        description: item.description,
      })),
    );

  // Created straight away so the dropdown has a real id to bind to.
  const openAddLocation = () => {
    openForm("flow-point-form", {
      headerText: "Add Location",
      formId: "flow-point-form",
      children: (
        <FlowPointFormComponent
          defaultValues={new FlowPointDto({ flowId: flowId ?? 0 })}
          formId="flow-point-form"
          isFormInDialog={true}
          formMode="ADD"
          areaOptions={areaOptions}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();
            const newId = await createFlowPointMutation.mutateAsync({
              ...data,
              flowId: flowId ?? 0,
            });
            setValue(locationFieldName, newId, {
              shouldValidate: true,
              shouldDirty: true,
            });
          }}
        />
      ),
    });
  };

  const handleTest = async () => {
    const locations = await loadLocations();
    const selected = locations.find((x) => x.value === flowPointId);
    if (!selected) return;

    await backendApiService.System.moveCursor({ x: selected.x, y: selected.y });
  };

  return (
    <div className="mt-5">
      <LabelComponent
        text={title}
        weight="bold"
      />

      <Controller
        name={isCustomFieldName}
        control={control}
        render={({ field }) => (
          <SelectButton
            value={field.value}
            options={sourceOptions}
            disabled={isDisabled}
            onChange={(e) => (e.value !== null ? field.onChange(e.value) : null)}
            className="mt-2"
          />
        )}
      />

      {isCustom ? (
        <div className="flex gap-3 align-items-end mt-3">
          <div className="flex-1">
            <FormDropdownComponent<CursorForm, LocationOption>
              fieldName={locationFieldName}
              labelText="Location"
              mode="remote"
              queryKey={["lookup", "flowPoint", flowId]}
              queryFn={loadLocations}
              optionLabel="label"
              optionValue="value"
              placeholderText="Select a location..."
              isRequired={true}
              isDisabled={isDisabled}
            />
          </div>

          <Button
            type="button"
            icon="pi pi-plus"
            label="New"
            onClick={openAddLocation}
            disabled={isDisabled || !flowId}
            className="p-button-outlined mb-3"
            tooltip="Create a location and use it here"
            tooltipOptions={{ position: "top" }}
          />

          <Button
            type="button"
            icon="pi pi-play"
            label="Test"
            onClick={handleTest}
            disabled={!flowPointId}
            className="p-button-outlined mb-3"
            tooltip="Move the real cursor to the selected location"
            tooltipOptions={{ position: "top" }}
          />
        </div>
      ) : (
        <div className="mt-3">
          <FormDropdownComponent<CursorForm, StepOption>
            fieldName={referenceFieldName}
            labelText="Use the result of"
            mode="remote"
            queryKey={["lookup", "flowStep", parentFlowStepId]}
            queryFn={loadStepReferences}
            optionLabel="label"
            optionValue="value"
            placeholderText="Select a step..."
            hintText="Only Image Search / Text Search steps this step runs under can be used."
            isRequired={true}
            isDisabled={isDisabled}
          />
        </div>
      )}
    </div>
  );
}
