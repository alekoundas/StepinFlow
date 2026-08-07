import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useCapturePoint } from "@/features/flow-location/hooks/use-capture-point";
import { Button } from "primereact/button";
import { useFormContext, useWatch } from "react-hook-form";

interface Props {
  isDisabled?: boolean;
}

export default function FlowLocationFormFieldsComponent({
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  const locationX = useWatch({ control, name: "locationX" });
  const locationY = useWatch({ control, name: "locationY" });

  const { capturePoint, cancelCapture, isCapturing } = useCapturePoint();

  // Arms capture and waits for the next click anywhere on screen. No window is opened, so the
  // point can be picked inside a live application rather than off a frozen screenshot.
  const handleCapture = async () => {
    const point = await capturePoint();
    if (!point) return;

    setValue("locationX", point.x, {
      shouldValidate: true,
      shouldDirty: true,
    });
    setValue("locationY", point.y, {
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

      <div className="flex gap-3 mt-5">
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
            tooltip="Move the real cursor here so you can confirm the point"
            tooltipOptions={{ position: "top" }}
          />
        </div>

        <div className="w-10">
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
        </div>
      </div>
    </>
  );
}
