import { useEffect, useRef, useState } from "react";
import { Button } from "primereact/button";
import { useFormContext, useWatch } from "react-hook-form";

import LabelComponent from "@/shared/components/LabelComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputFloatComponent } from "@/shared/components/form/FormInputFloatComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useCapturePoint } from "@/features/flow-point/hooks/use-capture-point";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";
import { ScalesWithEnum } from "@/shared/enums/backend/area/scales-with-enum";

interface Props {
  // Areas this point can be measured from.
  areaOptions: FlowAreaDto[];
  isDisabled?: boolean;
}

// Left unclamped on purpose: a click outside the area shows up as an out of range percent and
// the schema says so, which beats silently snapping the point to an edge.
const toRatio = (offset: number, size: number): number =>
  size > 0 ? Math.round((offset / size) * 10000) / 10000 : 0;

// The backend's rule: unset inherits the parent's, and an area with neither scales with DPI.
const scalesWithOf = (
  areaId: number | null | undefined,
  areas: FlowAreaDto[],
): ScalesWithEnum | undefined => {
  const area = areas.find((x) => x.id === areaId);
  if (!area) return undefined;
  if (area.scalesWith) return area.scalesWith;

  const parent = areas.find((x) => x.id === area.parentFlowAreaId);
  return parent?.scalesWith ?? ScalesWithEnum.DPI;
};

