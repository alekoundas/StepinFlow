import type { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import type { RunCommandShellEnum } from "@/shared/enums/backend/command/run-command-shell-enum";

// Sent by the backend so the form previews the same command the runner executes.
export interface CommandPresetDto {
  preset: RunCommandPresetEnum;
  label: string;
  description: string;
  shell: RunCommandShellEnum;

  // {0} is the parameter, when there is one.
  commandTemplate: string;

  hasParameter: boolean;
  parameterLabel: string;
  parameterPlaceholder: string;
  parameterDefault: string;

  isEditable: boolean;
  isConfirmationRequired: boolean;
}
