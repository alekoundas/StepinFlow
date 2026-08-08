import { Message } from "primereact/message";

import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowStepMovePreviewDto } from "@/shared/models/flow-step-move.dto";

interface Props {
  preview: FlowStepMovePreviewDto;
}

/**
 * Body of the move confirmation. The dialog chrome, the buttons and the busy state belong to
 * DialogConfirmComponent, so this is only what the user needs to read before answering.
 */
export function TreeMoveConfirmContentComponent({ preview }: Props) {
  if (!preview.isValid) {
    return (
      <Message
        severity="error"
        text={preview.errorMessage ?? "This move is not allowed."}
        className="w-full justify-content-start"
      />
    );
  }

  const hasBrokenReferences = preview.brokenReferences.length > 0;

  const summary = preview.isReorderOnly
    ? `Move "${preview.movedStepName}" to a new position among its current siblings.`
    : `Move "${preview.movedStepName}" into ${preview.targetParentName}.`;

  return (
    <div className="flex flex-column gap-3">
      <LabelComponent text={summary} />

      {preview.movedDescendantCount > 0 && (
        <LabelComponent
          size="sm"
          color="secondary"
          text={
            preview.movedDescendantCount === 1
              ? "1 child step moves with it."
              : `${preview.movedDescendantCount} child steps move with it.`
          }
        />
      )}

      {hasBrokenReferences && (
        <div className="flex flex-column gap-2">
          <Message
            severity="warn"
            className="w-full justify-content-start"
            text={
              preview.brokenReferences.length === 1
                ? "1 step will lose the search result it points at."
                : `${preview.brokenReferences.length} steps will lose the search result they point at.`
            }
          />

          <LabelComponent
            size="sm"
            color="secondary"
            text="A cursor step can only use the result of a step it runs under. After this move that is no longer true for:"
          />

          <ul className="m-0 pl-4">
            {preview.brokenReferences.map((reference) => (
              <li
                key={`${reference.flowStepId}-${reference.isEndReference}`}
                className="mb-1"
              >
                <LabelComponent
                  size="sm"
                  text={`${reference.flowStepName} → ${reference.referencedStepName}${
                    reference.isEndReference ? " (drop point)" : ""
                  }`}
                />
              </li>
            ))}
          </ul>

          <LabelComponent
            size="sm"
            color="secondary"
            text="You can still move it, but those steps need a new location before the flow runs correctly."
          />
        </div>
      )}
    </div>
  );
}
