import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";

interface Props {
  headerText?: string;
  children: React.ReactNode;
  onClose: () => void;
}
export function DialogWarningComponent({
  headerText,
  children,
  onClose,
}: Props) {
  return (
    <>
      <Dialog
        header={headerText}
        onHide={onClose}
        // style={{ width: "520px" }}
        maximizable
        modal
        footer={
          <div className="flex justify-end gap-3">
            <Button
              label="Ok"
              severity="secondary"
              onClick={onClose}
            />
          </div>
        }
      >
        {children}
      </Dialog>
    </>
  );
}
