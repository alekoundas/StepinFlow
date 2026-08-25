import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { SearchModeEnum } from "@/shared/enums/backend/search-mode-enum";
import { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import type { TreeNodeDetailDto, TreeNodeDto } from "@/shared/models/tree-node-dto";

export interface TreeNodeChip {
  text: string;
  isMuted?: boolean;
}

export interface FlowStepTreeDetail {
  // One short line under the name. Truncates before the chips do.
  text?: string;
  chips: TreeNodeChip[];
}

const EMPTY: FlowStepTreeDetail = { chips: [] };

const duration = (milliseconds: number): string =>
  milliseconds >= 1000
    ? `${(milliseconds / 1000).toFixed(milliseconds % 1000 === 0 ? 0 : 1)}s`
    : `${milliseconds}ms`;

const truncate = (text: string, max = 40): string =>
  text.length > max ? `${text.slice(0, max)}...` : text;

const readable = (value?: string | null): string =>
  (value ?? "").replaceAll("_", " ").toLowerCase();

/** Where a cursor step's point comes from, in the words the form uses. */
const pointName = (
  isCustom: boolean,
  pointName?: string | null,
  referenceStepName?: string | null,
): string =>
  isCustom
    ? (pointName ?? "no point")
    : referenceStepName
      ? `result of ${referenceStepName}`
      : "no source";

/** What a Check Value row compares against, which is nothing at all for IS_EMPTY. */
const conditionValue = (detail: TreeNodeDetailDto): string => {
  if (!detail.conditionText) return "";

  return detail.conditionTextEnd
    ? `"${truncate(detail.conditionText, 12)}" and "${truncate(detail.conditionTextEnd, 12)}"`
    : `"${truncate(detail.conditionText, 24)}"`;
};

const searchChips = (detail: TreeNodeDetailDto): TreeNodeChip[] =>
  detail.searchMode && detail.searchMode !== SearchModeEnum.FIND_BEST
    ? [{ text: readable(detail.searchMode), isMuted: true }]
    : [];

/**
 * What a row says besides its name. The tree is for navigating, so every type gets one line and
 * a chip or two, never a summary of the whole form.
 */
export const buildFlowStepTreeDetail = (node: TreeNodeDto): FlowStepTreeDetail => {
  const detail = node.detail;
  if (!detail) return EMPTY;

  switch (node.flowStepType) {
    case FlowStepTypeEnum.WAIT:
      return {
        chips: [
          {
            // A random wait shows its range: the minimum alone would read as the whole story.
            text: detail.waitForMillisecondsMax
              ? `${duration(detail.waitForMilliseconds)} - ${duration(detail.waitForMillisecondsMax)}`
              : duration(detail.waitForMilliseconds),
          },
        ],
      };

    case FlowStepTypeEnum.LOOP:
      return {
        chips: [
          { text: detail.isLoopInfinite ? "forever" : `x${detail.loopCount}` },
          { text: `${detail.childCount} inside`, isMuted: true },
        ],
      };

    case FlowStepTypeEnum.SUB_FLOW:
      return { text: detail.subFlowName ?? "no flow picked", chips: [] };

    case FlowStepTypeEnum.CURSOR_CLICK:
    case FlowStepTypeEnum.CURSOR_RELOCATE:
      return {
        text: pointName(
          detail.isPointCustom,
          detail.pointName,
          detail.referenceStepName,
        ),
        chips: detail.cursorButtonActionType
          ? [{ text: readable(detail.cursorButtonActionType) }]
          : [],
      };

    case FlowStepTypeEnum.CURSOR_DRAG:
      return {
        text: `${pointName(detail.isPointCustom, detail.pointName, detail.referenceStepName)} -> ${pointName(detail.isPointEndCustom, detail.pointEndName, detail.referenceStepEndName)}`,
        chips: [],
      };

    case FlowStepTypeEnum.CURSOR_SCROLL:
      return {
        chips: [
          { text: `${readable(detail.cursorScrollDirectionType)} ${detail.loopCount}` },
        ],
      };

    case FlowStepTypeEnum.KEYBOARD_INPUT:
      return {
        text: detail.keyboardInputText ? truncate(detail.keyboardInputText) : undefined,
        chips: detail.keyboardInputType
          ? [{ text: readable(detail.keyboardInputType), isMuted: true }]
          : [],
      };

    case FlowStepTypeEnum.WINDOW_FOCUS:
      return { text: detail.areaName ?? "no window picked", chips: [] };

    case FlowStepTypeEnum.WINDOW_RESIZE:
      return {
        text: detail.areaName ?? "no window picked",
        chips: [{ text: `${detail.windowWidth}x${detail.windowHeight}` }],
      };

    case FlowStepTypeEnum.WINDOW_RELOCATE:
      return {
        text: `${detail.areaName ?? "no window picked"} -> ${detail.pointName ?? "no point"}`,
        chips: [],
      };

    case FlowStepTypeEnum.IMAGE_SEARCH:
      return {
        text: detail.areaName ?? "no area picked",
        chips: [
          {
            text:
              detail.templateCount === 1
                ? "1 template"
                : `${detail.templateCount} templates`,
            isMuted: detail.templateCount === 0,
          },
          ...searchChips(detail),
        ],
      };

    case FlowStepTypeEnum.READ_TEXT:
      return {
        text: `${detail.areaName ?? "no area picked"} · "${truncate(detail.conditionText ?? "", 24)}"`,
        chips: searchChips(detail),
      };

    case FlowStepTypeEnum.SYSTEM_COMMAND: {
      const isCustom = detail.runCommandPreset === RunCommandPresetEnum.CUSTOM;

      return {
        text: isCustom
          ? truncate(detail.runCommand ?? "")
          : readable(detail.runCommandPreset),
        chips: isCustom
          ? [{ text: readable(detail.runCommandShell), isMuted: true }]
          : [],
      };
    }

    case FlowStepTypeEnum.SYSTEM_ACTION:
      return { text: readable(detail.systemActionType), chips: [] };

    case FlowStepTypeEnum.CHECK_VALUE:
      return {
        text: `${detail.referenceStepName ?? "no step picked"} ${readable(detail.conditionType)} ${conditionValue(detail)}`,
        chips: [],
      };

    case FlowStepTypeEnum.SUCCESS:
    case FlowStepTypeEnum.FAILURE:
      return {
        chips: [{ text: `${detail.childCount}`, isMuted: true }],
      };

    default:
      return EMPTY;
  }
};
