import { DataGridComponent } from "@/shared/components/DataGridComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import { Card } from "primereact/card";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import IconComponent from "@/shared/components/IconComponent";

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

  const flowStepTypes: FlowStepType[] = [
    // ── System ──
    {
      flowStepType: FlowStepTypeEnum.WAIT,
      name: "Wait",
      description:
        "Pause execution for a specified duration before continuing to the next step.",
      iconName: "clock",
    },
    {
      flowStepType: FlowStepTypeEnum.LOOP,
      name: "Loop",
      description:
        "Repeat a set of child steps a specified number of times or for ever.",
      iconName: "refresh",
    },
    {
      flowStepType: FlowStepTypeEnum.GO_TO,
      name: "Go To",
      description: "Jump execution to another step within the current flow.",
      iconName: "arrow-right-arrow-left",
    },
    {
      flowStepType: FlowStepTypeEnum.SYSTEM_COMMAND,
      name: "System Command",
      description:
        "Run a command on this machine, branch on whether it worked, and use what it printed in later steps.",
      iconName: "terminal",
    },
    {
      flowStepType: FlowStepTypeEnum.SYSTEM_ACTION,
      name: "System Action",
      description:
        "Ask Windows to lock, sleep, or turn the screens off. No command, no output.",
      iconName: "power-off",
    },
    {
      flowStepType: FlowStepTypeEnum.SUB_FLOW,
      name: "Sub-Flow",
      description:
        "Invoke another saved flow as a reusable subroutine within this flow.",
      iconName: "sitemap",
    },
    {
      flowStepType: FlowStepTypeEnum.CHECK_VALUE,
      name: "Check Value",
      description:
        "Test what an earlier step read or printed, and branch on the answer.",
      iconName: "filter",
    },

    // ── Cursor ──
    {
      flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
      name: "Cursor",
      description:
        "Simulate a cursor action. Click, drag, scroll, or relocate the cursor to specific coordinates.",
      iconName: "mouse",
    },

    // ── Window ──
    {
      flowStepType: FlowStepTypeEnum.WINDOW_FOCUS,
      name: "Window",
      description:
        "Select a named application window to bring it to the foreground, resize it, or move it to a specific position on the screen.",
      iconName: "expand",
    },

    // ── Keyboard ──
    {
      flowStepType: FlowStepTypeEnum.KEYBOARD_INPUT,
      name: "Keyboard Input",
      description:
        "Type a string or send individual key combinations (e.g. Ctrl+C, Enter, Tab) to the active window.",
      iconName: "keyboard",
    },

    // ── Screen Search ──
    {
      flowStepType: FlowStepTypeEnum.IMAGE_SEARCH,
      name: "Image Search",
      description:
        "Search the screen for a template image and return its center coordinates. Result can be used by Cursor steps.",
      iconName: "search",
    },
    {
      flowStepType: FlowStepTypeEnum.READ_TEXT,
      name: "Read Text",
      description:
        "Read the text inside an area, branch on whether it matches, and hand it to later steps.",
      iconName: "file-edit",
    },
  ];

  const loadData: LazyResponseDto<FlowStepType> = {
    data: flowStepTypes,
    totalRecords: flowStepTypes.length,
  };
  const cardTemplate = (item: FlowStepType) => (
    <Card
      key={item.name}
      className="w-full h-full border-round-2xl shadow-2 transition-all hover:shadow-4 flex flex-column"
      onClick={() => setSelectedFlowStepTypeToAdd(item.flowStepType)}
    >
      <div className="flex align-items-center gap-2">
        <IconComponent name={item.iconName} />
        <LabelComponent
          text={item.name}
          weight="semibold"
          size="sm"
        />
      </div>
      <LabelComponent
        text={item.description}
        size="sm"
        className="mt-5"
      />
    </Card>
  );

  return (
    <div className={className}>
      <DataGridComponent<FlowStepType>
        queryKey={["flowStepTypes", "list"]}
        queryFn={() => new Promise((resolve) => resolve(loadData))}
        itemTemplate={cardTemplate}
      />
    </div>
  );
}
