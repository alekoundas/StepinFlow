import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";

import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { UsageCountTagComponent } from "@/shared/components/UsageCountTagComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import { FlowSearchAreaTypeEnum } from "@/shared/enums/backend/flow-search-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

import FlowSearchAreaFormComponent from "@/features/flow-search-area/components/forms/FlowSearchAreaFormComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowSearchAreas", "fieldId">[];
  append: (item: FlowSearchAreaDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowSearchAreaDto) => void;
  formMode: FormMode;
  isDisabled?: boolean;
}

const FORM_ID = "search-area-form";

export function FlowSearchAreaDataTableComponent({
  fields,
  append,
  remove,
  update,
  formMode,
  isDisabled = false,
}: Props) {
  const { openForm, closeAll } = useDialogStore();

  const areas = fields as unknown as FlowSearchAreaDto[];

  // Depth is capped at one level, so only areas without a parent may become one.
  const frames = areas.filter((x) => !x.parentFlowSearchAreaId);
  const roots = frames;

  const childrenOf = (parent: FlowSearchAreaDto) =>
    areas.filter((x) => x.parentFlowSearchAreaId === parent.id);

  // A new row gets a negative id so a child added in the same save can point at it. The
  // backend swaps them for real ids once the inserts happen.
  const nextTempId = () =>
    Math.min(0, ...areas.map((x) => x.id)) - 1;

  const openEditor = (
    mode: FormMode,
    defaults: FlowSearchAreaDto,
    index?: number,
  ) => {
    openForm(FORM_ID, {
      headerText: mode === "ADD" ? "Add Area" : "Edit Area",
      formId: FORM_ID,
      children: (
        <FlowSearchAreaFormComponent
          defaultValues={defaults}
          formId={FORM_ID}
          isFormInDialog={true}
          formMode={mode}
          parentOptions={frames.filter((x) => x.id !== defaults.id)}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={(data) => handleSave(data, index)}
        />
      ),
    });
  };

  const handleSave = (data: FlowSearchAreaDto, index?: number) => {
    closeAll();
    if (index !== undefined) {
      update(index, data);
    } else {
      append(data);
    }
  };

  const handleDelete = (row: FlowSearchAreaDto) => {
    const index = areas.findIndex((x) => x.id === row.id);
    if (index === -1) return;

    const childCount = childrenOf(row).length;
    const message =
      row.flowStepsCount > 0 || childCount > 0
        ? `"${row.name}" is used by ${row.flowStepsCount} step(s)` +
          (childCount > 0 ? ` and contains ${childCount} region(s)` : "") +
          `. Deleting it clears those references. Continue?`
        : "Delete this area?";

    if (!confirm(message)) return;
    remove(index);
  };

  const typeBodyTemplate = (row: FlowSearchAreaDto) => {
    const map = {
      [FlowSearchAreaTypeEnum.CUSTOM]: { label: "Region", severity: "info" },
      [FlowSearchAreaTypeEnum.APPLICATION]: { label: "Application", severity: "success" },
      [FlowSearchAreaTypeEnum.BROWSER_TAB]: { label: "Browser tab", severity: "warning" },
      [FlowSearchAreaTypeEnum.MONITOR]: { label: "Monitor", severity: "warning" },
    } as const;

    const tag = map[row.type] ?? map[FlowSearchAreaTypeEnum.CUSTOM];
    return (
      <Tag
        value={tag.label}
        severity={tag.severity}
      />
    );
  };

  const detailsBodyTemplate = (row: FlowSearchAreaDto) => {
    if (row.type === FlowSearchAreaTypeEnum.CUSTOM) {
      return row.sizingMode === AreaSizingModeEnum.RATIO
        ? `${Math.round(row.ratioWidth * 100)}% × ${Math.round(row.ratioHeight * 100)}%`
        : `${row.locationX}, ${row.locationY} (${row.width}×${row.height})`;
    }
    if (row.type === FlowSearchAreaTypeEnum.MONITOR) return row.monitorUniqueId || "-";
    if (row.type === FlowSearchAreaTypeEnum.BROWSER_TAB)
      return row.tabMatchValue || row.processName || "-";

    return row.processName || row.titlePattern || "-";
  };

  const buildColumns = (
    isChildTable: boolean,
  ): DataTableColumnDto<FlowSearchAreaDto>[] => [
    { field: "name", header: "Name", sortable: !isChildTable },
    { field: "type", header: "Type", body: typeBodyTemplate },
    { field: "details", header: "Details", body: detailsBodyTemplate },
    {
      field: "flowStepsCount",
      header: "Used By",
      body: (row) => <UsageCountTagComponent count={row.flowStepsCount} />,
    },
    {
      field: "actions",
      header: "Actions",
      isHidden: formMode === "VIEW",
      body: (row) => (
        <ActionsMenuComponent
          id={row.id}
          onEdit={() =>
            openEditor("EDIT", row, areas.findIndex((x) => x.id === row.id))
          }
          onDelete={() => handleDelete(row)}
        />
      ),
    },
  ];

  // Regions inside a frame. Adding one from here pre-fills the parent, so the user picks a
  // frame by where they clicked rather than from a dropdown.
  const rowExpansionTemplate = (parent: FlowSearchAreaDto) => (
    <div className="p-3">
      <div className="flex justify-content-between align-items-center mb-2">
        <LabelComponent
          text={`Regions inside ${parent.name}`}
          weight="semibold"
          size="sm"
        />
        {!isDisabled && (
          <Button
            type="button"
            label="Add region inside"
            icon="pi pi-plus"
            size="small"
            onClick={() =>
              openEditor(
                "ADD",
                new FlowSearchAreaDto({
                  id: nextTempId(),
                  type: FlowSearchAreaTypeEnum.CUSTOM,
                  parentFlowSearchAreaId: parent.id,
                  flowId: parent.flowId,
                }),
              )
            }
          />
        )}
      </div>

      <LocalDataTableComponent
        value={childrenOf(parent)}
        columns={buildColumns(true)}
        emptyMessage="No regions inside this one yet."
      />
    </div>
  );

  return (
    <div className="mt-4">
      <div className="flex justify-between items-center mb-3">
        <h3 className="text-lg font-medium">Areas</h3>
        {!isDisabled && (
          <Button
            type="button"
            label="Add Area"
            icon="pi pi-plus"
            onClick={() =>
              openEditor("ADD", new FlowSearchAreaDto({ id: nextTempId() }))
            }
            size="small"
          />
        )}
      </div>

      <LocalDataTableComponent
        value={roots}
        columns={buildColumns(false)}
        dataKey="id"
        rowExpansionTemplate={rowExpansionTemplate}
        isRowExpandable={(row) => row.type !== FlowSearchAreaTypeEnum.CUSTOM}
        emptyMessage="No areas defined yet."
      />
    </div>
  );
}
