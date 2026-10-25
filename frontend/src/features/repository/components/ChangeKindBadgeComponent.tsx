import type { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";
import { CHANGE_KIND, MONO } from "@/features/repository/repository-format";

interface Props {
  kind: GitChangeKindEnum;
}

/** A, M or D in the colour of what happened to the file, as git clients mark them. */
export default function ChangeKindBadgeComponent({ kind }: Props) {
  const look = CHANGE_KIND[kind];

  return (
    <span
      title={look.word}
      className="inline-flex align-items-center justify-content-center flex-none border-round-sm text-xs font-bold"
      style={{ width: 20, height: 20, color: look.color, border: `1px solid ${look.color}`, fontFamily: MONO }}
    >
      {look.letter}
    </span>
  );
}
