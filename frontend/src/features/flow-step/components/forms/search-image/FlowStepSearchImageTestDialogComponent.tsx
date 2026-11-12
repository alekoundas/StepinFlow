import { useState } from "react";
import type { ReactNode } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";
import { Message } from "primereact/message";

import DisabledTooltipComponent from "@/shared/components/DisabledTooltipComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import type { ImageSearchTestResultDto } from "@/shared/models/database/image-search-test-result-dto";
import { FlowStepSearchImageTestRowComponent } from "@/features/flow-step/components/forms/search-image/FlowStepSearchImageTestRowComponent";
import { FlowStepSearchImageTestViewerComponent } from "@/features/flow-step/components/forms/search-image/FlowStepSearchImageTestViewerComponent";
import { FlowStepSearchImageTestPatchComponent } from "@/features/flow-step/components/forms/search-image/FlowStepSearchImageTestPatchComponent";
import {
  checksOf,
  hitsOf,
  isSameTemplates,
  modeLineOf,
  nameOf,
  nearMissOf,
  refusalOf,
  refusedOf,
  suggestAccuracy,
  usedIndexes,
  verdictOf,
  type ImageSearchTestRun,
} from "@/features/flow-step/components/forms/search-image/image-search-test";

interface Props {
  isVisible: boolean;
  result: ImageSearchTestResultDto;
  run: ImageSearchTestRun;
  // The form's templates now, which a suggestion writes to.
  images: FlowStepTemplateDto[];
  isTesting: boolean;
  canEdit: boolean;
  onTestAgain: () => void;
  onUseAccuracy: (index: number, accuracy: number) => void;
  onHide: () => void;
}

/**
 * What a Test found, one template at a time: its hits and the closest place under its bar, drawn
 * on the screenshot and side by side with the template, and the accuracy between the two.
 *
 * Rendered by the form rather than through the dialog store, which keeps elements: a suggestion
 * written to the form and a test made again both have to reach it.
 */
export default function FlowStepSearchImageTestDialogComponent({
  isVisible,
  result,
  run,
  images,
  isTesting,
  canEdit,
  onTestAgain,
  onUseAccuracy,
  onHide,
}: Props) {
  const [picked, setPicked] = useState<number | null>(null);

  const used = usedIndexes(result, run.searchMode);
  const selected = Math.min(picked ?? used[0] ?? 0, result.images.length - 1);
  const isCurrent = isSameTemplates(run, images);

  const accuracyOf = (index: number) =>
    isCurrent ? images[index].accuracy : run.images[index].accuracy;

  const testAgain = (
    <Button
      type="button"
      label="Test again"
      icon="pi pi-refresh"
      loading={isTesting}
      outlined
      onClick={onTestAgain}
    />
  );

  return (
    <Dialog
      visible={isVisible}
      onHide={onHide}
      header="What the search found"
      modal
      draggable
      resizable
      maximizable
      style={{ width: "80vw", height: "85vh" }}
      contentClassName="flex flex-column gap-3"
      footer={
        <Button
          type="button"
          label="Close"
          severity="secondary"
          onClick={onHide}
        />
      }
    >
      {!result.isResolved || !result.screenshot || selected < 0 ? (
        <div className="flex align-items-center gap-3">
          <Message
            severity="error"
            className="flex-1 justify-content-start"
            text={result.errorMessage ?? "The search area could not be captured."}
          />
          {testAgain}
        </div>
      ) : (
        <>
          <Verdict
            result={result}
            run={run}
            action={testAgain}
          />

          {!isCurrent && (
            <Message
              severity="warn"
              className="w-full justify-content-start"
              text="The templates have changed since this test. Test again to see them."
            />
          )}

          <div
            className="flex gap-3 flex-1"
            style={{ minHeight: 0 }}
          >
            <div
              className="flex flex-column gap-2 overflow-auto flex-shrink-0"
              style={{ width: "21rem" }}
            >
              {result.images.map((image, index) => (
                <FlowStepSearchImageTestRowComponent
                  key={`${image.flowStepTemplateId}-${index}`}
                  name={nameOf(run, result, index)}
                  image={image}
                  tested={run.images[index]}
                  accuracy={accuracyOf(index)}
                  isUsed={used.includes(index)}
                  isSelected={index === selected}
                  onSelect={() => setPicked(index)}
                />
              ))}
            </div>

            <div
              className="flex flex-column gap-3 flex-1"
              style={{ minWidth: 0, minHeight: 0 }}
            >
              <FlowStepSearchImageTestViewerComponent
                screenshot={result.screenshot}
                width={result.searchAreaWidth}
                height={result.searchAreaHeight}
                hits={hitsOf(result.images[selected])}
                nearMiss={nearMissOf(result.images[selected])}
                refused={refusedOf(result.images[selected])}
              />

              <Detail
                result={result}
                run={run}
                index={selected}
                accuracy={accuracyOf(selected)}
                useBlockedBecause={blockedReason(canEdit, isCurrent)}
                onUseAccuracy={onUseAccuracy}
              />
            </div>
          </div>

          <LabelComponent
            text={`Search area ${result.searchAreaWidth}x${result.searchAreaHeight} at ${result.searchAreaX}, ${result.searchAreaY}`}
            size="xs"
            color="secondary"
          />
        </>
      )}
    </Dialog>
  );
}

