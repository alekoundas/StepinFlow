import { DataGridComponent } from "@/shared/components/DataGridComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { Card } from "primereact/card";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { backendApiService } from "@/services/backend-api-service";

interface FlowStepType {
  name: string;
  description: string;
  iconName: string;
  flowStepType: FlowStepTypeEnum;
}

interface Props {
  className?: string;
}

export function FlowStepTypesDataGridComponent({ className }: Props) {
  const { setSelectedFlowStepTypeToAdd } = useWorkflowStore();
  const loadData: LazyResponseDto<FlowStepType> = {
    data: [
      {
        name: "Cursor Drag&Drop",
        description:
          "Simulate cursor drag and drop behavior. Coordinates can be set by a custom value or by using the result coordinates of an 'Image Search'",
        iconName: "",
        flowStepType: FlowStepTypeEnum.CURSOR_DRAG,
      },
      {
        name: "Cursor Click",
        description: "Simulate a cursor click action.",
        iconName: "",
        flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
      },
      {
        name: "Cursor Relocate",
        description:
          "Move the currsor to the specific coordinates. Coordinates can be set by a custom value or by using the result coordinates of an 'Image Search'",
        iconName: "",
        flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
      },

      {
        name: "Cursor scroll",
        description: "Simulate a currsor scroll behavior.",
        iconName: "",
        flowStepType: FlowStepTypeEnum.CURSOR_SCROLL,
      },

      {
        name: "Window Relocate",
        description:
          "Preset an application Window name. If application Window exists, the preselected Window will be moved to the new coordinates. If application Window does not exist, move action will not be executed and 'Failure' children will be executed (If any is set).",
        iconName: "",
        flowStepType: FlowStepTypeEnum.WINDOW_RELOCATE,
      },

      {
        name: "Window Resize",
        description:
          "Preset an application Window name. If application Window exists, the preselected Window will be resized to the new coordinates. If application Window does not exist, move action will not be executed and 'Failure' children will be executed (If any is set).",
        iconName: "",
        flowStepType: FlowStepTypeEnum.WINDOW_RESIZE,
      },

      {
        name: "Window Focus",
        description:
          "Preset an application Window name. If application Window exists, the preselected Window will be focused. If application Window does not exist, move action will not be executed and 'Failure' children will be executed (If any is set).",
        iconName: "",
        flowStepType: FlowStepTypeEnum.WINDOW_FOCUS,
      },
      {
        name: "Loop",
        description: "",
        iconName: "",
        flowStepType: FlowStepTypeEnum.LOOP,
      },
      {
        name: "Wait",
        description: "",
        iconName: "",
        flowStepType: FlowStepTypeEnum.WAIT,
      },
    ],
    totalRecords: 10,
  };
  const cardTemplate = (item: FlowStepType) => (
    <Card
      key={item.name}
      className="w-full h-full border-round-2xl shadow-2 transition-all hover:shadow-4 flex flex-column"
      onClick={() => setSelectedFlowStepTypeToAdd(item.flowStepType)}
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
      <DataGridComponent<FlowStepType>
        queryKey={["flowSteps", "list"]}
        queryFn={() => new Promise((resolve) => resolve(loadData))}
        itemTemplate={cardTemplate}
      />
    </div>
  );
}
