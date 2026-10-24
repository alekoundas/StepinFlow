import { Dialog } from "primereact/dialog";

import LabelComponent from "@/shared/components/LabelComponent";
import { ChoiceCardComponent } from "@/shared/components/ChoiceCardComponent";

export interface IFlowCreateChoiceDialogProps {
  /** Sub-Flow or Flow, so the wording matches the page it was opened from. */
  noun: string;

  /** Absent on the sub-flows page: a sub-flow is made by promoting a flow, never created directly. */
  onManual?: () => void;
  onRecord: () => void;
  onAi: () => void;

  onClose?: () => void; // injected by DialogRootComponent
}

/**
 * How to start a flow.
 *
 * The three ways used to be two buttons on the page and one that did not exist, which made the
 * recorder look like an afterthought next to New rather than the other half of the same decision.
 */
export function FlowCreateChoiceDialogComponent({
  noun,
  onManual,
  onRecord,
  onAi,
  onClose,
}: IFlowCreateChoiceDialogProps) {
  const choose = (act: () => void) => {
    onClose?.();
    act();
  };

  return (
    <Dialog
      visible
      header={`New ${noun}`}
      onHide={() => onClose?.()}
      style={{ width: "38rem" }}
      draggable={false}
    >
      <div className="flex flex-column gap-3">
        <LabelComponent
          text="Three ways to get to the same place. All of them end in the editor."
          size="sm"
          color="secondary"
        />

        {onManual ? (
          <ChoiceCardComponent
            icon="pencil"
            title="Build it myself"
            description="Start with an empty flow and add steps one at a time."
            onSelect={() => choose(onManual)}
          />
        ) : null}

        <ChoiceCardComponent
          icon="circle-fill"
          title="Record it"
          description="Do the task once. Every click and keystroke is captured, and you say what each one should become."
          onSelect={() => choose(onRecord)}
        />

        <ChoiceCardComponent
          icon="sparkles"
          title="Create with AI"
          description="Describe what you want, record it once, or both. The steps are written for you and land in the editor to check."
          onSelect={() => choose(onAi)}
        />
      </div>
    </Dialog>
  );
}

