import { SystemActionTypeEnum } from "@/shared/enums/backend/system-action-type-enum";

export interface SystemAction {
  systemActionType: SystemActionTypeEnum;
  label: string;
  description: string;
  defaultName: string;
}

export const SYSTEM_ACTIONS: SystemAction[] = [
  {
    systemActionType: SystemActionTypeEnum.LOCK_WORKSTATION,
    label: "Lock",
    description:
      "Locks Windows. Everything keeps running behind the lock screen.",

    defaultName: "Lock workstation",
  },
  {
    systemActionType: SystemActionTypeEnum.SLEEP_PC,
    label: "Sleep PC",
    description: "Puts the whole machine to sleep. The flow stops here.",

    defaultName: "Sleep PC",
  },
  {
    systemActionType: SystemActionTypeEnum.MONITOR_OFF,
    label: "Monitor off",
    description: "Turns the screens off while everything keeps running.",

    defaultName: "Monitor off",
  },
  {
    systemActionType: SystemActionTypeEnum.MONITOR_ON,
    label: "Monitor on",
    description: "Wakes the screens back up.",

    defaultName: "Monitor on",
  },
];
