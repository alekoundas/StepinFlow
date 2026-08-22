import { useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { useQueryClient } from "@tanstack/react-query";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { Stepper } from "primereact/stepper";
import { StepperPanel } from "primereact/stepperpanel";
import { Tree } from "primereact/tree";
import type { TreeNode } from "primereact/treenode";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepTreeNodeComponent } from "@/features/flow-step/components/templates/data-tree/FlowStepTreeNodeComponent";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import type { DraftStepDto } from "@/shared/models/database/flow-draft-dto";
import { useWizardStore } from "@/features/wizard/store/wizard-store";
import { buildDraftTree } from "@/features/wizard/draft-tree";
import { optionsFor } from "@/features/wizard/action-to-steps";
import ActionDecisionComponent, {
  type PlacementOption,
} from "@/features/wizard/components/ActionDecisionComponent";

/** Types the save gives branches to, which is what makes "inside Success" an option at all. */
const BRANCHING: FlowStepTypeEnum[] = [
  FlowStepTypeEnum.IMAGE_SEARCH,
  FlowStepTypeEnum.READ_TEXT,
  FlowStepTypeEnum.SYSTEM_COMMAND,
  FlowStepTypeEnum.CHECK_VALUE,
];

interface StepperHandle {
  nextCallback: () => void;
  prevCallback: () => void;
}

/**
 * Walks the recorded actions one at a time, asking what each should become and where it goes,
 * with the flow taking shape beside them.
 *
 * The tree only shows what has been decided. Placement is the user's answer, not a prediction,
 * so there is nothing honest to draw for an action nobody has answered yet beyond the dashed row
 * showing where the current one would land.
 */
