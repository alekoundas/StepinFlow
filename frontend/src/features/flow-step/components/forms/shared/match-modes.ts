import { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";

export interface MatchMode {
  value: TemplateMatchModeEnum;
  label: string;
  description: string;
}

// The accuracy is the shape score in both modes, so one default serves both.
export const DEFAULT_ACCURACY = 0.8;

export const MATCH_MODES: MatchMode[] = [
  {
    value: TemplateMatchModeEnum.SHAPE,
    label: "Shape",
    description:
      "Matches the outline and forgives colour and brightness, so it finds the thing in any state. For something whose colour changes, such as an icon shown in several colours.",
  },
  {
    value: TemplateMatchModeEnum.SHAPE_AND_COLOUR,
    label: "Shape and colour",
    description:
      "Finds the shape, then checks its colour and contrast there, so a greyed, faded or recoloured copy of the same picture is passed over. For telling states apart: enabled or disabled, live or done.",
  },
];
