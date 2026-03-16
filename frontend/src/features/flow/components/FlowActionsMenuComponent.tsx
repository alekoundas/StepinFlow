import { Button } from "primereact/button";
import { Menu } from "primereact/menu";
import { useRef } from "react";

type Props = {
  flowId: number;
  onEdit: (id: number) => void;
  onClone: (id: number) => void;
  onDelete: (id: number) => void;
};

export function FlowActionsMenuComponent({
  flowId,
  onEdit,
  onClone,
  onDelete,
}: Props) {
  const menuRef = useRef(null);

  const items = [
    { label: "Edit", icon: "pi pi-pencil", command: () => onEdit(flowId) },
    { label: "Clone", icon: "pi pi-copy", command: () => onClone(flowId) },
    {
      label: "Delete",
      icon: "pi pi-trash",
      command: () => onDelete(flowId),
      className: "text-red-600",
    },
  ];

  return (
    <>
      <Button
        icon="pi pi-ellipsis-v"
        text
        // onClick={(e) => menuRef.current?.toggle(e)}
      />
      <Menu
        model={items}
        popup
        ref={menuRef}
      />
    </>
  );
}
