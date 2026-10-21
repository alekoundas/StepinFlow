import { z } from "zod";
import { CODE_COMMENT_SCHEMA } from "@/features/flow-step/components/forms/shared/code-comment.zod";

// A branch's name is fixed, so the comment is all there is to edit.
export const FlowStepBranchSchema = z.object({
  codeComment: CODE_COMMENT_SCHEMA,
});
