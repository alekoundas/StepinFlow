import type { FlowDto } from "@/shared/models/flow/flow-dto";
import { DataGridComponent } from "@/shared/components/DataGridComponent";
import { backendApiService } from "@/services/backend-api-service";
import LabelComponent from "@/shared/components/LabelComponent";
import { Card } from "primereact/card";

type Props = {
  className?: string;
};

export function FlowDataGridComponent({ className }: Props) {
  const cardTemplate = (item: FlowDto) => (
    <Card
      key={item.id}
      className="w-full h-full border-round-2xl shadow-2 transition-all hover:shadow-4 flex flex-column"
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
        loadData={backendApiService.Flow.getLazy}
        itemTemplate={cardTemplate}
      />
    </div>
  );
}
