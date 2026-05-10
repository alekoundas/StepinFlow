import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import type { LookupItemDto } from "@/shared/models/lazy-data/lookup-item.dto";
import { backendApiService } from "@/shared/services/backend-api-service";
import { SelectButton } from "primereact/selectbutton";
import { Controller, useFormContext, useWatch } from "react-hook-form";
import type { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { Button } from "primereact/button";
import { useWindowOverlay } from "@/windows/overlay/hooks/use-window-overlay";
import LabelComponent from "@/shared/components/LabelComponent";

interface Props {
  isDisabled?: boolean;
}

export default function FlowSearchAreaFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  // Watch the two fields that control each other
  const selectedType = useWatch({ control, name: "type" });
  // const cursorOptions = Object.values(CursorActionTypeEnum).map((value) => ({
  //   label: value.replace("_", " ").toLowerCase(),
  //   value,
  // }));

  const typeOptions = [
    { label: "Custom", value: "CUSTOM" as FlowSearchAreaTypeEnum },
    {
      label: "By Application/Window",
      value: "APPLICATION" as FlowSearchAreaTypeEnum,
    },
    { label: "By Monitor", value: "MONITOR" as FlowSearchAreaTypeEnum },
  ];

  const { openWindow, isWindowOpen } = useWindowOverlay();

  const handleClick = async () => {
    const rect = await openWindow();
    if (rect) {
      setValue("locationX", rect.x);
      setValue("locationY", rect.y);
      setValue("width", rect.width);
      setValue("height", rect.height);
      // onSelect(rect);
    }
  };

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
        className="mt-5"
      />

      {/* Type */}
      <div className="mt-5">
        <Controller
          name="type"
          control={control}
          render={({ field }) => (
            <div className="flex justify-content-center ">
              <div className=" w-50">
                {/* <LabelComponent
                text="Choose Type"
                wrap={false}
                className="mr-3"
                alignContent="center"
                /> */}
                <SelectButton
                  value={field.value}
                  options={typeOptions}
                  onChange={(e) => (e.value ? field.onChange(e.value) : null)}
                  className=""
                />
              </div>
            </div>
          )}
        />
      </div>

      {/* Type = CUSTOM */}
      {selectedType === "CUSTOM" && (
        <div className="flex gap-3 mt-5">
          <div className="flex w-10 align-items-center justify-content-center">
            <Button
              label={isWindowOpen ? "Selecting..." : "Capture Area"}
              icon="pi pi-crop"
              loading={isWindowOpen}
              disabled={isWindowOpen}
              onClick={handleClick}
              className="p-button-outlined p-button-secondary"
              tooltip="Click and drag to select a region of your screen"
              tooltipOptions={{ position: "top" }}
            />
          </div>

          <div className="w-10 ">
            <div className="flex gap-3">
              <FormInputNumberComponent
                fieldName="locationX"
                label="X"
                // min={0}
                // max={2147483647}
                isRequired={true}
                isDisabled={isDisabled}
              />

              <FormInputNumberComponent
                fieldName="locationY"
                label="Y"
                // min={0}
                // max={2147483647}
                isRequired={true}
                isDisabled={isDisabled}
              />
            </div>
            <div className="flex gap-3">
              <FormInputNumberComponent
                fieldName="width"
                label="Width"
                // min={0}
                // max={2147483647}
                isRequired={true}
                isDisabled={isDisabled}
              />

              <FormInputNumberComponent
                fieldName="height"
                label="Height"
                // min={0}
                // max={2147483647}
                isRequired={true}
                isDisabled={isDisabled}
              />
            </div>
          </div>
        </div>
      )}

      {/* APPLICATION */}
      {selectedType === "APPLICATION" && (
        <FormDropdownComponent<FlowSearchAreaDto, LookupItemDto>
          fieldName="appWindowName"
          labelText="Select Application/Window"
          mode="remote"
          queryKey={["lookup", "window"]}
          queryFn={(_filter) =>
            backendApiService.Lookup.window({}).then((res) => res.data)
          }
          optionLabel="label"
          optionValue="value"
          placeholderText="Search item..."
          classNameContainer="mt-5"
          isRequired={true}
          defaultValue={""}
        />
      )}

      {/* MONITOR */}
      {selectedType === "MONITOR" && (
        <FormDropdownComponent<FlowSearchAreaDto, LookupItemDto>
          fieldName="monitorUniqueId"
          labelText="Select Monitor"
          mode="remote"
          queryKey={["lookup", "monitor"]}
          queryFn={(_filter) =>
            backendApiService.Lookup.monitor({}).then((res) => res.data)
          }
          optionLabel="label"
          optionValue="value"
          placeholderText="Search item..."
          isRequired={true}
          defaultValue={""}
          classNameContainer="mt-5"
        />
      )}
    </>
  );
}
