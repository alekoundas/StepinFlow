export const FormMode = {
  ADD: "ADD",
  VIEW: "VIEW",
  EDIT: "EDIT",
  DELETE: "DELETE",
  CLONE: "CLONE",
} as const;

export type FormMode = (typeof FormMode)[keyof typeof FormMode];
