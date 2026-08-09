import { Button } from "primereact/button";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useCapturePoint } from "@/features/flow-location/hooks/use-capture-point";
import type { FlowLocationDto } from "@/shared/models/database/flow-location-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

interface EnumOption {
  label: string;
  value: string;
}

interface Props {
  // Frames this point can be measured from.
  areaOptions: FlowSearchAreaDto[];
  isDisabled?: boolean;
}

const toOptions = (values: Record<string, string>): EnumOption[] =>
  Object.values(values).map((value) => ({
    label: value.replaceAll("_", " ").toLowerCase(),
    value,
  }));

export default function FlowLocationFormFieldsComponent({
  areaOptions,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  const flowSearchAreaId = useWatch({ control, name: "flowSearchAreaId" });
  const offsetMode = useWatch({ control, name: "offsetMode" });
  const locationX = useWatch({ control, name: "locationX" });
  const locationY = useWatch({ control, name: "locationY" });

  const { capturePoint, cancelCapture, isCapturing } = useCapturePoint();

  const areaDropdownOptions = [
    { label: "The whole screen (needs rebinding elsewhere)", value: 0 },
    ...areaOptions.map((x) => ({ label: x.name, value: x.id })),
  ];

  // Clicking anywhere gives an absolute point. With a frame chosen it is stored as an offset
  // inside it, so the user just clicks the thing and never sees a screen coordinate.
  const handleCapture = async () => {
    const point = await capturePoint();
    if (!point) return;

    let originX = 0;
    let originY = 0;

    if (flowSearchAreaId) {
      const preview =
        await backendApiService.FlowSearchArea.getPreview(flowSearchAreaId);
      if (preview.isResolved) {
        originX = preview.locationX;
        originY = preview.locationY;
      }
    }

    setValue("locationX", point.x - originX, {
      shouldValidate: true,
      shouldDirty: true,
    });
    setValue("locationY", point.y - originY, {
      shouldValidate: true,
      shouldDirty: true,
    });
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

      <FormDropdownComponent<FlowLocationDto, { label: string; value: number }>
        fieldName="flowSearchAreaId"
        labelText="Measured from"
        mode="local"
        options={areaDropdownOptions}
        optionLabel="label"
        optionValue="value"
        isDisabled={isDisabled}
        hintText="Anchor it to a window and the click survives being run elsewhere."
      />

      {flowSearchAreaId ? (
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
            disabled={isCapturing || !!flowSearchAreaId}
            onClick={handleTest}
            className="p-button-outlined"
            tooltip={
              flowSearchAreaId
                ? "Save first, then test from the grid so the frame can be resolved"
                : "Move the real cursor here so you can confirm the point"
            }
            tooltipOptions={{ position: "top" }}
          />
        </div>

        <div className="w-10">
          {offsetMode === AreaSizingModeEnum.RATIO && flowSearchAreaId ? (
            <div className="flex gap-3">
              <FormInputNumberComponent
                fieldName="ratioX"
                label="X %"
                isDisabled={isDisabled}
              />
              <FormInputNumberComponent
                fieldName="ratioY"
                label="Y %"
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
