import type { CSSProperties } from "react";

import { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";
import type { RepositoryImageChangeDto } from "@/shared/models/repository-dtos";
import ChangeKindBadgeComponent from "@/features/repository/components/ChangeKindBadgeComponent";
import { MONO, imageSource } from "@/features/repository/repository-format";

const MUTED: CSSProperties = { color: "var(--text-color-secondary)" };

// A checkerboard behind the picture, so a transparent template still shows its edges.
const PICTURE: CSSProperties = {
  height: 72,
  objectFit: "contain",
  background: "repeating-conic-gradient(#f2f3f5 0% 25%, #ffffff 0% 50%) 50% / 12px 12px",
};

interface Props {
  images: RepositoryImageChangeDto[];
}

/** Each template image that changed: as it was, and as it is. */
export default function TemplateImageChangesComponent({ images }: Props) {
  if (images.length === 0) return null;

  return (
    <div className="flex flex-column gap-2">
      <span className="font-medium">Template images</span>

      <div
        className="gap-2"
        style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(260px, 1fr))" }}
      >
        {images.map((image) => (
          <div
            key={image.name}
            className="flex flex-column gap-2 p-3 border-round-lg"
            style={{ border: "1px solid var(--surface-border)", background: "var(--surface-ground)" }}
          >
            <div className="flex align-items-center gap-2 min-w-0">
              <ChangeKindBadgeComponent kind={image.kind} />
              <span
                className="text-sm white-space-nowrap overflow-hidden text-overflow-ellipsis"
                style={{ fontFamily: MONO }}
              >
                {image.name}
              </span>
            </div>

            <div className="flex align-items-center gap-2">
              {image.before && (
                <figure className="m-0 flex-1 flex flex-column gap-1">
                  <img
                    src={imageSource(image.name, image.before)}
                    alt={`${image.name} at the last commit`}
                    className="w-full border-round"
                    style={{ ...PICTURE, opacity: image.kind === GitChangeKindEnum.DELETED ? 0.5 : 0.8 }}
                  />
                  <figcaption
                    className="text-xs"
                    style={MUTED}
                  >
                    {image.kind === GitChangeKindEnum.DELETED ? "Removed" : "Last commit"}
                  </figcaption>
                </figure>
              )}

              {image.before && image.after && (
                <i
                  className="pi pi-arrow-right text-sm"
                  style={MUTED}
                  aria-hidden="true"
                />
              )}

              {image.after && (
                <figure className="m-0 flex-1 flex flex-column gap-1">
                  <img
                    src={imageSource(image.name, image.after)}
                    alt={`${image.name} now`}
                    className="w-full border-round"
                    style={PICTURE}
                  />
                  <figcaption
                    className="text-xs"
                    style={MUTED}
                  >
                    {image.kind === GitChangeKindEnum.ADDED ? "New" : "Now"}
                  </figcaption>
                </figure>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
