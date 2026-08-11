import type z from "zod";
import { useFormContext, useWatch } from "react-hook-form";
import { useQuery } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Slider } from "primereact/slider";
import { Panel } from "primereact/panel";
import { Message } from "primereact/message";

import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormInputNumberComponent } from "@/shared/components/form/FormInputNumberComponent";
import { FormInputCheckboxComponent } from "@/shared/components/form/FormInputCheckboxComponent";
import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { ImageSearchModeEnum } from "@/shared/enums/backend/image-search-mode-enum";
import { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";
import { FlowStepImageSearchSchema } from "@/features/flow-step/components/forms/image-search/flow-step-image-search.zod";
import { IMAGE_SEARCH_MODES } from "@/features/flow-step/components/forms/image-search/image-search-modes";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { useFlowSearchAreaMutations } from "@/features/flow-search-area/hooks/use-flow-search-area";
import FlowSearchAreaFormComponent from "@/features/flow-search-area/components/forms/FlowSearchAreaFormComponent";
import { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";

type ImageSearchForm = z.infer<typeof FlowStepImageSearchSchema>;

interface IdOption {
  label: string;
  value: number;
}

interface EnumOption {
  label: string;
  value: string;
}

interface Props {
  flowId: number | undefined;
  templateCount: number;
  isDisabled?: boolean;
}

export default function FlowStepImageSearchFormFieldsComponent({
  flowId,
  templateCount,
  isDisabled = false,
}: Props) {
  const { control, setValue } = useFormContext();

  const mode = useWatch({ control, name: "imageSearchMode" });
  const accuracy = useWatch({ control, name: "accuracy" });
  const loopOnMultipleFindings = useWatch({
    control,
    name: "loopOnMultipleFindings",
  });
  const timeout = useWatch({ control, name: "timeoutMilliseconds" });
  const pollInterval = useWatch({ control, name: "pollIntervalMilliseconds" });

  const isWaiting = mode !== ImageSearchModeEnum.FIND_ONCE;

  const { openForm, closeAll } = useDialogStore();
  const { createFlowSearchAreaMutation } = useFlowSearchAreaMutations();

  const loadAreas = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.flowSearchArea({ searchText: filter, flowId }).then(
      (res) =>
        res.data.map((item) => ({
          label: item.label,
          value: Number(item.value),
        })),
    );

  // Frames a new region could sit inside. Only id and name are used by the picker.
  const { data: parentOptions = [] } = useQuery({
    queryKey: ["lookup", "flowSearchArea", "parents", flowId],
    queryFn: () =>
      backendApiService.Lookup.flowSearchArea({ flowId }).then((res) =>
        res.data.map(
          (item) =>
            new FlowSearchAreaDto({
              id: Number(item.value),
              name: item.label,
              flowId: flowId ?? 0,
            }),
        ),
      ),
    enabled: !!flowId,
  });

  // Saved straight away so the dropdown has a real id to bind to, same as the cursor step does
  // for locations.
  const openAddArea = () => {
    openForm("search-area-form", {
      headerText: "Add Area",
      formId: "search-area-form",
      children: (
        <FlowSearchAreaFormComponent
          defaultValues={new FlowSearchAreaDto({ flowId: flowId ?? 0 })}
          formId="search-area-form"
          isFormInDialog={true}
          formMode="ADD"
          parentOptions={parentOptions}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={async (data) => {
            closeAll();
            const newId = await createFlowSearchAreaMutation.mutateAsync({
              ...data,
              id: 0,
              flowId: flowId ?? 0,
            });
            setValue("flowSearchAreaId", newId, {
              shouldValidate: true,
              shouldDirty: true,
            });
          }}
        />
      ),
    });
  };

  // The semantics are fiddly enough that spelling them out beats another tooltip.
  const summary = () => {
    const what =
      templateCount === 0
        ? "no templates yet"
        : templateCount === 1
          ? "1 template"
          : `${templateCount} templates`;

    if (mode === ImageSearchModeEnum.WAIT_UNTIL_GONE) {
      return `Waits until ${what} is no longer on screen, checking every ${pollInterval}ms${
        timeout > 0 ? ` for up to ${timeout}ms` : " for as long as it takes"
      }. Then runs the Success steps once. If it is still there when time runs out, runs the Failure steps.`;
    }

    if (mode === ImageSearchModeEnum.WAIT_UNTIL_FOUND) {
      return `Waits for ${what} to appear, checking every ${pollInterval}ms${
        timeout > 0 ? ` for up to ${timeout}ms` : " for as long as it takes"
      }. On the first match, runs the Success steps once. If nothing appears in time, runs the Failure steps.`;
    }

    return loopOnMultipleFindings
      ? `Looks for ${what} once and runs the Success steps for every match found. If nothing matches, runs the Failure steps.`
      : `Looks for ${what} once and runs the Success steps for the best match. If nothing matches, runs the Failure steps.`;
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

      <FormSelectButtonComponent
        fieldName="imageSearchMode"
        labelText="Mode"
        options={IMAGE_SEARCH_MODES.map((x) => ({
          label: x.label,
          value: x.value,
        }))}
        isDisabled={isDisabled}
        isRequired={true}
        hintText={IMAGE_SEARCH_MODES.find((x) => x.value === mode)?.description}
      />

      <div className="flex gap-3 align-items-end">
        <div className="flex-1">
          <FormDropdownComponent<ImageSearchForm, IdOption>
            fieldName="flowSearchAreaId"
            labelText="Where to look"
            mode="remote"
            queryKey={["lookup", "flowSearchArea", flowId]}
            queryFn={loadAreas}
            optionLabel="label"
            optionValue="value"
            placeholderText="Select an area..."
            isRequired={true}
            isDisabled={isDisabled}
            hintText="A smaller area is the single biggest thing that makes the search fast."
          />
        </div>

        <Button
          type="button"
          icon="pi pi-plus"
          label="New"
          onClick={openAddArea}
          disabled={isDisabled || !flowId}
          className="p-button-outlined mb-3"
          tooltip="Create a search area and use it here"
          tooltipOptions={{ position: "top" }}
        />
      </div>

      {isWaiting && (
        <div className="flex gap-3">
          <FormInputNumberComponent
            fieldName="pollIntervalMilliseconds"
            label="Check every (ms)"
            min={50}
            max={2147483647}
            isRequired={true}
            isDisabled={isDisabled}
          />
          <FormInputNumberComponent
            fieldName="timeoutMilliseconds"
            label="Give up after (ms)"
            min={0}
            max={2147483647}
            isDisabled={isDisabled}
            hintText="0 = wait forever"
          />
        </div>
      )}

      <Panel
        header="Matching"
        toggleable
        collapsed
        className="mt-3"
      >
        <div className="field">
          <LabelComponent
            text={`Accuracy: ${Number(accuracy ?? 0).toFixed(2)}`}
            weight="bold"
          />
          <Slider
            value={accuracy}
            min={0.1}
            max={1}
            step={0.01}
            disabled={isDisabled}
            onChange={(e) =>
              setValue("accuracy", e.value as number, {
                shouldValidate: true,
                shouldDirty: true,
              })
            }
            className="mt-2"
          />
          <LabelComponent
            size="xs"
            color="secondary"
            text="Higher is stricter. Below about 0.7 you will start matching things you did not mean."
            className="mt-1"
          />
        </div>

        <FormDropdownComponent<ImageSearchForm, EnumOption>
          fieldName="templateMatchMode"
          labelText="Match mode"
          mode="local"
          options={Object.values(TemplateMatchModeEnum).map((value) => ({
            label: value,
            value,
          }))}
          optionLabel="label"
          optionValue="value"
          isRequired={true}
          isDisabled={isDisabled}
          hintText="The normalized modes are the ones where the accuracy number means something."
        />

        {!isWaiting && (
          <>
            <FormInputCheckboxComponent
              fieldName="loopOnMultipleFindings"
              label="Run the Success steps once per match"
              isDisabled={isDisabled}
            />

            {loopOnMultipleFindings && (
              <FormInputNumberComponent
                fieldName="maxMatches"
                label="Stop after"
                min={1}
                max={2147483647}
                isDisabled={isDisabled}
                hintText="Safety cap, so a loose threshold cannot fire hundreds of times."
              />
            )}
          </>
        )}
      </Panel>

      <Message
        severity="info"
        className="w-full justify-content-start mt-3"
        text={summary()}
      />
    </>
  );
}