// Why Use cannot write to the form, or null when it can.
function blockedReason(canEdit: boolean, isCurrent: boolean): string | null {
  if (!canEdit) return "Edit the step to change its accuracy.";
  if (!isCurrent) return "Test again first: the templates have changed since this test.";

  return null;
}

interface VerdictProps {
  result: ImageSearchTestResultDto;
  run: ImageSearchTestRun;
  action: ReactNode;
}

function Verdict({ result, run, action }: VerdictProps) {
  const verdict = verdictOf(result, run);
  const isSuccess = verdict.severity === "success";

  return (
    <div className="flex align-items-start gap-3">
      <div className="flex flex-column gap-1 flex-1">
        <span className="flex align-items-center gap-2 font-semibold">
          <i
            className={isSuccess ? "pi pi-check-circle" : "pi pi-exclamation-circle"}
            style={{ color: isSuccess ? "var(--green-500)" : "var(--orange-500)" }}
          />
          {verdict.text}
        </span>
        <LabelComponent
          text={modeLineOf(run)}
          size="xs"
          color="secondary"
        />
      </div>
      {action}
    </div>
  );
}

interface DetailProps {
  result: ImageSearchTestResultDto;
  run: ImageSearchTestRun;
  index: number;
  accuracy: number;
  useBlockedBecause: string | null;
  onUseAccuracy: (index: number, accuracy: number) => void;
}

function Detail({ result, run, index, accuracy, useBlockedBecause, onUseAccuracy }: DetailProps) {
  const image = result.images[index];
  const tested = run.images[index];
  const hits = hitsOf(image);
  const nearMiss = nearMissOf(image);
  const refused = refusedOf(image);
  const suggestion = suggestAccuracy(image);
  const screenshot = result.screenshot ?? "";
  const hitChecks = hits.length > 0 ? checksOf(hits[0]) : null;

  let advice = "Nothing on the screenshot resembles it.";
  if (hits.length > 0)
    advice = "Nothing else on the screenshot comes close, so any accuracy up to its hit is safe.";

  return (
    <div className="flex flex-column gap-2 p-3 border-1 surface-border border-round">
      <span className="text-sm font-semibold">{nameOf(run, result, index)}</span>

      <div className="flex gap-4 flex-wrap align-items-end">
        {tested.flowTemplate.templateImage && (
          <FlowStepSearchImageTestPatchComponent
            label="Template"
            outline="none"
            source={{ png: tested.flowTemplate.templateImage }}
          />
        )}
        {hits.length > 0 && (
          <FlowStepSearchImageTestPatchComponent
            label={`Hit · ${hits[0].score.toFixed(2)}${hitChecks ? ` · ${hitChecks}` : ""}`}
            outline="2px solid var(--green-400)"
            source={{ screenshot, box: hits[0] }}
          />
        )}
        {refused.length > 0 && (
          <FlowStepSearchImageTestPatchComponent
            label={`Refused · ${refused[0].score.toFixed(2)} · ${refusalOf(refused[0], result)}`}
            outline="2px dotted var(--red-400)"
            source={{ screenshot, box: refused[0] }}
          />
        )}
        {nearMiss && (
          <FlowStepSearchImageTestPatchComponent
            label={`Closest under the bar · ${nearMiss.score.toFixed(2)}`}
            outline="2px dashed var(--orange-400)"
            source={{ screenshot, box: nearMiss }}
          />
        )}
      </div>

      {image.scale !== 1 && (
        <LabelComponent
          text={`Searched at ${image.scale.toFixed(2)}x the size it was captured at.`}
          size="xs"
          color="secondary"
        />
      )}

      {refused.length > 0 && (
        <Message
          severity={hits.length > 0 ? "info" : "warn"}
          className="w-full justify-content-start"
          text={
            hits.length > 0
              ? `Passed over ${refused.length} with its shape but another colour or contrast - the same thing in another state - and took the one that matched.`
              : "The dotted box has its shape but not its colour or contrast: most likely the same thing in another state - greyed, faded, another colour. Lowering the accuracy will not find it. If that state should count too, use Shape, or add a template of it."
          }
        />
      )}

      {suggestion === null && refused.length === 0 && (
        <LabelComponent
          text={advice}
          size="sm"
          color="secondary"
        />
      )}

      {suggestion !== null && accuracy !== suggestion.accuracy && (
        <div className="flex align-items-center gap-3">
          <span className="text-sm text-color-secondary flex-1">{suggestion.reason}</span>
          <DisabledTooltipComponent
            tooltip={useBlockedBecause}
            position="top"
          >
            <Button
              type="button"
              label={`Use ${suggestion.accuracy.toFixed(2)}`}
              size="small"
              disabled={useBlockedBecause !== null}
              onClick={() => onUseAccuracy(index, suggestion.accuracy)}
            />
          </DisabledTooltipComponent>
        </div>
      )}

      {suggestion !== null && accuracy === suggestion.accuracy && (
        <span
          className="text-sm"
          style={{ color: "var(--green-600)" }}
        >
          {accuracy === tested.accuracy
            ? `Its accuracy is ${accuracy.toFixed(2)} already.`
            : `Set to ${accuracy.toFixed(2)} on the form. Save the step to keep it, or Test again to see it.`}
        </span>
      )}

      {suggestion?.isNarrow && (
        <Message
          severity="warn"
          className="w-full justify-content-start"
          text="The hit and the closest wrong place are close: that place looks a lot like the template, and a little light or a theme change can swap them. A tighter crop of the template helps more than any accuracy."
        />
      )}
    </div>
  );
}
