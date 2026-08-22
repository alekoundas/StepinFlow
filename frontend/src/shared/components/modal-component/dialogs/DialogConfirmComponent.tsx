import { useState } from "react";
import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";

export interface IDialogConfirmComponentProps {
  headerText?: string;
  children: React.ReactNode;

  confirmLabel?: string;
  confirmIcon?: string;
  confirmSeverity?: "secondary" | "success" | "info" | "warning" | "danger";
  cancelLabel?: string;

  /** Leaves only the dismiss button, for when the answer is "you cannot do this". */
  hideConfirm?: boolean;

  /** Wider than a question needs, for bodies that show something rather than ask something. */
  width?: string;

  onConfirm?: () => void | Promise<any>;
  onClose?: () => void; // injected by DialogRootComponent
}

/**
 * Ask a yes/no question with an arbitrary body.
 *
 * The busy state lives here rather than in the caller: dialogs are stored as elements in the
 * dialog store, so a prop passed at open time would be frozen at the value it had back then.
 */
export function DialogConfirmComponent({
  headerText,
  children,
  confirmLabel = "Confirm",
  confirmIcon = "pi pi-check",
  confirmSeverity,
  cancelLabel = "Cancel",
  hideConfirm = false,
  width = "34rem",
  onConfirm,
  onClose,
}: IDialogConfirmComponentProps) {
  const [isBusy, setIsBusy] = useState(false);

  const handleConfirm = async () => {
    setIsBusy(true);
    try {
      await onConfirm?.();
      onClose?.();
    } finally {
      setIsBusy(false);
    }
  };

  return (
    <Dialog
      header={headerText}
      visible={true}
      modal
      onHide={() => (isBusy ? undefined : onClose?.())}
      style={{ width }}
      footer={
        <div className="flex justify-content-end gap-3">
          <Button
            label={cancelLabel}
            severity="secondary"
            disabled={isBusy}
            onClick={onClose}
          />
          {!hideConfirm && (
            <Button
              label={confirmLabel}
              icon={confirmIcon}
              loading={isBusy}
              severity={confirmSeverity}
              onClick={handleConfirm}
            />
          )}
        </div>
      }
    >
      {children}
    </Dialog>
  );
}