export default function FlowPointFormFieldsComponent({
  areaOptions,
  isDisabled = false,
}: Props) {
  const { control, setValue, getValues } = useFormContext();
  const [pointError, setPointError] = useState<string | null>(null);

  const flowAreaId = useWatch({ control, name: "flowAreaId" });
  const offsetMode = useWatch({ control, name: "offsetMode" });

  const { capturePoint, cancelCapture, isCapturing } = useCapturePoint();

  const areaName = areaOptions.find((x) => x.id === flowAreaId)?.name ?? "The area";

  // Pixels inside an area that scales with its size are not scaled at all, so they land somewhere
  // else as soon as the window is another size. Only a percent follows it.
  const isPercentOnly = scalesWithOf(flowAreaId, areaOptions) === ScalesWithEnum.AREA;

  // Picking an area sets what the location is measured in: Percent where only a percent follows
  // it, Pixels where its contents keep their size. Only on a pick, so opening a saved location
  // changes nothing.
  const pickedAreaId = useRef(flowAreaId);
  useEffect(() => {
    if (isDisabled || pickedAreaId.current === flowAreaId) return;
    pickedAreaId.current = flowAreaId;

    if (!flowAreaId) return;

    const mode = isPercentOnly ? AreaSizingModeEnum.RATIO : AreaSizingModeEnum.ABSOLUTE_PX;
    setValue("offsetMode", mode, { shouldValidate: true, shouldDirty: true });
  }, [flowAreaId, isPercentOnly, isDisabled, setValue]);

  let measuredInHint: string | undefined;
  if (isPercentOnly && offsetMode === AreaSizingModeEnum.RATIO)
    measuredInHint = `${areaName} scales with its size, so a location in it is a percent of it.`;
  else if (isPercentOnly)
    measuredInHint = `${areaName} scales with its size, and pixels do not follow it. Switch to Percent and capture the location again.`;

  const areaDropdownOptions = areaOptions.map((x) => ({
    label: x.name,
    value: x.id,
  }));

  // Clicking anywhere gives an absolute point. With an area chosen it is stored as an offset
  // inside it, so the user just clicks the thing and never sees a screen coordinate.
  const handleCapture = async () => {
    const point = await capturePoint();
    if (!point) return;

    const write = (field: string, value: number) =>
      setValue(field, value, { shouldValidate: true, shouldDirty: true });

    // No area: the point is an absolute screen coordinate.
    if (!flowAreaId) {
      setPointError(null);
      write("locationX", point.x);
      write("locationY", point.y);
      return;
    }

    const preview =
      await backendApiService.FlowArea.getPreview(flowAreaId);

    // Writing the raw screen point as if it were an offset would look like it worked and place
    // the click somewhere else entirely, so refuse instead.
    if (!preview.isResolved) {
      setPointError(
        preview.errorMessage ??
          "That area is not on screen right now, so the point cannot be measured from it.",
      );
      return;
    }
    setPointError(null);

    const offsetX = point.x - preview.locationX;
    const offsetY = point.y - preview.locationY;

    // Percent shows different fields, so writing pixels here is a silent no-op.
    if (offsetMode === AreaSizingModeEnum.RATIO) {
      write("ratioX", toRatio(offsetX, preview.width));
      write("ratioY", toRatio(offsetY, preview.height));
      return;
    }

    // Pixels mean something only at the DPI they were measured at, so that goes with them.
    write("locationX", offsetX);
    write("locationY", offsetY);
    write("authoredDpi", preview.dpi);
  };

  // Worked out the way an execution does, from the values on the form and the saved area.
  const handleTest = async () => {
    setPointError(null);

    try {
      const point = await backendApiService.FlowPoint.resolve(getValues() as FlowPointDto);
      await backendApiService.System.moveCursor(point);
    } catch (err) {
      setPointError(err instanceof Error ? err.message : "The location could not be worked out.");
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

      <FormDropdownComponent<FlowPointDto, { label: string; value: number }>
        fieldName="flowAreaId"
        labelText="Measured from"
        mode="local"
        options={areaDropdownOptions}
        optionLabel="label"
        optionValue="value"
        placeholderText="Nothing — a point on screen"
        isDisabled={isDisabled}
        hintText="Anchor it to a window and the click survives being run elsewhere."
      />

      {flowAreaId ? (
        <div className="flex gap-3">
          <div className="flex-1">
            <FormSelectButtonComponent
              fieldName="offsetMode"
              labelText="Measured in"
              options={[
                { label: "Pixels", value: AreaSizingModeEnum.ABSOLUTE_PX },
                { label: "Percent", value: AreaSizingModeEnum.RATIO },
              ]}
              isDisabled={isDisabled || (isPercentOnly && offsetMode === AreaSizingModeEnum.RATIO)}
              hintText={measuredInHint}
            />
          </div>
        </div>
      ) : null}

      <div className="flex gap-3 mt-2">
        <div className="flex flex-column gap-2 w-10 align-items-center justify-content-center">
          <Button
            type="button"
            label={isCapturing ? "Click anywhere..." : "Capture Location"}
            icon={isCapturing ? "pi pi-times" : "pi pi-map-marker"}
            disabled={isDisabled}
            onClick={isCapturing ? cancelCapture : handleCapture}
            className={
              isCapturing
                ? "p-button-outlined p-button-warning"
                : "p-button-outlined p-button-secondary"
            }
            tooltip={
              isCapturing
                ? "Click anywhere on screen to set the point, or press Escape to cancel"
                : "Then click anywhere on screen to set the point"
            }
            tooltipOptions={{ position: "top" }}
          />

          <Button
            type="button"
            label="Test"
            icon="pi pi-play"
            disabled={isCapturing}
            onClick={handleTest}
            className="p-button-outlined"
            tooltip="Move the real cursor to where the flow will put it"
            tooltipOptions={{ position: "top" }}
          />

          <LabelComponent
            text={pointError ?? ""}
            hidden={pointError === null}
            size="sm"
            color="error"
          />
        </div>

        <div className="w-10">
          {offsetMode === AreaSizingModeEnum.RATIO && flowAreaId ? (
            <div className="flex gap-3">
              {/* No min/max: InputNumber clamps out of range values back into the form, which
                  would quietly move a stray capture to the area edge. Let the schema say it. */}
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
            </div>
          ) : (
            <div className="flex gap-3">
              <FormInputNumberComponent
                fieldName="locationX"
                label="X"
                isRequired={true}
                isDisabled={isDisabled}
              />
              <FormInputNumberComponent
                fieldName="locationY"
                label="Y"
                isRequired={true}
                isDisabled={isDisabled}
              />
            </div>
          )}
        </div>
      </div>
    </>
  );
}
