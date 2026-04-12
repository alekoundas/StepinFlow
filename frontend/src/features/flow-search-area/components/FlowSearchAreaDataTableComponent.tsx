import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";

import { Button } from "primereact/button";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";

import FlowSearchAreaFormComponent from "@/features/flow-search-area/components/forms/FlowSearchAreaFormComponent";
import { Tag } from "primereact/tag";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowSearchAreas">[];
  append: (item: FlowSearchAreaDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowSearchAreaDto) => void;
  formMode: FormMode;
  // move: (fromIndex: number, toIndex: number) => void;
  isDisabled?: boolean;
}

export function FlowSearchAreaDataTableComponent({
  fields,
  append,
  remove,
  update,
  // move,
  formMode,
  isDisabled = false,
}: Props) {
  const { openForm, closeAll } = useDialogStore();

  //  Add
  const openAdd = () => {
    openForm("search-area-form", {
      headerText: "Add Search Area",
      formId: "search-area-form",
      children: (
        <FlowSearchAreaFormComponent
          defaultValues={new FlowSearchAreaDto()}
          formId="search-area-form"
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
    openForm("search-area-form", {
      headerText: "Edit Search Area",
      formId: "search-area-form",
      children: (
        <FlowSearchAreaFormComponent
          defaultValues={fields[index] as unknown as FlowSearchAreaDto} // Fuck this double cast
          formId="search-area-form"
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
  const handleSave = (data: FlowSearchAreaDto, index?: number) => {
    closeAll();
    if (index !== undefined) {
      update(index, data);
    } else {
      append(data);
    }
  };

  // Delete by index
  const handleDelete = (index: number) => {
    if (confirm("Delete this search area?")) {
      remove(index);
    }
  };

  //  Columns
  const columns: DataTableColumnDto<FlowSearchAreaDto>[] = [
    { field: "name", header: "Name", sortable: true },
    {
      field: "flowSearchAreaType",
      header: "Type",
      // body: (row: FlowSearchAreaDto) => row.flowSearchAreaType,
    },
    {
      field: "details",
      header: "Details",
      body: (row: FlowSearchAreaDto) => {
        if (row.type === "CUSTOM") {
          return `${row.locationX}, ${row.locationY} (${row.width}×${row.height})`;
        }
        if (row.type === "APPLICATION") {
          return row.applicationName || "-";
        }
        if (row.type === "MONITOR") {
          return `Monitor ${row.monitorName}`;
        }
        return "-";
      },
    },
    {
      field: "details2",
      header: "Details22",
      body: (row: FlowSearchAreaDto) => typeBodyTemplate(row),
    },
    {
      field: "actions",
      header: "Actions",
      isHidden: formMode === "VIEW",
      body: (row: FlowSearchAreaDto, options: any) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() => openEdit(options.rowIndex)}
          onDelete={() => handleDelete(options.rowIndex)}
          // onClone={() => {}}
          // disabled={isDisabled}
        />
      ),
    },
  ];

  const typeBodyTemplate = (rowData: FlowSearchAreaDto) => {
    let label = "Custom Area";
    let severity: "success" | "info" | "warning" = "info";

    if (rowData.applicationName) {
      label = "Application";
      severity = "success";
    } else if (rowData.monitorName) {
      label = "Monitor";
      severity = "warning";
    }

    return (
      <Tag
        value={label}
        severity={severity}
      />
    );
  };

  return (
    <div className="mt-4">
      <div className="flex justify-between items-center mb-3">
        <h3 className="text-lg font-medium">Search Areas</h3>
        {!isDisabled && (
          <Button
            type="button"
            label="Add Search Area"
            icon="pi pi-plus"
            onClick={openAdd}
            size="small"
          />
        )}
      </div>

      <LocalDataTableComponent
        value={fields as unknown as FlowSearchAreaDto[]} // Also Fuck this double cast
        columns={columns}
        emptyMessage="No search areas defined yet."
      />
    </div>
  );
}
