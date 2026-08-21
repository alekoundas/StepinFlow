import { useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { Button } from "primereact/button";
import { Message } from "primereact/message";
import { Panel } from "primereact/panel";
import { Tree } from "primereact/tree";
import type { TreeNode } from "primereact/treenode";

import LabelComponent from "@/shared/components/LabelComponent";
import { backendApiService } from "@/shared/services/backend-api-service";
import { FlowStepTypeEnum } from "@/shared/enums/backend/flow-step-types-enum";
import { FlowStepImageDto } from "@/shared/models/database/flow-step-image-dto";
import { ValidationSeverityEnum } from "@/shared/models/database/flow-validation-result-dto";
import { FlowStepTreeNodeComponent } from "@/features/flow-step/components/templates/data-tree/FlowStepTreeNodeComponent";
import type { TreeNodeDto } from "@/shared/models/tree-node-dto";
import { useWizardStore } from "@/features/wizard/store/wizard-store";
import { buildDraftTree } from "@/features/wizard/draft-tree";
import DraftStepCardComponent from "@/features/wizard/components/DraftStepCardComponent";

/**
 * Resolves a draft into steps worth saving.
 *
 * The list is the work and the tree is the answer to "what am I about to get", which is the
 * thing a wizard usually hides until it is too late to change your mind.
 */
export default function WizardPage() {
  const navigate = useNavigate();
  const { draft, updateStep, removeStep, reset } = useWizardStore();

  const [selectedTempId, setSelectedTempId] = useState<number | null>(null);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  // Memoised together: a fresh [] on every render would defeat the tree memo below it.
  const steps = useMemo(() => draft?.steps ?? [], [draft]);
  const treeNodes = useMemo(() => buildDraftTree(steps), [steps]);

  const errorCount = steps.reduce(
    (total, step) =>
      total + step.unresolved.filter((x) => x.severity === ValidationSeverityEnum.ERROR).length,
    0,
  );

  if (!draft) {
    return (
      <div className="m-4 flex flex-column gap-3">
        <LabelComponent
          text="There is nothing to review."
          size="lg"
        />
        <div>
          <Button
            type="button"
            label="Record a flow"
            icon="pi pi-circle-fill"
            onClick={() => navigate("/record")}
          />
        </div>
      </div>
    );
  }

  // Trades fixed coordinates for a template the search can find wherever it moved to, which is
  // the difference between a recording that survives a window move and one that does not.
  //
  // The point the click would have used is dropped: the search result is the position now.
  const promoteToImageSearch = (tempId: number, templateBase64: string) =>
    updateStep(tempId, (step) => ({
      ...step,
      values: {
        ...step.values,
        flowStepType: FlowStepTypeEnum.IMAGE_SEARCH,
        name: "Find on screen",
        isPointCustom: false,
        flowStepImages: [
          new FlowStepImageDto({ name: "Recorded template", templateImage: templateBase64 }),
        ],
      },
      newPoint: null,
      newPointEnd: null,
    }));

  const handleSave = async () => {
    setIsSaving(true);
    setError(null);

    try {
      const result = await backendApiService.FlowStep.createMany(draft);
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
            text="Review recorded steps"
            size="lg"
            weight="bold"
          />
          <LabelComponent
            text={`${steps.length} steps proposed. Correct anything wrong, drop what you do not need, then save.`}
            size="sm"
            color="secondary"
          />
        </div>

        <div className="flex gap-2">
          <Button
            type="button"
            label={isSaving ? "Saving..." : `Save ${steps.length} steps`}
            icon="pi pi-check"
            loading={isSaving}
            disabled={steps.length === 0 || isSaving}
            onClick={handleSave}
          />
          <Button
            type="button"
            label="Discard"
            onClick={handleDiscard}
            className="p-button-text"
          />
        </div>
      </div>

      {error && (
        <Message
          severity="error"
          className="w-full justify-content-start"
          text={error}
        />
      )}

      {errorCount > 0 && (
        <Message
          severity="warn"
          className="w-full justify-content-start"
          text={`${errorCount} step${errorCount === 1 ? "" : "s"} still need something before the flow will run. You can save now and finish them in the tree.`}
        />
      )}

      <div className="grid">
        <div className="col-12 lg:col-7 flex flex-column gap-2">
          {steps.map((step, index) => (
            <DraftStepCardComponent
              key={step.tempId}
              step={step}
              index={index}
              isSelected={selectedTempId === step.tempId}
              onSelect={() => setSelectedTempId(step.tempId)}
              onPromoteToImageSearch={(template) => promoteToImageSearch(step.tempId, template)}
              onRemove={() => removeStep(step.tempId)}
            />
          ))}

          {steps.length === 0 && (
            <LabelComponent
              text="Every step was dropped. Discard and record again."
              color="secondary"
            />
          )}
        </div>

        <div className="col-12 lg:col-5">
          <Panel header="Result">
            <Tree
              value={treeNodes}
              nodeTemplate={(node: TreeNode) => (
                <FlowStepTreeNodeComponent treeNode={node as TreeNodeDto} />
              )}
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
