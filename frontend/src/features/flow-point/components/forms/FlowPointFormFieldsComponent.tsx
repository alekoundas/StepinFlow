import { useState } from "react";
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

interface Props {
  // Frames this point can be measured from.
  areaOptions: FlowAreaDto[];
  isDisabled?: boolean;
}

// Left unclamped on purpose: a click outside the frame shows up as an out of range percent and
// the schema says so, which beats silently snapping the point to an edge.
const toRatio = (offset: number, size: number): number =>
  size > 0 ? Math.round((offset / size) * 10000) / 10000 : 0;

export default function FlowPointFormFieldsComponent({
  areaOptions,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();
  const [captureError, setCaptureError] = useState<string | null>(null);

  const flowAreaId = useWatch({ control, name: "flowAreaId" });
  const offsetMode = useWatch({ control, name: "offsetMode" });
  const locationX = useWatch({ control, name: "locationX" });
  const locationY = useWatch({ control, name: "locationY" });

  const { capturePoint, cancelCapture, isCapturing } = useCapturePoint();

  const areaDropdownOptions = areaOptions.map((x) => ({
    label: x.name,
    value: x.id,
  }));

  // Clicking anywhere gives an absolute point. With a frame chosen it is stored as an offset
  // inside it, so the user just clicks the thing and never sees a screen coordinate.
  const handleCapture = async () => {
    const point = await capturePoint();
    if (!point) return;

    const write = (field: string, value: number) =>
      setValue(field, value, { shouldValidate: true, shouldDirty: true });

    // No frame: the point is an absolute screen coordinate.
    if (!flowAreaId) {
      setCaptureError(null);
      write("locationX", point.x);
      write("locationY", point.y);
      return;
    }

    const preview =
      await backendApiService.FlowArea.getPreview(flowAreaId);

    // Writing the raw screen point as if it were an offset would look like it worked and place
    // the click somewhere else entirely, so refuse instead.
    if (!preview.isResolved) {
      setCaptureError(
        preview.errorMessage ??
          "That frame is not on screen right now, so the point cannot be measured from it.",
      );
      return;
    }
    setCaptureError(null);

    const offsetX = point.x - preview.locationX;
    const offsetY = point.y - preview.locationY;

    // Percent shows different fields, so writing pixels here is a silent no-op.
    if (offsetMode === AreaSizingModeEnum.RATIO) {
      write("ratioX", toRatio(offsetX, preview.width));
      write("ratioY", toRatio(offsetY, preview.height));
      return;
    }

    write("locationX", offsetX);
    write("locationY", offsetY);
  };

  const handleTest = async () => {
    await backendApiService.System.moveCursor({ x: locationX, y: locationY });
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
              isDisabled={isDisabled}
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
            disabled={isCapturing || !!flowAreaId}
            onClick={handleTest}
            className="p-button-outlined"
            tooltip={
              flowAreaId
                ? "Save first, then test from the grid so the frame can be resolved"
                : "Move the real cursor here so you can confirm the point"
            }
            tooltipOptions={{ position: "top" }}
          />

          <LabelComponent
            text={captureError ?? ""}
            hidden={captureError === null}
            size="sm"
            color="error"
          />
        </div>

        <div className="w-10">
          {offsetMode === AreaSizingModeEnum.RATIO && flowAreaId ? (
            <div className="flex gap-3">
              {/* No min/max: InputNumber clamps out of range values back into the form, which
                  would quietly move a stray capture to the frame edge. Let the schema say it. */}
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
