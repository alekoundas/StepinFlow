import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useWindowOverlay } from "@/windows/overlay/hooks/use-window-overlay";
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

  const { openWindow, isWindowOpen } = useWindowOverlay();

  // The overlay hands back a rectangle, so the point is its centre. Dragging a small box is
  // easier to aim than trying to land a single pixel.
  const handleCapture = async () => {
    const rect = await openWindow();
    if (!rect) return;

    setValue("locationX", Math.round(rect.x + rect.width / 2), {
      shouldValidate: true,
      shouldDirty: true,
    });
    setValue("locationY", Math.round(rect.y + rect.height / 2), {
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
            label={isWindowOpen ? "Selecting..." : "Capture Location"}
            icon="pi pi-crop"
            loading={isWindowOpen}
            disabled={isWindowOpen || isDisabled}
            onClick={handleCapture}
            className="p-button-outlined p-button-secondary"
            tooltip="Drag a small box on screen, the centre becomes the location"
            tooltipOptions={{ position: "top" }}
          />

          <Button
            type="button"
            label="Test"
            icon="pi pi-play"
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
