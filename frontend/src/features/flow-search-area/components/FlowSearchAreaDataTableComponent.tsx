import { Button } from "primereact/button";
import {
  type FieldArrayWithId,
  type UseFieldArrayReturn,
} from "react-hook-form";
import { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { useState } from "react";
import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import type { FlowSchema } from "@/features/flow/schema/flow-schema.zod";
import type z from "zod";
import { Dialog } from "primereact/dialog";
import FlowSearchAreaFormComponent from "@/features/flow-search-area/components/forms/FlowSearchAreaFormComponent";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowSearchAreas">[];
  append: UseFieldArrayReturn["append"];
  remove: UseFieldArrayReturn["remove"];
  update: UseFieldArrayReturn["update"];
  move?: UseFieldArrayReturn["move"];
  isDisabled?: boolean;
}

export function FlowSearchAreaDataTableComponent({
  fields,
  append,
  remove,
  update,
  move,
  isDisabled = false,
}: Props) {
  const [dialogVisible, setDialogVisible] = useState(false);
  const [editingIndex, setEditingIndex] = useState<number | null>(null);
  const [editingItem, setEditingItem] = useState<FlowSearchAreaDto>(
    new FlowSearchAreaDto(),
  );

  const openAdd = () => {
    setEditingIndex(null);
    setEditingItem(new FlowSearchAreaDto());
    setDialogVisible(true);
  };

  const openEdit = (index: number, item: FlowSearchAreaDto) => {
    setEditingIndex(index);
    setEditingItem(item);
    setDialogVisible(true);
  };

  const handleSave = (data: FlowSearchAreaDto) => {
    if (editingIndex !== null) {
      update(editingIndex, data);
    } else {
      append(data);
    }
  };

  const handleDelete = (index: number) => {
    // Optional: confirm dialog
    if (confirm("Delete this search area?")) {
      remove(index);
    }
  };

  const columns: DataTableColumnDto<FlowSearchAreaDto>[] = [
    { field: "name", header: "Name", sortable: true },
    {
      field: "flowSearchAreaType",
      header: "Type",
      body: (row: FlowSearchAreaDto) => row.flowSearchAreaType,
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
      field: "actions",
      header: "Actions",
      body: (row: FlowSearchAreaDto) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() => openEdit(row.id, row)}
          onDelete={() => handleDelete(row.id)}
          onClone={() => {
            /* optional */
          }}
          // disabled={isDisabled}
        />
      ),
    },
  ];

  return (
    <div className="mt-4">
      <div className="flex justify-between items-center mb-3">
        <h3 className="text-lg font-medium">Search Areas</h3>
        {!isDisabled && (
          <Button
            label="Add Search Area"
            icon="pi pi-plus"
            onClick={openAdd}
            size="small"
          />
        )}
      </div>

      <LocalDataTableComponent
        value={fields}
        columns={columns}
        emptyMessage="No search areas defined yet."
      />

      {/* Dialog */}
      {/* <SearchAreaDialog
        visible={dialogVisible}
        onHide={() => setDialogVisible(false)}
        onSave={handleSave}
        initialData={editingItem}
        isEdit={editingIndex !== null}
      /> */}
      <Dialog
        header={editingIndex !== null ? "Edit Search Area" : "New Search Area"}
        visible={dialogVisible}
        onHide={() => setDialogVisible(false)}
        style={{ width: "520px" }}
        maximizable
      >
        <FlowSearchAreaFormComponent
          defaultValues={editingItem}
          formMode="ADD"
          onCancel={() => setDialogVisible(false)}
          onEdit={() => setDialogVisible(false)}
          onSubmit={handleSave}
        />
      </Dialog>
    </div>
  );
}
