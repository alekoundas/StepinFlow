import { useEffect, useState } from "react";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputFloatComponent } from "@/shared/components/form/FormInputFloatComponent";
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
  // Regions already living inside this one, so the form can say what an edit costs them.
  childAreas?: FlowSearchAreaDto[];
  isDisabled?: boolean;
}

const toOptions = (values: Record<string, string>): EnumOption[] =>
  Object.values(values).map((value) => ({
    label: value.replaceAll("_", " ").toLowerCase(),
    value,
  }));

// Left unclamped on purpose: a drag that leaves the frame shows up as an out of range percent
// rather than being silently snapped to an edge.
const toRatio = (size: number, frameSize: number): number =>
  frameSize > 0 ? Math.round((size / frameSize) * 10000) / 10000 : 0;

export default function FlowSearchAreaFormFieldsComponent({
  parentOptions,
  childAreas = [],
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();
  const [captureError, setCaptureError] = useState<string | null>(null);

  const type = useWatch({ control, name: "type" });
  const sizingMode = useWatch({ control, name: "sizingMode" });
  const parentId = useWatch({ control, name: "parentFlowSearchAreaId" });

  const { openWindow, isWindowOpen } = useWindowOverlay();

  const typeOptions = [
    { label: "Region", value: FlowSearchAreaTypeEnum.CUSTOM },
    {
      label: "Application",
      value: FlowSearchAreaTypeEnum.APPLICATION,
        disabled: !!parentId,
    },
    {
      label: "Browser tab",
      value: FlowSearchAreaTypeEnum.BROWSER_TAB,
        disabled: !!parentId,
    },
    {
      label: "Monitor",
      value: FlowSearchAreaTypeEnum.MONITOR,
        disabled: !!parentId,
    },
  ];

  const parentDropdownOptions = parentOptions.map((x) => ({
    label: x.name,
    value: x.id,
  }));

  const parentName =
    parentOptions.find((x) => x.id === parentId)?.name ?? "the parent frame";

  const pixelChildren = childAreas.filter(
    (x) => x.sizingMode === AreaSizingModeEnum.ABSOLUTE_PX,
  );

  useEffect(() => {
    if (!parentId && sizingMode === AreaSizingModeEnum.RATIO) {
      setValue("sizingMode", AreaSizingModeEnum.ABSOLUTE_PX, {
        shouldValidate: true,
        shouldDirty: true,
      });
    }
  }, [parentId, sizingMode, setValue]);

  // The capture window hands back an absolute rect. With a frame chosen it is stored as an
  // offset inside that frame, so the user drags a box and never sees a coordinate.
  const handleCapture = async () => {
    const write = (field: string, value: number) =>
      setValue(field, value, { shouldValidate: true, shouldDirty: true });

    // No parent: the rect is absolute screen coordinates and the whole desktop is fair game.
    if (!parentId) {
      setCaptureError(null);
      const rect = await openWindow();
      if (!rect) return;

      write("locationX", rect.x);
      write("locationY", rect.y);
      write("width", rect.width);
      write("height", rect.height);
      return;
    }

    // A parent added in this same editing session only has a temporary negative id, so there is
    // nothing for the backend to resolve yet.
    if (parentId < 0) {
      setCaptureError(
        `Save the flow before capturing inside "${parentName}", which has not been created yet.`,
      );
      return;
    }

    // Resolved before the overlay opens, not after: the overlay needs these bounds to confine
    // the drag, and a frame that cannot be found is a reason not to open it at all. Writing the
    // raw screen rect as an offset would look like it worked and put the region elsewhere.
    const preview = await backendApiService.FlowSearchArea.getPreview(parentId);
    if (!preview.isResolved) {
      setCaptureError(
        preview.errorMessage ??
          `"${parentName}" is not on screen right now, so the region cannot be measured inside it.`,
      );
      return;
    }
    setCaptureError(null);

    // A child can never legally leave its parent, so the selection is clamped to it rather than
    // cropped away later by the resolver.
    const rect = await openWindow(
      {
        x: preview.locationX,
        y: preview.locationY,
        width: preview.width,
        height: preview.height,
      },
      parentName,
    );
    if (!rect) return;

    // Percent shows different fields, so writing pixels here is a silent no-op.
    if (sizingMode === AreaSizingModeEnum.RATIO) {
      write("ratioX", toRatio(rect.x - preview.locationX, preview.width));
      write("ratioY", toRatio(rect.y - preview.locationY, preview.height));
      write("ratioWidth", toRatio(rect.width, preview.width));
      write("ratioHeight", toRatio(rect.height, preview.height));
      return;
    }

    write("locationX", rect.x - preview.locationX);
    write("locationY", rect.y - preview.locationY);
    write("width", rect.width);
    write("height", rect.height);
  };

  return (
    <>
      {/*========   What an edit here costs the regions inside   ========*/}
      {pixelChildren.length > 0 && !isDisabled && (
        <Message
          severity="warn"
          className="w-full justify-content-start mt-4"
          content={
            <div className="text-sm">
              <div className="font-bold">
                {pixelChildren.length} region
                {pixelChildren.length === 1 ? "" : "s"} inside this area{" "}
                {pixelChildren.length === 1 ? "is" : "are"} measured in pixels:{" "}
                {pixelChildren.map((x) => x.name || "(unnamed)").join(", ")}
              </div>
              <div className="mt-1">
                Their position is fixed against this area's own size. Any that
                no longer fit after your changes are detached on save: they keep
                where they are on screen, but stop being inside this area.
              </div>
            </div>
          }
        />
      )}

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
        hintText={
          !!parentId
            ? "Only a Region can sit inside another area. Clear Inside to change the type."
            : undefined
        }
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
            placeholderText="Nothing — positioned on screen"
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

            <LabelComponent
              text={captureError ?? ""}
              hidden={captureError === null}
              size="sm"
              color="error"
              className="align-self-center mb-3"
            />
          </div>

          {sizingMode === AreaSizingModeEnum.RATIO ? (
            <div className="flex gap-3">
              {/* No min/max: InputNumber clamps out of range values back into the form, which
                  would quietly resize a drag that left the frame. Let the schema say it. */}
              <FormInputFloatComponent fieldName="ratioX" label="X" isPercent={true} isDisabled={isDisabled} />
              <FormInputFloatComponent fieldName="ratioY" label="Y" isPercent={true} isDisabled={isDisabled} />
              <FormInputFloatComponent fieldName="ratioWidth" label="Width" isPercent={true} isDisabled={isDisabled} />
              <FormInputFloatComponent fieldName="ratioHeight" label="Height" isPercent={true} isDisabled={isDisabled} />
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
                isRequired={true}
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
                isRequired={true}
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
                isRequired={true}
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
