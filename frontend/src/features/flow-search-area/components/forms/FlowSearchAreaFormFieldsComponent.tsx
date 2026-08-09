import { Button } from "primereact/button";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { LookupItemDto } from "@/shared/models/lazy-data/lookup-item.dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { TitleMatchModeEnum } from "@/shared/enums/backend/area/title-match-mode-enum";
import { BrowserTypeEnum } from "@/shared/enums/backend/area/browser-type-enum";
import { TabMatchOnEnum } from "@/shared/enums/backend/area/tab-match-on-enum";
import { useWindowOverlay } from "@/windows/overlay/hooks/use-window-overlay";

interface EnumOption {
  label: string;
  value: string;
}

interface Props {
  // Areas this one may sit inside. Only frames, and never itself.
  parentOptions: FlowSearchAreaDto[];
  isDisabled?: boolean;
}

const toOptions = (values: Record<string, string>): EnumOption[] =>
  Object.values(values).map((value) => ({
    label: value.replaceAll("_", " ").toLowerCase(),
    value,
  }));

export default function FlowSearchAreaFormFieldsComponent({
  parentOptions,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  const type = useWatch({ control, name: "type" });
  const sizingMode = useWatch({ control, name: "sizingMode" });
  const parentId = useWatch({ control, name: "parentFlowSearchAreaId" });

  const { openWindow, isWindowOpen } = useWindowOverlay();

  const typeOptions = [
    { label: "Region", value: FlowSearchAreaTypeEnum.CUSTOM },
    { label: "Application", value: FlowSearchAreaTypeEnum.APPLICATION },
    { label: "Browser tab", value: FlowSearchAreaTypeEnum.BROWSER_TAB },
    { label: "Monitor", value: FlowSearchAreaTypeEnum.MONITOR },
  ];

  const parentDropdownOptions = [
    { label: "The whole screen (needs rebinding elsewhere)", value: 0 },
    ...parentOptions.map((x) => ({ label: x.name, value: x.id })),
  ];

  // The capture window hands back an absolute rect. With a frame chosen it is stored as an
  // offset inside that frame, so the user drags a box and never sees a coordinate.
  const handleCapture = async () => {
    const rect = await openWindow();
    if (!rect) return;

    let originX = 0;
    let originY = 0;

    if (parentId) {
      const preview = await backendApiService.FlowSearchArea.getPreview(parentId);
      if (preview.isResolved) {
        originX = preview.locationX;
        originY = preview.locationY;
      }
    }

    setValue("locationX", rect.x - originX, { shouldValidate: true, shouldDirty: true });
    setValue("locationY", rect.y - originY, { shouldValidate: true, shouldDirty: true });
    setValue("width", rect.width, { shouldValidate: true, shouldDirty: true });
    setValue("height", rect.height, { shouldValidate: true, shouldDirty: true });
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

      <FormSelectButtonComponent
        fieldName="type"
        labelText="Type"
        options={typeOptions}
        isDisabled={isDisabled}
        isRequired={true}
      />

      {/* CUSTOM */}
      {type === FlowSearchAreaTypeEnum.CUSTOM && (
        <>
          <FormDropdownComponent<FlowSearchAreaDto, { label: string; value: number }>
            fieldName="parentFlowSearchAreaId"
            labelText="Inside"
            mode="local"
            options={parentDropdownOptions}
            optionLabel="label"
            optionValue="value"
            isDisabled={isDisabled}
            hintText="Put it inside a window and the flow keeps working on another machine."
          />

          <FormSelectButtonComponent
            fieldName="sizingMode"
            labelText="Measured in"
            options={[
              { label: "Pixels", value: AreaSizingModeEnum.ABSOLUTE_PX },
              { label: "Percent", value: AreaSizingModeEnum.RATIO },
            ]}
            isDisabled={isDisabled || !parentId}
            hintText={
              !parentId
                ? "Percent needs a frame to be a percentage of."
                : undefined
            }
          />

          <div className="flex gap-3">
            <Button
              type="button"
              label={isWindowOpen ? "Selecting..." : "Capture Area"}
              icon="pi pi-crop"
              loading={isWindowOpen}
              disabled={isWindowOpen || isDisabled}
              onClick={handleCapture}
              className="p-button-outlined p-button-secondary mb-3"
              tooltip="Click and drag to select a region of your screen"
              tooltipOptions={{ position: "top" }}
            />
          </div>

          {sizingMode === AreaSizingModeEnum.RATIO ? (
            <div className="flex gap-3">
              <FormInputNumberComponent fieldName="ratioX" label="X %" isDisabled={isDisabled} />
              <FormInputNumberComponent fieldName="ratioY" label="Y %" isDisabled={isDisabled} />
              <FormInputNumberComponent fieldName="ratioWidth" label="Width %" isDisabled={isDisabled} />
              <FormInputNumberComponent fieldName="ratioHeight" label="Height %" isDisabled={isDisabled} />
            </div>
          ) : (
            <div className="flex gap-3">
              <FormInputNumberComponent fieldName="locationX" label="X" isDisabled={isDisabled} />
              <FormInputNumberComponent fieldName="locationY" label="Y" isDisabled={isDisabled} />
              <FormInputNumberComponent fieldName="width" label="Width" isDisabled={isDisabled} />
              <FormInputNumberComponent fieldName="height" label="Height" isDisabled={isDisabled} />
            </div>
          )}
        </>
      )}

      {/* APPLICATION and BROWSER_TAB share how the window is found */}
      {(type === FlowSearchAreaTypeEnum.APPLICATION ||
        type === FlowSearchAreaTypeEnum.BROWSER_TAB) && (
        <>
          <FormDropdownComponent<FlowSearchAreaDto, LookupItemDto>
            fieldName="processName"
            labelText="Application"
            mode="remote"
            queryKey={["lookup", "window"]}
            queryFn={(filter) =>
              backendApiService.Lookup.window({ searchText: filter }).then(
                (res) => res.data,
              )
            }
            optionLabel="label"
            optionValue="value"
            placeholderText="Search open windows..."
            isDisabled={isDisabled}
            defaultValue={""}
            hintText="Matched on the process name, which does not change while the app runs."
          />

          <FormInputTextComponent
            fieldName="titlePattern"
            label="Window title"
            isDisabled={isDisabled}
          />

          <div className="flex gap-3">
            <div className="flex-1">
              <FormDropdownComponent<FlowSearchAreaDto, EnumOption>
                fieldName="titleMatchMode"
                labelText="Title match"
                mode="local"
                options={toOptions(TitleMatchModeEnum)}
                optionLabel="label"
                optionValue="value"
                isDisabled={isDisabled}
              />
            </div>
            <div className="flex-1">
              <FormInputNumberComponent
                fieldName="instanceIndex"
                label="If several match, use #"
                min={0}
                isDisabled={isDisabled}
              />
            </div>
          </div>

          <FormInputCheckboxComponent
            fieldName="useClientArea"
            label="Ignore title bar and borders"
            isDisabled={isDisabled}
            hintText="Keeps offsets correct whatever chrome the window has."
          />
        </>
      )}

      {/* BROWSER_TAB */}
      {type === FlowSearchAreaTypeEnum.BROWSER_TAB && (
        <>
          <div className="flex gap-3">
            <div className="flex-1">
              <FormDropdownComponent<FlowSearchAreaDto, EnumOption>
                fieldName="browserType"
                labelText="Browser"
                mode="local"
                options={toOptions(BrowserTypeEnum)}
                optionLabel="label"
                optionValue="value"
                isDisabled={isDisabled}
              />
            </div>
            <div className="flex-1">
              <FormDropdownComponent<FlowSearchAreaDto, EnumOption>
                fieldName="tabMatchOn"
                labelText="Find tab by"
                mode="local"
                options={toOptions(TabMatchOnEnum)}
                optionLabel="label"
                optionValue="value"
                isDisabled={isDisabled}
              />
            </div>
          </div>

          <FormInputTextComponent
            fieldName="tabMatchValue"
            label="Tab title or URL contains"
            isRequired={true}
            isDisabled={isDisabled}
          />

          <LabelComponent
            size="sm"
            color="warning"
            text="Browser tabs are not resolved yet. The area saves, but it will not run until tab support lands."
          />
        </>
      )}

      {/* MONITOR */}
      {type === FlowSearchAreaTypeEnum.MONITOR && (
        <FormDropdownComponent<FlowSearchAreaDto, LookupItemDto>
          fieldName="monitorUniqueId"
          labelText="Monitor"
          mode="remote"
          queryKey={["lookup", "monitor"]}
          queryFn={() =>
            backendApiService.Lookup.monitor({}).then((res) => res.data)
          }
          optionLabel="label"
          optionValue="value"
          placeholderText="Select a monitor..."
          isRequired={true}
          isDisabled={isDisabled}
          defaultValue={""}
        />
      )}
    </>
  );
}
