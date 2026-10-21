import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";

/**
 * Steps grouped by what they do to the machine, which is what the tree colours by. Control is
 * deliberately neutral: WAIT and LOOP are the most common steps in any flow, and colouring them
 * would drown out the steps that actually act.
 *
 * This is the per type source the tree reads. The "add a step" picker keeps its own list because
 * it offers entry points rather than types: one Cursor card covers all four cursor types.
 */
export const FlowStepGroupEnum = {
  CONTROL: "CONTROL",
  INPUT: "INPUT",
  WINDOW: "WINDOW",
  PERCEPTION: "PERCEPTION",
  SYSTEM: "SYSTEM",
  BRANCH: "BRANCH",
} as const;

export type FlowStepGroupEnum =
  (typeof FlowStepGroupEnum)[keyof typeof FlowStepGroupEnum];

export interface FlowStepCatalogEntry {
  flowStepType: FlowStepTypeEnum;
  group: FlowStepGroupEnum;
  label: string;
  iconName: string;
}

export const FLOW_STEP_CATALOG: FlowStepCatalogEntry[] = [
  // ── Control ──
  {
    flowStepType: FlowStepTypeEnum.WAIT,
    group: FlowStepGroupEnum.CONTROL,
    label: "Wait",
    iconName: "clock",
  },
  {
    flowStepType: FlowStepTypeEnum.LOOP,
    group: FlowStepGroupEnum.CONTROL,
    label: "Loop",
    iconName: "refresh",
  },
  {
    flowStepType: FlowStepTypeEnum.GO_BACK,
    group: FlowStepGroupEnum.CONTROL,
    label: "Go Back",
    iconName: "arrow-right-arrow-left",
  },
  {
    flowStepType: FlowStepTypeEnum.SUB_FLOW,
    group: FlowStepGroupEnum.CONTROL,
    label: "Sub-Flow",
    iconName: "sitemap",
  },
  {
    flowStepType: FlowStepTypeEnum.CHECK_VALUE,
    group: FlowStepGroupEnum.CONTROL,
    label: "Check Value",
    iconName: "filter",
  },
  {
    flowStepType: FlowStepTypeEnum.END_EXECUTION,
    group: FlowStepGroupEnum.CONTROL,
    label: "End Execution",
    iconName: "stop-circle",
  },
  {
    flowStepType: FlowStepTypeEnum.STAGE_MARKER,
    group: FlowStepGroupEnum.CONTROL,
    label: "Stage Marker",
    iconName: "bookmark",
  },

  // ── Input ──
  {
    flowStepType: FlowStepTypeEnum.CURSOR_CLICK,
    group: FlowStepGroupEnum.INPUT,
    label: "Cursor Click",
    iconName: "bullseye",
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_DRAG,
    group: FlowStepGroupEnum.INPUT,
    label: "Cursor Drag & Drop",
    iconName: "arrows-alt",
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_SCROLL,
    group: FlowStepGroupEnum.INPUT,
    label: "Cursor Scroll",
    iconName: "sort-alt",
  },
  {
    flowStepType: FlowStepTypeEnum.CURSOR_RELOCATE,
    group: FlowStepGroupEnum.INPUT,
    label: "Cursor Relocate",
    iconName: "map-marker",
  },
  {
    flowStepType: FlowStepTypeEnum.KEYBOARD_INPUT,
    group: FlowStepGroupEnum.INPUT,
    label: "Keyboard Input",
    iconName: "pencil",
  },

  // ── Window ──
  {
    flowStepType: FlowStepTypeEnum.WINDOW_FOCUS,
    group: FlowStepGroupEnum.WINDOW,
    label: "Window Focus",
    iconName: "window-maximize",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_RESIZE,
    group: FlowStepGroupEnum.WINDOW,
    label: "Window Resize",
    iconName: "expand",
  },
  {
    flowStepType: FlowStepTypeEnum.WINDOW_RELOCATE,
    group: FlowStepGroupEnum.WINDOW,
    label: "Window Relocate",
    iconName: "directions",
  },

  // ── Perception ──
  {
    flowStepType: FlowStepTypeEnum.SEARCH_IMAGE,
    group: FlowStepGroupEnum.PERCEPTION,
    label: "Search Image",
    iconName: "search",
  },
  {
    flowStepType: FlowStepTypeEnum.SEARCH_TEXT,
    group: FlowStepGroupEnum.PERCEPTION,
    label: "Search Text",
    iconName: "file-edit",
  },

  // ── System ──
  {
    flowStepType: FlowStepTypeEnum.SYSTEM_COMMAND,
    group: FlowStepGroupEnum.SYSTEM,
    label: "System Command",
    iconName: "code",
  },
  {
    flowStepType: FlowStepTypeEnum.SYSTEM_ACTION,
    group: FlowStepGroupEnum.SYSTEM,
    label: "System Action",
    iconName: "power-off",
  },
  {
    flowStepType: FlowStepTypeEnum.NOTIFY,
    group: FlowStepGroupEnum.SYSTEM,
    label: "Notify",
    iconName: "send",
  },

  // ── Branches ── structural, created with their parent
  {
    flowStepType: FlowStepTypeEnum.SUCCESS,
    group: FlowStepGroupEnum.BRANCH,
    label: "Success",
    iconName: "check",
  },
  {
    flowStepType: FlowStepTypeEnum.FAILURE,
    group: FlowStepGroupEnum.BRANCH,
    label: "Failure",
    iconName: "times",
  },
];

