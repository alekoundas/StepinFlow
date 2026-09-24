import { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";

export interface MatchMode {
  value: TemplateMatchModeEnum;
  label: string;
  description: string;
}

export const MATCH_MODES: MatchMode[] = [
  {
    value: TemplateMatchModeEnum.SHAPE,
    label: "Shape",
    description:
      "Matches the outline and forgives brightness, so it still finds the button on another PC's screen or theme. Cannot tell an enabled button from a disabled one.",
  },
  {
    value: TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS,
    label: "Shape and brightness",
    description:
      "Brightness counts too. For state: enabled or disabled, checked or not, lit or dim. Keep the accuracy high - around 0.95 - or it matches almost anything on a plain background.",
  },
];
