import type { FlowDto } from "@/shared/models/flow/flow-dto";
import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { FlowActionsMenuComponent } from "./FlowActionsMenuComponent";

type Props = {
  flows: FlowDto[];
  onEdit: (id: number) => void;
  onDelete: (id: number) => void;
  onClone: (id: number) => void;
};

export function FlowDataTableComponent({
  flows,
  onEdit,
  onDelete,
  onClone,
}: Props) {
  return (
    <DataTable
      value={flows}
      paginator
      rows={10}
      stripedRows
    >
      <Column
        field="name"
        header="Name"
        sortable
      />
      <Column
        field="orderNumber"
        header="Order"
        sortable
      />
      <Column
        header="Actions"
        body={(row: FlowDto) => (
          <FlowActionsMenuComponent
            flowId={row.id}
            onEdit={onEdit}
            onClone={onClone}
            onDelete={onDelete}
          />
        )}
      />
    </DataTable>
  );
}
