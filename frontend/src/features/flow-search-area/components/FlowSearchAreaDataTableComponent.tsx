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

interface Props {
  fields: FieldArrayWithId<FlowSearchAreaDto>[];
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

  // const openAdd = () => {
  //   setEditingIndex(null);
  //   setEditingItem(new FlowSearchAreaDto());
  //   // setDialogVisible(true);
  //   openForm("Search Area", {
  //     header: "Add Search Area",
  //     children: (
  //       <FlowSearchAreaFormComponent
  //         defaultValues={editingItem}
  //         formMode="ADD"
  //         onCancel={() => closeAll()}
  //         onEdit={() => closeAll()}
  //         onSubmit={(data) => {
  //           handleSave(data);
  //         }}
  //       />
  //     ),
  //   });
  // };

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
          onCancel={() => closeAll()}
          onEdit={() => closeAll()}
          onSubmit={(data) => handleSave(data)}
        />
      ),
    });
  };

  // const openEdit = (index: number, item: FlowSearchAreaDto) => {
  //   setEditingIndex(index);
  //   setEditingItem(item);
  //   // setDialogVisible(true);
  //   openForm("search-area-form", {
  //     headerText: "Add Search Area",
  //     formId: "search-area-form",
  //     children: (
  //       <FlowSearchAreaFormComponent
  //         defaultValues={new FlowSearchAreaDto()}
  //         formId="search-area-form"
  //         isFormInDialog={true}
  //         formMode="EDIT"
  //         onCancel={() => closeAll()}
  //         onEdit={() => closeAll()}
  //         onSubmit={(data) => handleSave(data)}
  //       />
  //     ),
  //   });
  // };

  const handleSave = (data: FlowSearchAreaDto) => {
    closeAll();
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

  const columns: DataTableColumnDto<FieldArrayWithId<FlowSearchAreaDto>>[] = [
    { field: "name", header: "Name", sortable: true },
    {
      field: "flowSearchAreaType",
      header: "Type",
      // body: (row: FlowSearchAreaDto) => row.flowSearchAreaType,
    },
    {
      field: "details",
      header: "Details",
      body: (row: FieldArrayWithId<FlowSearchAreaDto>) => {
        // if (row.flowSearchAreaType === "CUSTOM") {
        //   return `${row.locationX}, ${row.locationY} (${row.width}×${row.height})`;
        // }
        // if (row.flowSearchAreaType === "APPLICATION") {
        //   return row.applicationName || "-";
        // }
        // if (row.flowSearchAreaType === "MONITOR") {
        //   return `Monitor ${row.monitorIndex}`;
        // }
        return "-";
      },
    },
    {
      field: "details2",
      header: "Details22",
      // body: (row: FieldArrayWithId<FlowSearchAreaDto>) => typeBodyTemplate(row),
    },
    {
      field: "actions",
      header: "Actions",
      body: (row: FieldArrayWithId<FlowSearchAreaDto>) => (
        <ActionsMenuComponent
          id={row.id}
          // onEdit={() => openEdit(row.id, row)}
          onDelete={() => handleDelete(row.id)}
          onClone={() => {
            /* optional */
          }}
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
      {/* <Dialog
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
      </Dialog> */}
    </div>
  );
}
