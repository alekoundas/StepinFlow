import { Button } from "primereact/button";
import { Dialog } from "primereact/dialog";

export interface IDialogFormComponentProps {
  headerText?: string;
  formId: string;
  children: React.ReactNode;
  onClose?: () => void;
  // onSubmit?: () => void;
}
export function DialogFormComponent({
  headerText,
  children,
  formId,
  onClose,
  // onSubmit,
}: IDialogFormComponentProps) {
  return (
    <>
      <Dialog
        header={headerText}
        onHide={onClose ?? (() => {})}
        visible={true}
        // style={{ width: "520px" }}
        maximizable
        modal
        footer={
          <div className="flex justify-end gap-3">
            <Button
              label="Cancel"
              severity="secondary"
              onClick={onClose}
            />
            <Button
              type="submit"
              form={formId}
              label={"Save"}
              icon="pi pi-check"
            />
          </div>
        }
      >
        {children}
      </Dialog>
    </>
  );
}
