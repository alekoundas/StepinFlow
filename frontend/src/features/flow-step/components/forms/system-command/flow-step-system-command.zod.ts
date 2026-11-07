import { z } from "zod";
import { ShellTypeEnum } from "@/shared/enums/backend/shell/shell-type-enum";
import { ShellCommandModeEnum } from "@/shared/enums/backend/shell/shell-command-mode-enum";
import { ShellResultSourceEnum } from "@/shared/enums/backend/shell/shell-result-source-enum";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

// The backend catalog decides which modes take a parameter. The schema cannot read it, so the
// handful that take none are listed here; anything new defaults to requiring one, which shows up
// as a field asking for a value rather than as a command that silently renders wrong.
const MODES_WITHOUT_PARAMETER: ShellCommandModeEnum[] = [
  ShellCommandModeEnum.READ_CLIPBOARD,
  ShellCommandModeEnum.CANCEL_SHUTDOWN,
];

export const FlowStepSystemCommandSchema = z
  .object({
    codeComment: CODE_COMMENT_SCHEMA,
    name: z
      .string()
      .min(1, "Name is required")
      .max(120, "Name too long")
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),

    shellType: z.enum(ShellTypeEnum),
    shellCommandMode: z.enum(ShellCommandModeEnum),
    shellCommandValue: z
      .string()
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
    // A launch's window, waited for once it is started.
    flowAreaId: z.number().int().nullish(),
    // What goes after what that area is started by.
    shellArguments: z
      .string()
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
    shellWorkingDirectory: z
      .string()
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),

    shellSuccessExitCodes: z
      .string()
      .regex(/^\s*\d+(\s*,\s*\d+)*\s*$/, "One or more exit codes, separated by commas"),
    shellResultSource: z.enum(ShellResultSourceEnum),
    timeoutMilliseconds: z.number().int().min(0),

    resultExtractPattern: z
      .string()
      .refine((text) => !text.includes("<[") && !text.includes("]>"), "Can't contain <[ or ]>"),
  })
  .superRefine((data, ctx) => {
    if (data.shellCommandMode === ShellCommandModeEnum.CUSTOM) {
      if (data.shellCommandValue.trim().length === 0) {
        ctx.addIssue({
          code: "custom",
          message: "Type the command to run",
          path: ["shellCommandValue"],
        });
      }
      return;
    }

    if (MODES_WITHOUT_PARAMETER.includes(data.shellCommandMode)) return;

    // A launch of an area starts what the area is started by. The validator says when that is
    // nothing.
    if (data.shellCommandMode === ShellCommandModeEnum.LAUNCH_APP && data.flowAreaId) return;

    if (data.shellCommandValue.trim().length === 0) {
      ctx.addIssue({
        code: "custom",
        message: "This is required",
        path: ["shellCommandValue"],
      });
    }
  });
