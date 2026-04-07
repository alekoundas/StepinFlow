import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { CursorActionTypeEnum } from "@/shared/enums/backend/cursor-action-type-enum";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { LookupItemDto } from "@/shared/models/lazy-data/lookup-item.dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import { SelectButton } from "primereact/selectbutton";
import { Controller, useFormContext, useWatch } from "react-hook-form";
import type { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { Button } from "primereact/button";

interface Props {
  isDisabled?: boolean;
}

export default function FlowSearchAreaFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue, trigger } = useFormContext();

  // Watch the two fields that control each other
  const selectedType = useWatch({ control, name: "flowSearchAreaType" });
  // const cursorOptions = Object.values(CursorActionTypeEnum).map((value) => ({
  //   label: value.replace("_", " ").toLowerCase(),
  //   value,
  // }));

  const typeOptions = [
    { label: "Custom Rectangle", value: "CUSTOM" as FlowSearchAreaTypeEnum },
    {
      label: "By Application/Window",
      value: "APPLICATION" as FlowSearchAreaTypeEnum,
    },
    { label: "By Monitor", value: "MONITOR" as FlowSearchAreaTypeEnum },
  ];

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />
      {/* 
      <FormDropdownComponent
        fieldName="cursorActionType"
        labelText="Cursor Action Type"
        mode="local"
        options={cursorOptions}
        optionLabel="label"
        optionValue="value"
        isRequired={true}
        isDisabled={isDisabled}
      /> */}

      <Controller
        name="flowSearchAreaType"
        control={control}
        render={({ field }) => (
          <SelectButton
            value={field.value}
            options={typeOptions}
            onChange={(e) => field.onChange(e.value)}
            className="w-full"
          />
        )}
      />

      {/* CUSTOM */}
      {selectedType === "CUSTOM" && (
        <div className="grid grid-cols-2 gap-3">
          <div className="field col-6">
            <FormInputNumberComponent
              fieldName="locationX"
              label="Location X"
              // min={0}
              // max={2147483647}
              isRequired={isDisabled}
              isDisabled={isDisabled}
            />
          </div>

          <div className="field col-6">
            <FormInputNumberComponent
              fieldName="locationY"
              label="Location Y"
              // min={0}
              // max={2147483647}
              isRequired={isDisabled}
              isDisabled={isDisabled}
            />
          </div>

          <div className="field col-6">
            <FormInputNumberComponent
              fieldName="width"
              label="Width"
              // min={0}
              // max={2147483647}
              isRequired={isDisabled}
              isDisabled={isDisabled}
            />
          </div>

          <div className="field col-6">
            <FormInputNumberComponent
              fieldName="height"
              label="Height"
              // min={0}
              // max={2147483647}
              isRequired={isDisabled}
              isDisabled={isDisabled}
            />
          </div>

          <div className="col-12">
            <Button
              type="button"
              label="Capture from Screen"
              icon="pi pi-camera"
              severity="secondary"
              onClick={() => {
                /* Implement overlay capture via IPC */
              }}
            />
          </div>
        </div>
      )}

      {/* APPLICATION */}
      {selectedType === "APPLICATION" && (
        <FormDropdownComponent<FlowSearchAreaDto, LookupItemDto>
          fieldName="applicationName"
          labelText="Select Item"
          mode="remote"
          queryKey={["lookup", "window"]}
          queryFn={(_filter) =>
            backendApiService.Lookup.window({}).then((res) => res.data)
          }
          optionLabel="label"
          optionValue="value"
          placeholderText="Search items..."
        />
      )}

      {/* MONITOR */}
      {selectedType === "MONITOR" && (
        <FormDropdownComponent<FlowSearchAreaDto, LookupItemDto>
          fieldName="monitorIndex"
          labelText="Select Item"
          mode="remote"
          queryKey={["lookup", "monitor"]}
          queryFn={(_filter) =>
            backendApiService.Lookup.monitor({}).then((res) => res.data)
          }
          optionLabel="label"
          optionValue="value"
          placeholderText="Search items..."
        />
      )}
    </>
  );
}
