import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import WindowMatchFieldsComponent from "@/shared/components/form/WindowMatchFieldsComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepWindowSchema } from "@/features/flow-step/components/forms/window/flow-step-window.zod";
import { WINDOW_MODES } from "@/features/flow-step/components/forms/window/window-modes";

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
  const { control, getValues, setValue } = useFormContext();
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

  /**
   * Picking an app names the step, so the tree reads "Focus - Google Chrome" rather than three
   * identical "Window Focus" rows. Only while the name is still one this form wrote: changing the
   * dropdown must never overwrite something typed on purpose.
   */
  const nameFromWindow = (label: string) => {
    const current = getValues("name") as string;

    const action = WINDOW_MODES.find((x) => x.flowStepType === flowStepType);
    if (!action) return;

    const isUntouched =
      current.length === 0 ||
      WINDOW_MODES.some(
        (x) => current === x.defaultName || current.startsWith(`${x.label} - `),
      );

    if (isUntouched)
      setValue("name", `${action.label} - ${label}`, { shouldValidate: true, shouldDirty: true });
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

      <WindowMatchFieldsComponent
        isDisabled={isDisabled}
        onWindowPicked={nameFromWindow}
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
