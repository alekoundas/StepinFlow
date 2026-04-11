import React from "react";

import { useDialogStore } from "@/shared/components/modal-component/store/dialog-store";

export function DialogRootComponent() {
  const { dialogs, close } = useDialogStore();

  return (
    <>
      {dialogs.map(({ id, component}) => (
        <React.Fragment key={id}>
          {component &&
            React.cloneElement(component as any, {
              onClose: () => close(id),
            })}
        </React.Fragment>
      ))}
            {/* {dialogs.map(({ id, component }) => (
        <React.Fragment key={id}>
          {React.cloneElement(component as React.ReactElement, {
            // onClose: () => close(id),     // auto-injected for all dialogs
          })}
        </React.Fragment>
      ))} */}
    </>
  );
}
