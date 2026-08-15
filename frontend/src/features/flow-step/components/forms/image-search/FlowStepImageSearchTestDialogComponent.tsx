import { useState } from "react";
import { Checkbox } from "primereact/checkbox";
import { Tag } from "primereact/tag";
import { classNames } from "primereact/utils";

import LabelComponent from "@/shared/components/LabelComponent";
import type {
  ImageSearchTestImageDto,
  ImageSearchTestResultDto,
} from "@/shared/models/database/image-search-test-result-dto";

interface Props {
  result: ImageSearchTestResultDto;
}

// Enough to tell templates apart at a glance. Green and red are left out: they mean found and
// missing on the summary tags, and a box colour is a template identity, not a verdict.
const BOX_COLOURS = [
  "var(--blue-400)",
  "var(--purple-400)",
  "var(--orange-400)",
  "var(--teal-400)",
  "var(--pink-400)",
  "var(--indigo-400)",
];

const percent = (value: number, total: number) =>
  total > 0 ? `${(value / total) * 100}%` : "0%";

export default function FlowStepImageSearchTestDialogComponent({ result }: Props) {
  const [hiddenIds, setHiddenIds] = useState<number[]>([]);

  const width = result.searchAreaWidth;
  const height = result.searchAreaHeight;

  const isHidden = (image: ImageSearchTestImageDto) =>
    hiddenIds.includes(image.flowStepImageId);

  const toggle = (image: ImageSearchTestImageDto) =>
    setHiddenIds((prev) =>
      prev.includes(image.flowStepImageId)
        ? prev.filter((x) => x !== image.flowStepImageId)
        : [...prev, image.flowStepImageId],
    );

  const colourOf = (index: number) => BOX_COLOURS[index % BOX_COLOURS.length];

  if (!result.screenshot) {
    return (
      <LabelComponent
        text="No screenshot was captured for this test."
        color="secondary"
        size="sm"
      />
    );
  }

  return (
    <div className="flex flex-column gap-3">
      {/* Boxes are positioned in percentages of the natural size, so they track the image as the
          dialog resizes without any scale factor to keep in sync. */}
      <div className="relative w-full">
        <img
          src={`data:image/jpeg;base64,${result.screenshot}`}
          alt=""
          className="w-full h-auto border-round-sm block"
        />

        {result.images.map((image, index) =>
          isHidden(image)
            ? null
            : image.matches.map((match, matchIndex) => (
                <div
                  key={`${image.flowStepImageId}-${matchIndex}`}
                  className="absolute"
                  style={{
                    left: percent(match.x, width),
                    top: percent(match.y, height),
                    width: percent(match.width, width),
                    height: percent(match.height, height),
                    border: `2px solid ${colourOf(index)}`,
                    boxSizing: "border-box",
                  }}
                  title={`${image.name} · ${match.score.toFixed(2)}${match.scale !== 1 ? ` · scaled ${match.scale.toFixed(2)}x` : ""}`}
                >
                  {/* Where the cursor actually lands once the click offset is applied. A box in
                      the right place with a crosshair in the wrong one is its own bug. */}
                  <div
                    className="absolute border-circle"
                    style={{
                      left: percent(match.clickX - match.x, match.width),
                      top: percent(match.clickY - match.y, match.height),
                      width: 8,
                      height: 8,
                      marginLeft: -4,
                      marginTop: -4,
                      background: colourOf(index),
                      boxShadow: "0 0 0 2px rgba(0,0,0,0.5)",
                    }}
                  />
                </div>
              )),
        )}
      </div>

      <div className="flex flex-column gap-2">
        {result.images.map((image, index) => (
          <div
            key={image.flowStepImageId}
            className="flex align-items-center gap-2"
          >
            <Checkbox
              inputId={`template-${image.flowStepImageId}`}
              checked={!isHidden(image)}
              disabled={image.matches.length === 0}
              onChange={() => toggle(image)}
            />

            <span
              className="flex-shrink-0 border-round-sm"
              style={{
                width: 12,
                height: 12,
                background: colourOf(index),
                opacity: image.matches.length === 0 ? 0.3 : 1,
              }}
            />

            <label
              htmlFor={`template-${image.flowStepImageId}`}
              className={classNames("text-sm flex-1", {
                "text-color-secondary": image.matches.length === 0,
              })}
            >
              {image.name || "(unnamed)"}
            </label>

            {image.isRequired && (
              <Tag
                severity="info"
                value="required"
              />
            )}

            <Tag
              severity={image.isFound ? "success" : "danger"}
              value={
                image.isFound
                  ? `${image.matchCount} · best ${image.bestScore.toFixed(2)}`
                  : "not found"
              }
            />
          </div>
        ))}
      </div>

      <LabelComponent
        text={`Search area ${width}x${height} at ${result.searchAreaX}, ${result.searchAreaY}`}
        color="secondary"
        size="sm"
      />
    </div>
  );
}
