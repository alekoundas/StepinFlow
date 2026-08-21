import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { DraftStepDto } from "@/shared/models/database/flow-draft-dto";
import { TreeNodeDto, type TreeNodeDetailDto } from "@/shared/models/tree-node-dto";

/**
 * Draft steps as tree nodes, so the preview is drawn by the same row component the real tree
 * uses. A pending step then looks exactly like the step it is about to become, which is the
 * whole point of showing a preview rather than a list of names.
 */
export const buildDraftTree = (steps: DraftStepDto[]): TreeNodeDto[] => {
  const nodeByTempId = new Map<number, TreeNodeDto>();

  const nodes = steps.map((step) => {
    const node = new TreeNodeDto({
      key: `draft-${step.tempId}`,
      entityId: step.tempId,
      name: step.values.name,
      flowStepType: step.values.flowStepType,
      orderNumber: 0,
      isFlow: false,
      isNew: false,
      leaf: true,
      draggable: false,
      droppable: false,
      selectable: true,
      detail: buildDetail(step),
      children: [],
    });

    nodeByTempId.set(step.tempId, node);
    return node;
  });

  const roots: TreeNodeDto[] = [];

  steps.forEach((step, index) => {
    const node = nodes[index];
    const parent = step.parentTempId != null ? nodeByTempId.get(step.parentTempId) : undefined;

    if (parent) {
      parent.children.push(node);
      parent.leaf = false;
    } else {
      roots.push(node);
    }
  });

  return roots;
};

/** The few fields a row reads, taken off the step the draft is proposing. */
const buildDetail = (step: DraftStepDto): TreeNodeDetailDto => {
  const values = step.values;

  return {
    waitForMilliseconds: values.waitForMilliseconds,
    loopCount: values.loopCount,
    isLoopInfinite: values.isLoopInfinite,

    areaName: null,
    // A point the save is going to create has no name yet, so the row says where it goes.
    pointName: step.newPoint ? step.newPoint.name : null,
    pointEndName: step.newPointEnd ? step.newPointEnd.name : null,
    referenceStepName: null,
    referenceStepEndName: null,
    subFlowName: null,

    isPointCustom: values.isPointCustom,
    isPointEndCustom: values.isPointEndCustom,

    cursorButtonType: values.cursorButtonType,
    cursorButtonActionType: values.cursorButtonActionType,
    cursorScrollDirectionType: values.cursorScrollDirectionType,

    keyboardInputText: values.keyboardInputText,
    keyboardInputType: values.keyboardInputType,

    windowWidth: values.windowWidth,
    windowHeight: values.windowHeight,

    searchMode: values.searchMode,
    templateCount: values.flowStepImages.length,
    thumbnail: null,

    conditionText: values.conditionText,
    conditionTextEnd: values.conditionTextEnd,
    conditionType: values.conditionType,

    runCommandShell: values.runCommandShell,
    runCommandPreset: values.runCommandPreset,
    runCommand: values.runCommand,

    systemActionType: values.systemActionType,

    childCount: 0,
  };
};

/** Branch steps get their Success and Failure children on save, which the preview should show. */
export const BRANCHING_TYPES: FlowStepTypeEnum[] = [
  FlowStepTypeEnum.IMAGE_SEARCH,
  FlowStepTypeEnum.READ_TEXT,
  FlowStepTypeEnum.SYSTEM_COMMAND,
  FlowStepTypeEnum.CHECK_VALUE,
];
