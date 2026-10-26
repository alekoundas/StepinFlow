import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import FlowStepAreaFieldComponent from "@/features/flow-step/components/forms/shared/FlowStepAreaFieldComponent";
import { FlowStepWindowSchema } from "@/features/flow-step/components/forms/window/flow-step-window.zod";

type WindowForm = z.infer<typeof FlowStepWindowSchema>;

interface IdOption {
  label: string;
  value: number;
  description?: string;
}

interface Props {
  flowId: number | undefined;
  isDisabled?: boolean;
}

export default function FlowStepWindowFormFieldsComponent({
  flowId,
  isDisabled = false,
}: Props) {
  const { control } = useFormContext();
  const flowStepType = useWatch({ control, name: "flowStepType" });

  const loadLocations = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.flowPoint({ searchText: filter, flowId }).then(
      (res) =>
        res.data.map((item) => ({
          label: item.label,
          value: Number(item.value),
          description: item.description,
        })),
    );

  return (
    <>
      <FlowStepAreaFieldComponent
        flowId={flowId}
        labelText="Window"
        hintText="A window area, found by its application and title. Change how it is found on the Areas tab, and every step using it follows."
        flowAreaType={FlowAreaTypeEnum.APPLICATION}
        isDisabled={isDisabled}
      />

      {flowStepType === FlowStepTypeEnum.WINDOW_RESIZE && (
        <>
          <div className="flex gap-3 mt-3">
            <FormInputNumberComponent
              fieldName="windowWidth"
              label="Width"
              min={1}
              max={2147483647}
              isRequired={true}
              isDisabled={isDisabled}
            />
            <FormInputNumberComponent
              fieldName="windowHeight"
              label="Height"
              min={1}
              max={2147483647}
              isRequired={true}
              isDisabled={isDisabled}
            />
          </div>

          <LabelComponent
            size="sm"
            color="secondary"
            text="The whole window, title bar and borders included. Fixing the size is what makes templates and click points match on someone else's machine."
          />
        </>
      )}

      {flowStepType === FlowStepTypeEnum.WINDOW_RELOCATE && (
        <div className="mt-3">
          <FormDropdownComponent<WindowForm, IdOption>
            fieldName="flowPointId"
            labelText="Move to"
            mode="remote"
            queryKey={["lookup", "flowPoint", flowId]}
            queryFn={loadLocations}
            optionLabel="label"
            optionValue="value"
            placeholderText="Select a location..."
            isRequired={true}
            isDisabled={isDisabled}
            hintText="The outer corner of the window lands here, title bar included."
          />
        </div>
      )}
    </>
  );
}