export default function WizardPage() {
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { openConfirm } = useDialogStore();
  const {
    target,
    lookupFlowId,
    actions,
    cursor,
    steps,
    openParentTempId,
    openParentBranch,
    addSteps,
    rewindTo,
    reset,
  } = useWizardStore();

  const stepperRef = useRef<StepperHandle | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const action = actions[cursor];

  /**
   * Where the next action can land, relative to the last step added.
   *
   * Three answers at most, and only the ones that exist: stay beside the last step, step into
   * its Success branch when it has one, or step back out of the branch it sits in. Asking
   * against the last step alone was wrong, because an option like "find this image then click
   * it" ends on the click, so the search it belongs to was never offered.
   */
  const placementOptions = useMemo((): PlacementOption[] => {
    const last = steps[steps.length - 1];
    if (!last) return [];

    const contextOf = (step: DraftStepDto) => ({
      parentTempId: step.parentTempId ?? undefined,
      parentBranch: step.parentBranch ?? undefined,
    });

    const options: PlacementOption[] = [
      {
        id: "after",
        label: `After "${last.values.name}"`,
        iconName: "arrow-down",
        placement: contextOf(last),
      },
    ];

    if (BRANCHING.includes(last.values.flowStepType)) {
      options.push({
        id: "inside",
        label: `Inside "${last.values.name}" → Success`,
        iconName: "chevron-right",
        placement: {
          parentTempId: last.tempId,
          parentBranch: FlowStepTypeEnum.SUCCESS,
        },
      });
    }

    const owner = steps.find((x) => x.tempId === last.parentTempId);
    if (owner) {
      options.push({
        id: "outside",
        label: `After "${owner.values.name}", outside its branch`,
        iconName: "reply",
        placement: contextOf(owner),
      });
    }

    return options;
  }, [steps]);

  const nextTempId = useMemo(
    () => steps.reduce((max, step) => Math.max(max, step.tempId), 0) + 1,
    [steps],
  );

  const treeNodes = useMemo(
    () =>
      buildDraftTree(
        steps,
        action
          ? {
              parentTempId: openParentTempId,
              parentBranch: openParentBranch,
              name: optionsFor(action)[0]?.label ?? action.summary,
            }
          : undefined,
      ),
    [steps, action, openParentTempId, openParentBranch],
  );

  if (actions.length === 0) {
    return (
      <div className="m-4 flex flex-column gap-3">
        <LabelComponent
          text="There is nothing to review."
          size="lg"
        />
        <div>
          <Button
            type="button"
            label="Back to flows"
            icon="pi pi-arrow-left"
            onClick={() => navigate("/flows")}
          />
        </div>
      </div>
    );
  }

  // Going back throws away everything decided after it, so it is worth a stop.
  const goBack = (to: number) => {
    if (to >= cursor || to < 0) return;

    const losing = steps.filter((step) => (step.actionIndex ?? 0) >= to).length;

    if (losing === 0) {
      rewindTo(to);
      stepperRef.current?.prevCallback();
      return;
    }

    openConfirm("wizard-rewind", {
      headerText: "Go back?",
      confirmLabel: "Go back and redo",
      confirmSeverity: "warning",
      children: (
        <LabelComponent
          text={`Changing this action drops the ${losing} step${losing === 1 ? "" : "s"} decided after it. Later steps can sit inside this one, so they cannot survive it changing.`}
        />
      ),
      onConfirm: () => {
        rewindTo(to);
        stepperRef.current?.prevCallback();
      },
    });
  };

  const handleSave = async () => {
    if (!target) return;

    setIsSaving(true);
    setError(null);

    try {
      const result = await backendApiService.FlowStep.createMany({ target, steps });

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ["flow"] }),
        queryClient.invalidateQueries({ queryKey: ["flowStep"] }),
        queryClient.invalidateQueries({ queryKey: ["flows", "list"] }),
        queryClient.invalidateQueries({ queryKey: ["flowValidation"] }),
      ]);

      reset();
      navigate(`/workflow/${result.flowId}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDiscard = async () => {
    try {
      await backendApiService.Recording.discard();
    } catch (err) {
      console.error(err);
    }

    reset();
    navigate("/flows");
  };

  return (
    <div className="m-4 flex flex-column gap-3">
      <div className="flex align-items-center justify-content-between">
        <div className="flex flex-column">
          <LabelComponent
            text="Build the flow"
            size="lg"
            weight="bold"
          />
          <LabelComponent
            text={`Action ${Math.min(cursor + 1, actions.length)} of ${actions.length}. Decide what each one becomes.`}
            size="sm"
            color="secondary"
          />
        </div>

        <Button
          type="button"
          label="Discard"
          onClick={handleDiscard}
          className="p-button-text"
        />
      </div>

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={error}
        />
      )}

      <div className="grid">
        <div className="col-12 lg:col-7">
          <Stepper
            ref={stepperRef as never}
            orientation="vertical"
            onChangeStep={(e) => goBack(e.index)}
          >
            {actions.map((current, index) => (
              <StepperPanel
                key={current.index}
                header={current.summary}
              >
                {index === cursor && (
                  <ActionDecisionComponent
                    action={current}
                    placementOptions={placementOptions}
                    flowId={lookupFlowId}
                    nextTempId={nextTempId}
                    onBack={() => goBack(index - 1)}
                    onConfirm={(built, placement) => {
                      addSteps(
                        built.map((step) => ({ ...step, actionIndex: index })),
                        placement,
                      );
                      stepperRef.current?.nextCallback();
                    }}
                    onSkip={() => {
                      // Nothing to add, but the cursor still has to move on.
                      addSteps([], {
                        parentTempId: openParentTempId,
                        parentBranch: openParentBranch,
                      });
                      stepperRef.current?.nextCallback();
                    }}
                  />
                )}
              </StepperPanel>
            ))}

            <StepperPanel header="Save">
              <div className="flex flex-column gap-3 pt-3">
                <LabelComponent
                  text={
                    steps.length === 0
                      ? "Every action was skipped. Discard and record again."
                      : `${steps.length} steps will be added to the flow.`
                  }
                />

                <div>
                  <Button
                    type="button"
                    label={isSaving ? "Saving..." : "Save flow"}
                    icon="pi pi-check"
                    loading={isSaving}
                    disabled={steps.length === 0 || isSaving}
                    onClick={handleSave}
                  />
                </div>
              </div>
            </StepperPanel>
          </Stepper>
        </div>

        <div className="col-12 lg:col-5">
          <Panel header="Flow so far">
            {steps.length === 0 && !action && (
              <LabelComponent
                text="Nothing added yet."
                size="sm"
                color="secondary"
              />
            )}

            <Tree
              value={treeNodes}
              nodeTemplate={(node: TreeNode) => {
                const treeNode = node as TreeNodeDto;

                return treeNode.isNew ? (
                  <div
                    className="p-2 border-round"
                    style={{
                      border: "1px dashed var(--primary-color)",
                      opacity: 0.7,
                    }}
                  >
                    <LabelComponent
                      text={treeNode.name}
                      size="sm"
                      color="secondary"
                    />
                  </div>
                ) : (
                  <FlowStepTreeNodeComponent treeNode={treeNode} />
                );
              }}
              className="border-none p-0"
              pt={{
                content: () => ({
                  className: "border-none bg-transparent shadow-none",
                }),
              }}
            />
          </Panel>
        </div>
      </div>
    </div>
  );
}
