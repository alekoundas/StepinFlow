import { z } from "zod";
import { scriptName } from "@/shared/utils/script-text";

export const FlowStepMarkerSchema = z.object({
  name: scriptName(),
});
