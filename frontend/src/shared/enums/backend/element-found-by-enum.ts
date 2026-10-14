export const ElementFoundByEnum = {
  MODEL: "MODEL", // The detection model's rectangle
  FALLBACK: "FALLBACK", // A box around the click, when the model had nothing to say
} as const;

export type ElementFoundByEnum =
  (typeof ElementFoundByEnum)[keyof typeof ElementFoundByEnum];
