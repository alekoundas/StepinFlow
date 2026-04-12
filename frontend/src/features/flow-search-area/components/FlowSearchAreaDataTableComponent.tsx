import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";

import { Button } from "primereact/button";
import { useState } from "react";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";

import FlowSearchAreaFormComponent from "@/features/flow-search-area/components/forms/FlowSearchAreaFormComponent";
import { Tag } from "primereact/tag";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type z from "zod";

interface Props {
  // fields: FieldArrayWithId<FlowSearchAreaDto>[];
  // fields: FieldArrayWithId<any, "flowSearchAreas", "fieldId">[];
  fields: FieldArrayWithId<
    z.infer<typeof FlowSchema>,
    "flowSearchAreas",
    "fieldId"
  >[];
  append: (item: FlowSearchAreaDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowSearchAreaDto) => void;
  // move: (fromIndex: number, toIndex: number) => void;
  isDisabled?: boolean;
}

export function FlowSearchAreaDataTableComponent({
  fields,
  append,
  remove,
  update,
  // move,
  isDisabled = false,
}: Props) {
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editingItem, setEditingItem] = useState<FlowSearchAreaDto>(
    new FlowSearchAreaDto(),
  );

  const { openForm, closeAll } = useDialogStore();

  //  Add
  const openAdd = () => {
    setEditingIndex(null);
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
    setEditingIndex(index);
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
          onSubmit={(data) => handleSave(data)}
        />
      ),
    });
  };

  //  Save (add & edit)
  const handleSave = (data: FlowSearchAreaDto) => {
    closeAll();
    if (editingIndex !== null) {
      update(editingIndex, data);
      setEditingIndex(null);
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
        if (row.flowSearchAreaType === "CUSTOM") {
          return `${row.locationX}, ${row.locationY} (${row.width}×${row.height})`;
        }
        if (row.flowSearchAreaType === "APPLICATION") {
          return row.applicationName || "-";
        }
        if (row.flowSearchAreaType === "MONITOR") {
          return `Monitor ${row.monitorIndex}`;
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
      body: (row: FlowSearchAreaDto) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() => openEdit(row.id)}
          onDelete={() => handleDelete(row.id)}
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
    } else if (rowData.monitorIndex) {
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
