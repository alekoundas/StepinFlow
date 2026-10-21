import { z } from "zod";

/** Why it is here, on every step's form. Each line is printed as a # line, so any text will do. */
export const CODE_COMMENT_SCHEMA = z.string().max(1000, "1000 characters at most").optional();
