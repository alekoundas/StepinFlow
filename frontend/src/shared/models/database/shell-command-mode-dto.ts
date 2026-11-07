import type { ShellCommandModeEnum } from "@/shared/enums/backend/shell/shell-command-mode-enum";
import type { ShellTypeEnum } from "@/shared/enums/backend/shell/shell-type-enum";

// Sent by the backend so the form previews the same command the executor executes.
export interface ShellCommandModeDto {
  mode: ShellCommandModeEnum;
  label: string;
  description: string;
  shellType: ShellTypeEnum;

  // {0} is the parameter, when there is one.
  commandTemplate: string;

  hasParameter: boolean;
  parameterLabel: string;
  parameterPlaceholder: string;
  parameterDefault: string;

  isEditable: boolean;
  isConfirmationRequired: boolean;
}
