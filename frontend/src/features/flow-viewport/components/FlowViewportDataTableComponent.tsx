import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";
import type z from "zod";

import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { FlowViewportDto } from "@/shared/models/database/flow-viewport-dto";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";
import FlowViewportFormComponent from "@/features/flow-viewport/components/forms/FlowViewportFormComponent";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowViewports", "fieldId">[];
  append: (item: FlowViewportDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowViewportDto) => void;
  formMode: FormMode;
  isDisabled?: boolean;
}

const FORM_ID = "flow-viewport-form";

export function FlowViewportDataTableComponent({
  fields,
  append,
  remove,
  update,
  formMode,
  isDisabled = false,
}: Props) {
  const { openForm, closeAll } = useDialogStore();

  const viewports = fields as unknown as FlowViewportDto[];

  const openEditor = (mode: FormMode, defaults: FlowViewportDto, index?: number) => {
    openForm(FORM_ID, {
      headerText: mode === "ADD" ? "Add Screen Size" : "Edit Screen Size",
      formId: FORM_ID,
      children: (
        <FlowViewportFormComponent
          defaultValues={defaults}
          formId={FORM_ID}
          isFormInDialog={true}
          formMode={mode}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={(data) => handleSave(data, index)}
        />
      ),
    });
  };

  const handleSave = (data: FlowViewportDto, index?: number) => {
    closeAll();
    if (index !== undefined) {
      update(index, data);
    } else {
      append(data);
    }
  };

  const handleDelete = (index: number) => {
    const viewport = viewports[index];

    const message =
      `Stop running this flow at ${viewport.width} x ${viewport.height}? ` +
      `Past executions at that size are kept, but nothing new will run there.`;

    if (!confirm(message)) return;

    remove(index);
  };

  const columns: DataTableColumnDto<FlowViewportDto>[] = [
    {
      field: "size",
      header: "Size",
      body: (row) => (
        <Tag
          value={`${row.width} x ${row.height}`}
          severity="info"
        />
      ),
    },
    {
      field: "actions",
      header: "Actions",
      isHidden: formMode === "VIEW",
      body: (row, options) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() => openEditor("EDIT", row, options?.rowIndex)}
          onDelete={() => handleDelete(options!.rowIndex)}
        />
      ),
    },
  ];

  return (
    <div className="mt-4">
      <div className="flex justify-between items-center mb-3">
        <h3 className="text-lg font-medium">Screen sizes</h3>
        {!isDisabled && (
          <Button
            type="button"
            label="Add Screen Size"
            icon="pi pi-plus"
            onClick={() =>
              openEditor(
                "ADD",
                new FlowViewportDto({
                  id: Math.min(0, ...viewports.map((x) => x.id)) - 1,
                }),
              )
            }
            size="small"
          />
        )}
      </div>

      <LocalDataTableComponent
        value={viewports}
        columns={columns}
        emptyMessage="No screen sizes yet. Without one the flow runs at whatever size the window happens to be."
      />
    </div>
  );
}
