import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import type { DraftStepDto } from "@/shared/models/database/flow-draft-dto";
import { TreeNodeDto, type TreeNodeDetailDto } from "@/shared/models/tree-node-dto";

/** Types the save gives a Success and a Failure child to, mirroring TreeStepHelper. */
const BRANCHING_TYPES: FlowStepTypeEnum[] = [
  FlowStepTypeEnum.IMAGE_SEARCH,
  FlowStepTypeEnum.READ_TEXT,
  FlowStepTypeEnum.SYSTEM_COMMAND,
  FlowStepTypeEnum.CHECK_VALUE,
];

/**
 * Confirmed steps as tree nodes, drawn by the same row component the real tree uses, so a
 * pending step looks exactly like the step it is about to become.
 *
 * The Success and Failure rows are synthesised here because the save creates them and a child
 * needs somewhere visible to sit. Nothing else in the draft knows they exist.
 */
export const buildDraftTree = (
  steps: DraftStepDto[],
  pending?: { parentTempId?: number; parentBranch?: FlowStepTypeEnum; name: string },
): TreeNodeDto[] => {
  const nodeByTempId = new Map<number, TreeNodeDto>();
  const branchByKey = new Map<string, TreeNodeDto>();
  const roots: TreeNodeDto[] = [];

  const branchKey = (tempId: number, branch: FlowStepTypeEnum) => `${tempId}-${branch}`;

  const attach = (
    node: TreeNodeDto,
    parentTempId?: number | null,
    parentBranch?: FlowStepTypeEnum | null,
  ) => {
    const parent = parentTempId != null ? nodeByTempId.get(parentTempId) : undefined;

    if (!parent) {
      roots.push(node);
      return;
    }

    if (parentBranch == null) {
      parent.children.push(node);
      parent.leaf = false;
      return;
    }

    const branch = branchByKey.get(branchKey(parentTempId!, parentBranch));
    if (branch) {
      branch.children.push(node);
      branch.leaf = false;
    } else {
      parent.children.push(node);
      parent.leaf = false;
    }
  };

  for (const step of steps) {
    const node = new TreeNodeDto({
      key: `draft-${step.tempId}`,
      entityId: step.tempId,
      name: step.values.name,
      flowStepType: step.values.flowStepType,
      isFlow: false,
      isNew: false,
      leaf: true,
      draggable: false,
      droppable: false,
      selectable: false,
      detail: buildDetail(step),
      children: [],
    });

    nodeByTempId.set(step.tempId, node);
    attach(node, step.parentTempId, step.parentBranch);

    if (BRANCHING_TYPES.includes(step.values.flowStepType)) {
      node.leaf = false;

      for (const branch of [FlowStepTypeEnum.SUCCESS, FlowStepTypeEnum.FAILURE]) {
        const branchNode = new TreeNodeDto({
          key: `draft-${step.tempId}-${branch}`,
          entityId: -1,
          name: branch === FlowStepTypeEnum.SUCCESS ? "Success" : "Failure",
          flowStepType: branch,
          isFlow: false,
          isNew: false,
          leaf: true,
          draggable: false,
          droppable: false,
          selectable: false,
          children: [],
        });

        branchByKey.set(branchKey(step.tempId, branch), branchNode);
        node.children.push(branchNode);
      }
    }
  }

  // A dashed placeholder for the action being decided, so the shape is visible before it is
  // committed rather than after.
  if (pending) {
    const node = new TreeNodeDto({
      key: "draft-pending",
      entityId: -2,
      name: pending.name,
      isFlow: false,
      isNew: true,
      leaf: true,
      draggable: false,
      droppable: false,
      selectable: false,
      children: [],
    });

    attach(node, pending.parentTempId, pending.parentBranch);
  }

  return roots;
};

/** The few fields a row reads, taken off the step the draft is proposing. */
const buildDetail = (step: DraftStepDto): TreeNodeDetailDto => {
  const values = step.values;

  return {
    waitForMilliseconds: values.waitForMilliseconds,
    waitForMillisecondsMax: 0,
    loopCount: values.loopCount,
    isLoopInfinite: values.isLoopInfinite,

    areaName: null,
    // A point the save is going to create has no name yet, so the row says where it goes.
    pointName: step.newPoint ? step.newPoint.name : null,
    pointEndName: step.newPointEnd ? step.newPointEnd.name : null,
    referenceStepName: step.referenceTempId != null ? "the search above" : null,
    referenceStepEndName: null,
    subFlowName: null,

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
    runCommandValue: values.runCommandValue,

    systemActionType: values.systemActionType,

    childCount: 0,
  };
};
