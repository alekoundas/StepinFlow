import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { FlowActionsMenuComponent } from "./FlowActionsMenuComponent";
import { Flow } from "../../../models/dto/flow";

type Props = {
  flows: Flow[];
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
        body={(row: Flow) => (
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
