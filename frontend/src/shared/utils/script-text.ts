import { z } from "zod";

/**
 * The flow script writes text between "<[" and "]>" and escapes nothing, so text holding either
 * one could not be exported. Kept in step with the catalog in
 * backend/Business/FlowScript/Catalogs/ScriptKeywordCatalog.cs, which the backend validates against.
 */
export const SCRIPT_TEXT_START = "<[";
export const SCRIPT_TEXT_END = "]>";

/** A text field the flow script writes, so it refuses the script's delimiters. */
export const scriptText = (schema: z.ZodString = z.string()): z.ZodString =>
  schema.refine(
    (text) => !text.includes(SCRIPT_TEXT_START) && !text.includes(SCRIPT_TEXT_END),
    { message: `Can't contain "${SCRIPT_TEXT_START}" or "${SCRIPT_TEXT_END}"` },
  );

/** The name a flow, area, point or step carries. */
export const scriptName = (): z.ZodString =>
  scriptText(z.string().min(1, "Name is required").max(120, "Name too long"));
