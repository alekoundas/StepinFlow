import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";

import { Button } from "primereact/button";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { UsageCountTagComponent } from "@/shared/components/UsageCountTagComponent";
import { FlowLocationDto } from "@/shared/models/database/flow-location-dto";

import FlowLocationFormComponent from "@/features/flow-location/components/forms/FlowLocationFormComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowLocations">[];
  append: (item: FlowLocationDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowLocationDto) => void;
  formMode: FormMode;
  isDisabled?: boolean;
}

export function FlowLocationDataTableComponent({
  fields,
  append,
  remove,
  update,
  formMode,
  isDisabled = false,
}: Props) {
  const { openForm, closeAll } = useDialogStore();

  //  Add
  const openAdd = () => {
    openForm("flow-location-form", {
      headerText: "Add Location",
      formId: "flow-location-form",
      children: (
        <FlowLocationFormComponent
          defaultValues={new FlowLocationDto()}
          formId="flow-location-form"
          isFormInDialog={true}
          formMode="ADD"
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={(data) => handleSave(data)}
        />
      ),
    });
  };

  // Edit
  const openEdit = (index: number) => {
    openForm("flow-location-form", {
      headerText: "Edit Location",
      formId: "flow-location-form",
      children: (
        <FlowLocationFormComponent
          defaultValues={fields[index] as unknown as FlowLocationDto}
          formId="flow-location-form"
          isFormInDialog={true}
          formMode="EDIT"
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={(data) => handleSave(data, index)}
        />
      ),
    });
  };

  //  Save (add & edit)
  const handleSave = (data: FlowLocationDto, index?: number) => {
    closeAll();
    if (index !== undefined) {
      update(index, data);
    } else {
      append(data);
    }
  };

  // Delete by index
  const handleDelete = (index: number) => {
    const flowLocation = fields[index] as unknown as FlowLocationDto;

    if (flowLocation.flowStepsCount > 0) {
      const message =
        `"${flowLocation.name}" is used by ${flowLocation.flowStepsCount} step(s). ` +
        `Deleting it clears their location. Continue?`;
      if (!confirm(message)) return;
    } else if (!confirm("Delete this location?")) {
      return;
    }

    remove(index);
  };

  //  Columns
  const columns: DataTableColumnDto<FlowLocationDto>[] = [
    { field: "name", header: "Name", sortable: true },
    {
      field: "point",
      header: "Point",
      body: (row: FlowLocationDto) => `${row.locationX}, ${row.locationY}`,
    },
    {
      field: "flowStepsCount",
      header: "Used By",
      sortable: true,
      body: (row: FlowLocationDto) => (
        <UsageCountTagComponent count={row.flowStepsCount} />
      ),
    },
    {
      field: "actions",
      header: "Actions",
      isHidden: formMode === "VIEW",
      body: (row: FlowLocationDto, options: any) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() => openEdit(options.rowIndex)}
          onDelete={() => handleDelete(options.rowIndex)}
        />
      ),
    },
  ];

  return (
    <div className="mt-4">
      <div className="flex justify-between items-center mb-3">
        <h3 className="text-lg font-medium">Locations</h3>
        {!isDisabled && (
          <Button
            type="button"
            label="Add Location"
            icon="pi pi-plus"
            onClick={openAdd}
            size="small"
          />
        )}
      </div>

      <LocalDataTableComponent
        value={fields as unknown as FlowLocationDto[]}
        columns={columns}
        emptyMessage="No locations defined yet."
      />
    </div>
  );
}
