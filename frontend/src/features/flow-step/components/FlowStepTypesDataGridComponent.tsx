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
        "Repeat a set of child steps a specified number of times or until a condition is met.",
      iconName: "refresh",
    },
    {
      flowStepType: FlowStepTypeEnum.GO_TO,
      name: "Go To",
      description: "Jump execution to another step within the current flow.",
      iconName: "arrow-right-arrow-left",
    },
    {
      flowStepType: FlowStepTypeEnum.RUN_CMD,
      name: "Run Command",
      description:
        "Execute a shell command or script on the host machine and optionally capture its output.",
      iconName: "terminal",
    },
    {
      flowStepType: FlowStepTypeEnum.SUB_FLOW,
      name: "Sub-Flow",
      description:
        "Invoke another saved flow as a reusable subroutine within this flow.",
      iconName: "sitemap",
    },
    // {
    //   flowStepType: FlowStepTypeEnum.VARIABLE_CONDITION,
    //   name: "Variable Condition",
    //   description:
    //     "Branch execution based on whether a variable satisfies a defined condition (equals, contains, greater than, etc.).",
    //   iconName: "pi-code",
    // },
    // {
    //   flowStepType: FlowStepTypeEnum.NOTIFICATION_EMAIL,
    //   name: "Email Notification",
    //   description:
    //     "Send an email notification with a configurable recipient, subject, and body.",
    //   iconName: "pi-envelope",
    // },

    // ── Cursor ──
    {
      flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
      name: "Cursor Click",
      description:
        "Simulate a left, right, or double mouse click at the specified screen coordinates.",
      iconName: "mouse",
    },
    {
      flowStepType: FlowStepTypeEnum.CURSOR_DRAG,
      name: "Cursor Drag & Drop",
      description:
        "Click and hold at a source position, drag to a target position, then release. Coordinates can be the result of an Image Search result.",
      iconName: "arrows-alt",
    },
    {
      flowStepType: FlowStepTypeEnum.CURSOR_SCROLL,
      name: "Cursor Scroll",
      description:
        "Simulate a mouse wheel scroll (up or down) by a specified amount at the current cursor position.",
      iconName: "sort-alt",
    },
    {
      flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
      name: "Cursor Relocate",
      description:
        "Move the cursor to specific coordinates without clicking. Coordinates can come from an Image Search result.",
      iconName: "map-marker",
    },

    // ── Window ──
    {
      flowStepType: FlowStepTypeEnum.WINDOW_FOCUS,
      name: "Window Focus",
      description:
        "Bring a named application window to the foreground. Runs Failure children if the window is not found.",
      iconName: "window-maximize",
    },
    {
      flowStepType: FlowStepTypeEnum.WINDOW_RESIZE,
      name: "Window Resize",
      description:
        "Resize a named application window to the specified dimensions. Runs Failure children if the window is not found.",
      iconName: "expand",
    },
    {
      flowStepType: FlowStepTypeEnum.WINDOW_RELOCATE,
      name: "Window Relocate",
      description:
        "Move a named application window to the specified screen position. Runs Failure children if the window is not found.",
      iconName: "directions",
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
      flowStepType: FlowStepTypeEnum.TEXT_SEARCH,
      name: "Text Search",
      description:
        "Search the screen for a text string using OCR and return its bounding-box coordinates.",
      iconName: "file-find",
    },

    // ── Control-flow children ──
    // {
    //   flowStepType: FlowStepTypeEnum.SUCCESS,
    //   name: "Success",
    //   description:
    //     "Child steps executed when the parent step completes successfully.",
    //   iconName: "pi-check-circle",
    // },
    // {
    //   flowStepType: FlowStepTypeEnum.FAILURE,
    //   name: "Failure",
    //   description:
    //     "Child steps executed when the parent step fails or its condition is not met.",
    //   iconName: "pi-times-circle",
    // },
  ];
  const loadData: LazyResponseDto<FlowStepType> = {
    data: flowStepTypes,
    totalRecords: 10,
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
      <p
        className="text-color-secondary m-0"
        style={{ fontSize: "0.78rem", lineHeight: 1.4 }}
      >
        {item.description}
      </p>
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
