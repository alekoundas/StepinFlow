export const GitChangeKindEnum = {
  ADDED: "ADDED",
  MODIFIED: "MODIFIED",
  DELETED: "DELETED",
} as const;

export type GitChangeKindEnum =
  (typeof GitChangeKindEnum)[keyof typeof GitChangeKindEnum];
