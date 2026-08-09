import type { MenuItem } from "primereact/menuitem";

import { Button } from "primereact/button";
import { Menu } from "primereact/menu";
import { useRef } from "react";

interface ExtraAction {
  label: string;
  icon: string;
  command: (id: number) => void;
}

interface Props {
  id: number;
  onEdit?: (id: number) => void;
  onClone?: (id: number) => void;
  onDelete?: (id: number) => void;
  // Anything that is not edit/clone/delete, rather than overloading one of those.
  extraActions?: ExtraAction[];
}

export function ActionsMenuComponent({
  id,
  onEdit,
  onClone,
  onDelete,
  extraActions,
}: Props) {
  const menuRef = useRef<Menu>(null);
  const menuItems: MenuItem[] = [];

  if (onEdit)
    menuItems.push({
      label: "Edit",
      icon: "pi pi-pencil",
      command: () => onEdit(id),
    });

  if (onClone)
    menuItems.push({
      label: "Clone",
      icon: "pi pi-copy",
      command: () => onClone(id),
    });

  extraActions?.forEach((action) =>
    menuItems.push({
      label: action.label,
      icon: action.icon,
      command: () => action.command(id),
    }),
  );

  if (onDelete)
    menuItems.push({
      label: "Delete",
      icon: "pi pi-trash",
      command: () => onDelete(id),
      className: "text-red-600",
    });

  return (
    <>
      <Button
        type="button"
        icon="pi pi-ellipsis-v"
        text
        onClick={(e) => menuRef.current?.toggle(e)}
      />
      <Menu
        model={menuItems}
        popup
        ref={menuRef}
      />
    </>
  );
}
