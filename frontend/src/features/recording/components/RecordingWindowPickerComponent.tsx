import type { ReactNode } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { SelectButton } from "primereact/selectbutton";

import LabelComponent from "@/shared/components/LabelComponent";
import WindowMatchFieldsComponent from "@/shared/components/form/WindowMatchFieldsComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import type { RecordingWindowForm } from "@/features/recording/components/recording-setup.zod";

const TYPE_OPTIONS = [
  { label: "Application", value: FlowAreaTypeEnum.APPLICATION },
  { label: "Website", value: FlowAreaTypeEnum.BROWSER_TAB },
];

interface Props {
  // Fields of the same section that go under the window.
  children?: ReactNode;
}

/**
 * The window a recording works in, which becomes the flow's main area: an application's window,
 * or a website's tab in a browser. The fields the area form shows for those two, so the setup
 * makes the area that form would: apps listed one to a row with their windows under them, and a
 * website by its browser and address.
 */
export default function RecordingWindowPickerComponent({ children }: Props) {
  const { control, setValue } = useFormContext<RecordingWindowForm>();
  const areaType = useWatch({ control, name: "areaType" });
  const useClientArea = useWatch({ control, name: "useClientArea" });

  const isWebsite = areaType === FlowAreaTypeEnum.BROWSER_TAB;

  // An app picked for one would be the wrong pick for the other, and a tab names no title.
  const switchType = (next: RecordingWindowForm["areaType"] | null) => {
    if (!next || next === areaType) return;

    setValue("areaType", next, { shouldValidate: true, shouldDirty: true });
    setValue("processName", "", { shouldValidate: true, shouldDirty: true });
    setValue("titlePattern", "", { shouldValidate: true, shouldDirty: true });
  };

  return (
    <div className="flex flex-column gap-4">
      <SelectButton
        value={areaType}
        options={TYPE_OPTIONS}
        optionLabel="label"
        optionValue="value"
        onChange={(e) => switchType(e.value)}
        allowEmpty={false}
        className="align-self-start"
      />

      {/* Its own each time, so the way of finding it starts over with the kind. */}
      <WindowMatchFieldsComponent
        key={areaType}
        isBrowser={isWebsite}
        useClientArea={useClientArea}
        bringsToFront={true}
      />

      {isWebsite && (
        <>
          <FormInputTextComponent
            fieldName="tabAddress"
            label="Address"
            placeholderText="shop.example.com/cart"
            isRequired={true}
            hintText="The tab whose address starts with this, with or without https:// and www. Launch opens the browser here when no tab has it."
          />
          <LabelComponent
            size="sm"
            color="secondary"
            text="Have it open, in front in its window, when you start. The recording follows that window, a sign-in on another site included."
          />
        </>
      )}

      {children}
    </div>
  );
}
