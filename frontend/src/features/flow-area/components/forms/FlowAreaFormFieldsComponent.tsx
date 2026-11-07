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
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { useWindowOverlay } from "@/windows/overlay/hooks/use-window-overlay";
import WindowMatchFieldsComponent from "@/shared/components/form/WindowMatchFieldsComponent";
import FlowAreaScalesWithFieldComponent from "@/features/flow-area/components/forms/FlowAreaScalesWithFieldComponent";

interface Props {
  // Areas this one may sit inside. Only top level ones, and never itself.
  parentOptions: FlowAreaDto[];
  // Regions already living inside this one, so the form can say what an edit costs them.
  childAreas?: FlowAreaDto[];
  isDisabled?: boolean;
}

// Left unclamped on purpose: a drag that leaves the area shows up as an out of range percent
// rather than being silently snapped to an edge.
const toRatio = (size: number, frameSize: number): number =>
  frameSize > 0 ? Math.round((size / frameSize) * 10000) / 10000 : 0;

export default function FlowAreaFormFieldsComponent({
  parentOptions,
  childAreas = [],
  isDisabled = false,
}: Props) {
  const { control, setValue, getValues } = useFormContext();
  const [captureError, setCaptureError] = useState<string | null>(null);

  const type = useWatch({ control, name: "type" });
  const sizingMode = useWatch({ control, name: "sizingMode" });
  const parentId = useWatch({ control, name: "parentFlowAreaId" });
  const isMain = useWatch({ control, name: "isMain" });
  const titlePattern = useWatch({ control, name: "titlePattern" });

  // The test reports bounds the way the area will actually resolve them, so the numbers on screen
  // are the numbers the flow gets rather than a different measurement of the same window.
  const ignoreWindowBorders = useWatch({ control, name: "ignoreWindowBorders" });

  const { openWindow, isWindowOpen } = useWindowOverlay();

  /**
   * Picking from a dropdown names the area, so a list of them is readable without opening each
   * one. Only while the name is still one this form wrote - "Window - Chrome", "Monitor - ..." or
   * empty - so changing the dropdown never overwrites something typed on purpose.
   */
  const nameFromPick = (prefix: string, label: string) => {
    const current = (getValues("name") as string) ?? "";

    const isUntouched =
      current.length === 0 ||
      current.startsWith("Window - ") ||
      current.startsWith("Monitor - ");

    if (isUntouched)
      setValue("name", `${prefix} - ${label}`, {
        shouldValidate: true,
        shouldDirty: true,
      });
  };

  // Picking an app says what starts it, unless something is already there for the same app.
  const startedByFromPick = async (processId: number, isAnotherApp: boolean) => {
    if (type !== FlowAreaTypeEnum.APPLICATION && type !== FlowAreaTypeEnum.BROWSER_TAB) return;

    const current = (getValues("launchCommand") as string) ?? "";
    if (current.length > 0 && !isAnotherApp) return;

    const command = await backendApiService.Lookup.launchCommand(processId);
    setValue("launchCommand", command, { shouldValidate: true, shouldDirty: true });
  };

  const typeOptions = [
    { label: "Region", value: FlowAreaTypeEnum.CUSTOM },
    {
      label: "Application",
      value: FlowAreaTypeEnum.APPLICATION,
      disabled: !!parentId,
    },
    {
      label: "Browser tab",
      value: FlowAreaTypeEnum.BROWSER_TAB,
      disabled: !!parentId,
    },
    {
      label: "Monitor",
      value: FlowAreaTypeEnum.MONITOR,
      disabled: !!parentId,
    },
  ];

  const parentDropdownOptions = parentOptions.map((x) => ({
    label: x.name,
    value: x.id,
  }));

  const parentName =
    parentOptions.find((x) => x.id === parentId)?.name ?? "the parent area";

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

  // A tab names no title: a browser's is its front tab's, so it would only match while that tab was in front.
  useEffect(() => {
    if (type === FlowAreaTypeEnum.BROWSER_TAB && titlePattern) {
      setValue("titlePattern", "", { shouldValidate: true, shouldDirty: true });
    }
  }, [type, titlePattern, setValue]);

  useEffect(() => {
    if (parentId && isMain) {
      setValue("isMain", false, { shouldValidate: true, shouldDirty: true });
    }
  }, [parentId, isMain, setValue]);

  let mainHint =
    "The window the flow works in. A recording is stored relative to it.";
  if (type === FlowAreaTypeEnum.CUSTOM)
    mainHint =
      "A region on screen is only where it is on this machine, so a flow working in it does not carry over to another.";

  // The capture window hands back an absolute rect. With a parent chosen it is stored as an
  // offset inside it, so the user drags a box and never sees a coordinate.
  const handleCapture = async () => {
    const write = (field: string, value: number) =>
      setValue(field, value, { shouldValidate: true, shouldDirty: true });

    // No parent: the rect is absolute screen coordinates and the whole desktop is fair game.
    if (!parentId) {
      setCaptureError(null);
      const rect = (await openWindow())?.rect;
      if (!rect) return;

      write("locationX", rect.x);
      write("locationY", rect.y);
      write("width", rect.width);
      write("height", rect.height);
      return;
    }

    // Resolved before the overlay opens, not after: the overlay needs these bounds to confine
    // the drag, and a parent that cannot be found is a reason not to open it at all. Writing the
    // raw screen rect as an offset would look like it worked and put the region elsewhere.
    const preview = await backendApiService.FlowArea.getPreview(parentId);
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
    const rect = (
      await openWindow(
        {
          x: preview.locationX,
          y: preview.locationY,
          width: preview.width,
          height: preview.height,
        },
        parentName,
      )
    )?.rect;
    if (!rect) return;

    // Percent shows different fields, so writing pixels here is a silent no-op.
    if (sizingMode === AreaSizingModeEnum.RATIO) {
      write("ratioX", toRatio(rect.x - preview.locationX, preview.width));
      write("ratioY", toRatio(rect.y - preview.locationY, preview.height));
      write("ratioWidth", toRatio(rect.width, preview.width));
      write("ratioHeight", toRatio(rect.height, preview.height));
      return;
    }

    // Pixels mean something only at the DPI they were measured at, so that goes with them.
    write("locationX", rect.x - preview.locationX);
    write("locationY", rect.y - preview.locationY);
    write("width", rect.width);
    write("height", rect.height);
    write("authoredDpi", preview.dpi);
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
          parentId
            ? "Only a Region can sit inside another area. Clear Inside to change the type."
            : undefined
        }
      />

      {!parentId && (
        <FormInputCheckboxComponent
          fieldName="isMain"
          label="Main area"
          isDisabled={isDisabled}
          hintText={mainHint}
        />
      )}

      {/* CUSTOM */}
      {type === FlowAreaTypeEnum.CUSTOM && (
        <>
          {/* Areas go one level deep, so one with regions inside it stays on top. */}
          {childAreas.length === 0 && (
            <FormDropdownComponent<FlowAreaDto, { label: string; value: number }>
              fieldName="parentFlowAreaId"
              labelText="Inside"
              mode="local"
              options={parentDropdownOptions}
              optionLabel="label"
              optionValue="value"
              placeholderText="Nothing — positioned on screen"
              isDisabled={isDisabled}
              hintText="Put it inside a window and the flow keeps working on another machine."
            />
          )}

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
                ? "Percent needs a parent area to be a percentage of."
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
                  would quietly resize a drag that left the area. Let the schema say it. */}
              <FormInputFloatComponent
                fieldName="ratioX"
                label="X"
                isPercent={true}
                isDisabled={isDisabled}
              />
              <FormInputFloatComponent
                fieldName="ratioY"
                label="Y"
                isPercent={true}
                isDisabled={isDisabled}
              />
              <FormInputFloatComponent
                fieldName="ratioWidth"
                label="Width"
                isPercent={true}
                isDisabled={isDisabled}
              />
              <FormInputFloatComponent
                fieldName="ratioHeight"
                label="Height"
                isPercent={true}
                isDisabled={isDisabled}
              />
            </div>
          ) : (
            <div className="flex gap-3">
              <FormInputNumberComponent
                fieldName="locationX"
                label="X"
                isDisabled={isDisabled}
              />
              <FormInputNumberComponent
                fieldName="locationY"
                label="Y"
                isDisabled={isDisabled}
              />
              <FormInputNumberComponent
                fieldName="width"
                label="Width"
                isDisabled={isDisabled}
              />
              <FormInputNumberComponent
                fieldName="height"
                label="Height"
                isDisabled={isDisabled}
              />
            </div>
          )}
        </>
      )}

      {/* APPLICATION */}
      {type === FlowAreaTypeEnum.APPLICATION && (
        <>
          <WindowMatchFieldsComponent
            isDisabled={isDisabled}
            ignoreWindowBorders={ignoreWindowBorders === true}
            onAppPicked={(app, processId, isAnotherApp) => {
              nameFromPick("Window", app);
              void startedByFromPick(processId, isAnotherApp);
            }}
          />

          <div className="mt-3">
            <FormInputCheckboxComponent
              fieldName="ignoreWindowBorders"
              label="Ignore title bar and borders"
              isDisabled={isDisabled}
              hintText="Points stay put when the title bar or borders change size."
            />
          </div>

          <FormInputTextComponent
            fieldName="launchCommand"
            label="Started by"
            placeholderText='"C:\Program Files\App\app.exe"'
            isDisabled={isDisabled}
            className="mt-3"
            hintText="What a Launch step starts when the app is not open. Picking an open app fills it in, and a Steam game is started through Steam."
          />
        </>
      )}

      {/* BROWSER_TAB: its browser and its address. A browser's title is its front tab's. */}
      {type === FlowAreaTypeEnum.BROWSER_TAB && (
        <>
          <WindowMatchFieldsComponent
            isDisabled={isDisabled}
            isBrowser={true}
            onAppPicked={(app, processId, isAnotherApp) => {
              nameFromPick("Window", app);
              void startedByFromPick(processId, isAnotherApp);
            }}
          />

          <FormInputTextComponent
            fieldName="tabAddress"
            label="Address"
            placeholderText="shop.example.com/cart"
            isRequired={true}
            isDisabled={isDisabled}
            hintText="The tab whose address starts with this, with or without https:// and www. Launch opens the browser here when no tab has it."
          />

          <FormInputTextComponent
            fieldName="launchCommand"
            label="Started by"
            placeholderText='"C:\Program Files\Google\Chrome\Application\chrome.exe"'
            isDisabled={isDisabled}
            hintText="The browser a Launch step starts when no tab has the address. Picking an open browser fills it in."
          />

          <LabelComponent
            size="sm"
            color="secondary"
            text="A step that searches here looks at the tab in front of each window of the browser. Focus Window and Launch bring it to the front when it is behind."
          />
        </>
      )}

      {/* MONITOR */}
      {type === FlowAreaTypeEnum.MONITOR && (
        <FormDropdownComponent<FlowAreaDto, LookupItemDto>
          fieldName="monitorDeviceName"
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
          onChanged={(_value, option) => {
            if (option) nameFromPick("Monitor", option.label);
          }}
        />
      )}

      <FlowAreaScalesWithFieldComponent
        type={type}
        parentName={parentId ? parentName : null}
        isDisabled={isDisabled}
      />
    </>
  );
}
