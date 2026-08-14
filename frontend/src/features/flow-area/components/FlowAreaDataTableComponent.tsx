import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";

import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { UsageCountTagComponent } from "@/shared/components/UsageCountTagComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import { FlowAreaTypeEnum } from "@/shared/enums/backend/flow-area-type.enum";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

import FlowAreaFormComponent from "@/features/flow-area/components/forms/FlowAreaFormComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowAreas", "fieldId">[];
  append: (item: FlowAreaDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowAreaDto) => void;
  formMode: FormMode;
  isDisabled?: boolean;
}

const FORM_ID = "search-area-form";

export function FlowAreaDataTableComponent({
  fields,
  append,
  remove,
  update,
  formMode,
  isDisabled = false,
}: Props) {
  const { openForm, closeAll } = useDialogStore();

  const areas = fields as unknown as FlowAreaDto[];

  // Depth is capped at one level, so only areas without a parent may become one.
  const frames = areas.filter((x) => !x.parentFlowAreaId);
  const roots = frames;

  const childrenOf = (parent: FlowAreaDto) =>
    areas.filter((x) => x.parentFlowAreaId === parent.id);

  // A new row gets a negative id so a child added in the same save can point at it. The
  // backend swaps them for real ids once the inserts happen.
  const nextTempId = () =>
    Math.min(0, ...areas.map((x) => x.id)) - 1;

  const openEditor = (
    mode: FormMode,
    defaults: FlowAreaDto,
    index?: number,
  ) => {
    openForm(FORM_ID, {
      headerText: mode === "ADD" ? "Add Area" : "Edit Area",
      formId: FORM_ID,
      children: (
        <FlowAreaFormComponent
          defaultValues={defaults}
          formId={FORM_ID}
          isFormInDialog={true}
          formMode={mode}
          parentOptions={frames.filter((x) => x.id !== defaults.id)}
          childAreas={childrenOf(defaults)}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={(data) => handleSave(data, index)}
        />
      ),
    });
  };

  // if parent is saved and a child of pixel type doesnt fit, remove parentId
  const detachRegionsLeftOutside = (parent: FlowAreaDto) => {
    if (parent.type !== FlowAreaTypeEnum.CUSTOM) return;

    const isOutside = (child: FlowAreaDto) =>
      child.locationX < 0 ||
      child.locationY < 0 ||
      child.locationX + child.width > parent.width ||
      child.locationY + child.height > parent.height;

    childrenOf(parent)
      .filter((x) => x.sizingMode === AreaSizingModeEnum.ABSOLUTE_PX)
      .filter(isOutside)
      .forEach((child) => {
        const childIndex = areas.findIndex((x) => x.id === child.id);
        if (childIndex === -1) return;

        update(
          childIndex,
          new FlowAreaDto({
            ...child,
            parentFlowAreaId: null,
            // Nesting is one level deep, so a parent is always top level and its own location is
            // already absolute. Adding it keeps the region over the same pixels.
            locationX: child.locationX + parent.locationX,
            locationY: child.locationY + parent.locationY,
          }),
        );
      });
  };

  const handleSave = (data: FlowAreaDto, index?: number) => {
    closeAll();
    if (index !== undefined) {
      update(index, data);
      detachRegionsLeftOutside(data);
    } else {
      append(data);
    }
  };

  const handleDelete = (row: FlowAreaDto) => {
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

  const typeBodyTemplate = (row: FlowAreaDto) => {
    const map = {
      [FlowAreaTypeEnum.CUSTOM]: { label: "Region", severity: "info" },
      [FlowAreaTypeEnum.APPLICATION]: { label: "Application", severity: "success" },
      [FlowAreaTypeEnum.BROWSER_TAB]: { label: "Browser tab", severity: "warning" },
      [FlowAreaTypeEnum.MONITOR]: { label: "Monitor", severity: "warning" },
    } as const;

    const tag = map[row.type] ?? map[FlowAreaTypeEnum.CUSTOM];
    return (
      <Tag
        value={tag.label}
        severity={tag.severity}
      />
    );
  };

  const detailsBodyTemplate = (row: FlowAreaDto) => {
    if (row.type === FlowAreaTypeEnum.CUSTOM) {
      return row.sizingMode === AreaSizingModeEnum.RATIO
        ? `${Math.round(row.ratioWidth * 100)}% × ${Math.round(row.ratioHeight * 100)}%`
        : `${row.locationX}, ${row.locationY} (${row.width}×${row.height})`;
    }
    if (row.type === FlowAreaTypeEnum.MONITOR) return row.monitorUniqueId || "-";
    if (row.type === FlowAreaTypeEnum.BROWSER_TAB)
      return row.tabMatchValue || row.processName || "-";

    return row.processName || row.titlePattern || "-";
  };

  const buildColumns = (
    isChildTable: boolean,
  ): DataTableColumnDto<FlowAreaDto>[] => [
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
  const rowExpansionTemplate = (parent: FlowAreaDto) => (
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
                new FlowAreaDto({
                  id: nextTempId(),
                  type: FlowAreaTypeEnum.CUSTOM,
                  parentFlowAreaId: parent.id,
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
              openEditor("ADD", new FlowAreaDto({ id: nextTempId() }))
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
        isRowExpandable={(row) => row.type !== FlowAreaTypeEnum.CUSTOM}
        emptyMessage="No areas defined yet."
      />
    </div>
  );
}
