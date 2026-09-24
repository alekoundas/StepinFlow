import { TemplateMatchModeEnum } from "@/shared/enums/backend/template-match-mode-enum";

export interface MatchMode {
  value: TemplateMatchModeEnum;
  label: string;
  description: string;
  // Where a new template's accuracy starts. The two modes are different instruments, so one
  // number cannot mean the same strictness in both.
  defaultAccuracy: number;
}

export const MATCH_MODES: MatchMode[] = [
  {
    value: TemplateMatchModeEnum.SHAPE,
    label: "Shape",
    description:
      "Matches the outline and forgives brightness, so it still finds the button on another PC's screen or theme. Cannot tell an enabled button from a disabled one.",
    defaultAccuracy: 0.8,
  },
  {
    value: TemplateMatchModeEnum.SHAPE_AND_BRIGHTNESS,
    label: "Shape and brightness",
    description:
      "Brightness counts too. For state: enabled or disabled, checked or not, lit or dim. Keep each template's accuracy high - around 0.95 - or it matches almost anything on a plain background.",
    defaultAccuracy: 0.95,
  },
];

export function defaultAccuracyFor(mode: TemplateMatchModeEnum | undefined): number {
  return MATCH_MODES.find((x) => x.value === mode)?.defaultAccuracy ?? 0.8;
}