export const getFlowStepCatalogEntry = (
  flowStepType: FlowStepTypeEnum | undefined,
): FlowStepCatalogEntry | undefined =>
  FLOW_STEP_CATALOG.find((x) => x.flowStepType === flowStepType);

/**
 * A colour per group, so a glance says what kind of thing a step does, in the tree and on its form.
 *
 * Only the accent is picked here and the tints are mixed from it. PrimeFlex's bg-{colour}-50
 * classes cannot be used: the palette is not inverted for dark themes, so bg-blue-50 is near
 * white on a dark theme. Mixing into transparent tints whatever surface is actually behind, which
 * lands correctly on either.
 *
 * No group uses green or red. Those mean Success and Failure.
 */
const GROUP_ACCENTS: Record<FlowStepGroupEnum, string> = {
  CONTROL: "var(--bluegray-400)",
  INPUT: "var(--blue-400)",
  WINDOW: "var(--purple-400)",
  PERCEPTION: "var(--orange-400)",
  SYSTEM: "var(--teal-400)",
  BRANCH: "var(--bluegray-400)",
};

/** The two branches carry meaning rather than a category, so they get the semantic colours. */
const TYPE_ACCENTS: Partial<Record<FlowStepTypeEnum, string>> = {
  [FlowStepTypeEnum.SUCCESS]: "var(--green-400)",
  [FlowStepTypeEnum.FAILURE]: "var(--red-400)",
};

export const flowStepAccent = (flowStepType: FlowStepTypeEnum | undefined): string =>
  (flowStepType && TYPE_ACCENTS[flowStepType]) ??
  GROUP_ACCENTS[getFlowStepCatalogEntry(flowStepType)?.group ?? FlowStepGroupEnum.CONTROL];

export const tint = (accent: string, percent: number): string =>
  `color-mix(in srgb, ${accent} ${percent}%, transparent)`;

/**
 * Types created with a Success and a Failure child. Mirrors TreeStepHelper.BranchTypes - only these
 * route on their outcome, so only these are worth reporting one for.
 */
const BRANCH_STEP_TYPES: FlowStepTypeEnum[] = [
  FlowStepTypeEnum.SEARCH_IMAGE,
  FlowStepTypeEnum.SEARCH_TEXT,
  FlowStepTypeEnum.CHECK_VALUE,
  FlowStepTypeEnum.SYSTEM_COMMAND,
  FlowStepTypeEnum.WINDOW_FOCUS,
  FlowStepTypeEnum.WINDOW_RESIZE,
  FlowStepTypeEnum.WINDOW_RELOCATE,
];

export const hasBranches = (flowStepType: FlowStepTypeEnum | undefined): boolean =>
  !!flowStepType && BRANCH_STEP_TYPES.includes(flowStepType);
