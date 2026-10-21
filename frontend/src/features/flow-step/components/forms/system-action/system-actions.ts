import { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";

export interface SystemAction {
  systemActionType: SystemActionTypeEnum;
  label: string;
  description: string;
}

export const SYSTEM_ACTIONS: SystemAction[] = [
  {
    systemActionType: SystemActionTypeEnum.LOCK_WORKSTATION,
    label: "Lock",
    description:
      "Locks Windows. Everything keeps running behind the lock screen.",
  },
  {
    systemActionType: SystemActionTypeEnum.SLEEP_PC,
    label: "Sleep PC",
    description: "Puts the whole machine to sleep. The flow stops here.",
  },
  {
    systemActionType: SystemActionTypeEnum.MONITOR_OFF,
    label: "Monitor off",
    description: "Turns the screens off while everything keeps running.",
  },
  {
    systemActionType: SystemActionTypeEnum.MONITOR_ON,
    label: "Monitor on",
    description: "Wakes the screens back up.",
  },
];
