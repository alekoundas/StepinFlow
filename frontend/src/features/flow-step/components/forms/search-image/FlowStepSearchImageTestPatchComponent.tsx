import { useEffect, useState } from "react";

import type { ImageSearchTestMatchDto } from "@/shared/models/database/image-search-test-result-dto";

interface Props {
  label: string;
  outline: string;
  // A PNG for the template itself, or the screenshot to cut the box out of.
  source: { png: string } | { screenshot: string; box: ImageSearchTestMatchDto };
}

/**
 * The template, or the patch of the screenshot it was matched against, at one height - so what it
 * matched instead is answered by looking rather than by a number.
 */
export function FlowStepSearchImageTestPatchComponent({ label, outline, source }: Props) {
  const patch = usePatch(source);

  return (
    <div className="flex flex-column gap-1 text-xs text-color-secondary">
      <span>{label}</span>
      {patch && (
        <img
          src={patch}
          alt=""
          className="border-round-sm"
          style={{
            height: 56,
            maxWidth: 180,
            objectFit: "contain",
            imageRendering: "pixelated",
            outline,
            outlineOffset: 2,
          }}
        />
      )}
    </div>
  );
}

// The box cut out of the screenshot with a canvas, at its own pixels.
function usePatch(source: Props["source"]): string | null {
  const [cut, setCut] = useState<string | null>(null);

  const screenshot = "screenshot" in source ? source.screenshot : null;
  const box = "box" in source ? source.box : null;

  useEffect(() => {
    if (screenshot === null || box === null) return;

    let isCurrent = true;
    const image = new Image();
    image.onload = () => {
      const canvas = document.createElement("canvas");
      canvas.width = box.width;
      canvas.height = box.height;
      canvas.getContext("2d")?.drawImage(image, box.x, box.y, box.width, box.height, 0, 0, box.width, box.height);

      if (isCurrent) setCut(canvas.toDataURL("image/png"));
    };
    image.src = `data:image/jpeg;base64,${screenshot}`;

    return () => {
      isCurrent = false;
    };
  }, [screenshot, box]);

  if ("png" in source) return `data:image/png;base64,${source.png}`;

  return cut;
}
