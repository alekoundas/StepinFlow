import type { FlowDto } from "@/shared/models/database/flow/flow-dto";
import { DataGridComponent } from "@/shared/components/DataGridComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { Card } from "primereact/card";
import { useNavigate } from "react-router-dom";
import { backendApiService } from "@/shared/services/backend-api-service";

type Props = {
  className?: string;
};

export function FlowDataGridComponent({ className }: Props) {
  const navigate = useNavigate();

  const onClick = (id: number) => navigate(`/workflow/${id}`);
  const cardTemplate = (item: FlowDto) => (
    <Card
      key={item.id}
      className="w-full h-full border-round-2xl shadow-2 transition-all hover:shadow-4 flex flex-column"
      onClick={() => onClick(item.id)}
    >
      <div className="flex flex-wrap align-items-center justify-content-between gap-2">
        <div className="flex align-items-center gap-2">
          <i className="pi pi-tag"></i>
          <span className="font-semibold">{item.name}</span>
        </div>
        <LabelComponent text={item.name} />
      </div>
      <div className="flex flex-column align-items-center gap-3 py-5">
        <div className="text-2xl font-bold">{item.name}</div>
        <LabelComponent text={item.name} />
      </div>
      <div className="flex align-items-center justify-content-between">
        <span className="text-2xl font-semibold">${item.name}</span>
        <LabelComponent text={item.name} />
      </div>
    </Card>
  );

  return (
    <div className={className}>
      <DataGridComponent<FlowDto>
        queryKey={["flows", "list"]}
        queryFn={backendApiService.Flow.getLazy}
        itemTemplate={cardTemplate}
        enablePaging={true}
      />
    </div>
  );
}
