import { z } from "zod";
import { RunCommandShellEnum } from "@/shared/enums/backend/command/run-command-shell-enum";
import { RunCommandPresetEnum } from "@/shared/enums/backend/command/run-command-preset-enum";
import { ResultSourceEnum } from "@/shared/enums/backend/command/result-source-enum";

// The backend catalog decides which presets take a parameter. The schema cannot read it, so the
// handful that take none are listed here; anything new defaults to requiring one, which shows up
// as a field asking for a value rather than as a command that silently renders wrong.
const PRESETS_WITHOUT_PARAMETER: RunCommandPresetEnum[] = [
  RunCommandPresetEnum.READ_CLIPBOARD,
  RunCommandPresetEnum.CANCEL_SHUTDOWN,
];

export const FlowStepSystemCommandSchema = z
  .object({
    name: z.string().min(1, "Name is required").max(120, "Name too long"),

    runCommandShell: z.enum(RunCommandShellEnum),
    runCommandPreset: z.enum(RunCommandPresetEnum),
    runCommandValue: z.string(),
    runCommandWorkingDirectory: z.string(),

    successExitCodes: z
      .string()
      .regex(/^\s*\d+(\s*,\s*\d+)*\s*$/, "One or more exit codes, separated by commas"),
    resultSource: z.enum(ResultSourceEnum),
    timeoutMilliseconds: z.number().int().min(0),

    resultExtractPattern: z.string(),
  })
  .superRefine((data, ctx) => {
    if (data.runCommandPreset === RunCommandPresetEnum.CUSTOM) {
      if (data.runCommandValue.trim().length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Type the command to run",
          path: ["runCommandValue"],
        });
      }
      return;
    }

    if (PRESETS_WITHOUT_PARAMETER.includes(data.runCommandPreset)) return;

    if (data.runCommandValue.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "This is required",
        path: ["runCommandValue"],
      });
    }
  });
