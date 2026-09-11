import { FormDropdownComponent } from "@/shared/components/form/FormDropdownComponent";
import { FormInputTextComponent } from "@/shared/components/form/FormInputTextComponent";
import { FormSelectButtonComponent } from "@/shared/components/form/FormSelectButtonComponent";
import { AppCloseModeEnum } from "@/shared/enums/backend/app-close-mode-enum";
import { backendApiService } from "@/shared/services/backend-api-service";
import type { FlowDto } from "@/shared/models/database/flow-dto";

interface IdOption {
  label: string;
  value: number;
}

interface Props {
  flowId?: number;
  isDisabled?: boolean;
}

export function FlowFormFieldsComponent({ flowId, isDisabled = false }: Props) {
  const loadAreas = (filter?: string): Promise<IdOption[]> =>
    backendApiService.Lookup.flowArea({ searchText: filter, flowId }).then((res) =>
      res.data.map((item) => ({
        label: item.label,
        value: Number(item.value),
      })),
    );

  return (
    <>
      <FormInputTextComponent
        fieldName="name"
        label="Name"
        isRequired={true}
        isDisabled={isDisabled}
      />

      <FormInputTextComponent
        fieldName="description"
        label="Description"
        placeholderText="Logs in and downloads this month's invoices"
        hintText="Shown in the list. Worth a line, so you can tell two flows apart at a glance."
        isDisabled={isDisabled}
      />

      <FormDropdownComponent<FlowDto, IdOption>
        fieldName="appUnderTestAreaId"
        labelText="Application under test"
        mode="remote"
        queryKey={["lookup", "flowArea", flowId]}
        queryFn={loadAreas}
        optionLabel="label"
        optionValue="value"
        placeholderText="Select the area bound to it..."
        isDisabled={isDisabled || !flowId}
        hintText="The window a viewport resizes. Every other window step is left alone."
      />

      <FormSelectButtonComponent<FlowDto, AppCloseModeEnum>
        fieldName="appCloseMode"
        labelText="When an execution ends"
        options={[
          { label: "Leave it", value: AppCloseModeEnum.LEAVE },
          { label: "Close it", value: AppCloseModeEnum.CLOSE_WINDOW },
          { label: "Kill it", value: AppCloseModeEnum.KILL_PROCESS },
        ]}
        isDisabled={isDisabled}
        hintText="Runs whatever the verdict. A failed pass that leaves the application open makes the next screen size fail for the wrong reason."
      />
    </>
  );
}
