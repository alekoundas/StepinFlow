import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import { type FieldArrayWithId } from "react-hook-form";

import { Button } from "primereact/button";
import { Tag } from "primereact/tag";

import { LocalDataTableComponent } from "@/shared/components/data/LocalDataTableComponent";
import { ActionsMenuComponent } from "@/shared/components/ActionsMenuComponent";
import { UsageCountTagComponent } from "@/shared/components/UsageCountTagComponent";
import { FlowLocationDto } from "@/shared/models/database/flow-location-dto";
import type { FlowSearchAreaDto } from "@/shared/models/database/flow-search-area-dto";
import { AreaSizingModeEnum } from "@/shared/enums/backend/area/area-sizing-mode-enum";

import FlowLocationFormComponent from "@/features/flow-location/components/forms/FlowLocationFormComponent";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import type { FlowSchema } from "@/features/flow/components/form/flow.zod";
import type z from "zod";
import type { FormMode } from "@/shared/enums/form-mode-enum";

interface Props {
  fields: FieldArrayWithId<z.infer<typeof FlowSchema>, "flowLocations", "fieldId">[];
  append: (item: FlowLocationDto) => void;
  remove: (index: number) => void;
  update: (index: number, value: FlowLocationDto) => void;
  areaOptions: FlowSearchAreaDto[];
  formMode: FormMode;
  isDisabled?: boolean;
}

const FORM_ID = "flow-location-form";

export function FlowLocationDataTableComponent({
  fields,
  append,
  remove,
  update,
  areaOptions,
  formMode,
  isDisabled = false,
}: Props) {
  const { openForm, closeAll } = useDialogStore();

  const locations = fields as unknown as FlowLocationDto[];

  const openEditor = (
    mode: FormMode,
    defaults: FlowLocationDto,
    index?: number,
  ) => {
    openForm(FORM_ID, {
      headerText: mode === "ADD" ? "Add Location" : "Edit Location",
      formId: FORM_ID,
      children: (
        <FlowLocationFormComponent
          defaultValues={defaults}
          formId={FORM_ID}
          isFormInDialog={true}
          formMode={mode}
          areaOptions={areaOptions}
          onEdit={() => closeAll()}
          onCancel={() => closeAll()}
          onSubmit={(data) => handleSave(data, index)}
        />
      ),
    });
  };

  const handleSave = (data: FlowLocationDto, index?: number) => {
    closeAll();
    if (index !== undefined) {
      update(index, data);
    } else {
      append(data);
    }
  };

  const handleDelete = (index: number) => {
    const location = locations[index];

    if (location.flowStepsCount > 0) {
      const message =
        `"${location.name}" is used by ${location.flowStepsCount} step(s). ` +
        `Deleting it clears their location. Continue?`;
      if (!confirm(message)) return;
    } else if (!confirm("Delete this location?")) {
      return;
    }

    remove(index);
  };

  // Absolute points are the ones an import has to ask about, so they are called out here.
  const frameBodyTemplate = (row: FlowLocationDto) => {
    const areaName =
      row.flowSearchAreaName ||
      areaOptions.find((x) => x.id === row.flowSearchAreaId)?.name;

    return areaName ? (
      <Tag
        value={areaName}
        severity="info"
      />
    ) : (
      <Tag
        value="Whole screen"
        severity="warning"
      />
    );
  };

  const columns: DataTableColumnDto<FlowLocationDto>[] = [
    { field: "name", header: "Name", sortable: true },
    { field: "frame", header: "Measured from", body: frameBodyTemplate },
    {
      field: "point",
      header: "Point",
      body: (row) =>
        row.offsetMode === AreaSizingModeEnum.RATIO && row.flowSearchAreaId
          ? `${Math.round(row.ratioX * 100)}%, ${Math.round(row.ratioY * 100)}%`
          : `${row.locationX}, ${row.locationY}`,
    },
    {
      field: "flowStepsCount",
      header: "Used By",
      sortable: true,
      body: (row) => <UsageCountTagComponent count={row.flowStepsCount} />,
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
        <h3 className="text-lg font-medium">Locations</h3>
        {!isDisabled && (
          <Button
            type="button"
            label="Add Location"
            icon="pi pi-plus"
            onClick={() =>
              openEditor(
                "ADD",
                new FlowLocationDto({
                  id: Math.min(0, ...locations.map((x) => x.id)) - 1,
                }),
              )
            }
            size="small"
          />
        )}
      </div>

      <LocalDataTableComponent
        value={locations}
        columns={columns}
        emptyMessage="No locations defined yet."
      />
    </div>
  );
}
