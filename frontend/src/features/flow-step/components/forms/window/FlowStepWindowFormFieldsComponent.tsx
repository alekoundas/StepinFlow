import type z from "zod";
import { useEffect } from "react";
import { useFormContext, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";

import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
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
  const { control, setValue } = useFormContext();
  const flowStepType = useWatch({ control, name: "flowStepType" });
  const flowAreaId = useWatch({ control, name: "flowAreaId" });
  const isCloseForced = useWatch({ control, name: "isCloseForced" });
  const isClose = flowStepType === FlowStepTypeEnum.WINDOW_CLOSE;

  // The lookup says each area's type as its description. A tab's program is the whole browser, so
  // a close of one is never forced.
  const { data: areaTypes = {} } = useQuery({
    queryKey: ["lookup", "flowArea", "types", flowId],
    queryFn: () =>
      backendApiService.Lookup.flowArea({ flowId }).then((res) =>
        Object.fromEntries(res.data.map((item) => [Number(item.value), item.description])),
      ),
    enabled: !!flowId && isClose,
  });
  const isTab = !!flowAreaId && areaTypes[flowAreaId] === FlowAreaTypeEnum.BROWSER_TAB;

  useEffect(() => {
    if (isTab && isCloseForced) setValue("isCloseForced", false, { shouldDirty: true });
  }, [isTab, isCloseForced, setValue]);

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
        hintText="An application, or a browser tab, which is brought to the front of its window first. Change how it is found on the Areas tab, and every step using it follows."
        flowAreaTypes={[FlowAreaTypeEnum.APPLICATION, FlowAreaTypeEnum.BROWSER_TAB]}
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

      {isClose && (
        <div className="mt-3">
          <FormInputNumberComponent
            fieldName="timeoutMilliseconds"
            label="Wait for it to go (ms)"
            min={0}
            max={2147483647}
            isDisabled={isDisabled}
            hintText="0 = 5 seconds"
          />

          {isTab ? (
            <LabelComponent
              size="sm"
              color="secondary"
              text="A tab is closed the way Ctrl+W closes it. It is never forced: its program is the whole browser, every tab in it."
            />
          ) : (
            <FormInputCheckboxComponent
              fieldName="isCloseForced"
              label="Force it if it is still open"
              isDisabled={isDisabled}
              hintText="Ends the window's program, with every window it has and nothing saved. Left unticked, a window still asking whether to save is a failure."
            />
          )}
        </div>
      )}
    </>
  );
}
