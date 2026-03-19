import { Card } from "primereact/card";
import { FlowActionsMenuComponent } from "./FlowActionsMenuComponent";
import type { FlowDto } from "@/shared/models/flow/flow-dto";

type Props = {
  flows: FlowDto[];
  onEdit: (id: number) => void;
  onClone: (id: number) => void;
  onDelete: (id: number) => void;
};

export function FlowCardListComponent({
  flows,
  onEdit,
  onClone,
  onDelete,
}: Props) {
  return (
    <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
      {flows.map((flow) => (
        <Card
          key={flow.id}
          title={flow.name}
          subTitle={`Order: ${flow.orderNumber}`}
        >
          <FlowActionsMenuComponent
            flowId={flow.id}
            onEdit={onEdit}
            onClone={onClone}
            onDelete={onDelete}
          />
        </Card>
      ))}
    </div>
  );
}
