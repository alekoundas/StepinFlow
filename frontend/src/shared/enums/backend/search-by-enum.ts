// How a recorded click, drag or check finds its element: by the picture cut there, or by the words on it.
export const SearchByEnum = {
  IMAGE: "IMAGE",
  TEXT: "TEXT",
} as const;

export type SearchByEnum = (typeof SearchByEnum)[keyof typeof SearchByEnum];
