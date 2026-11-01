import { Tag } from "primereact/tag";
import { classNames } from "primereact/utils";

import IconComponent from "@/shared/components/IconComponent";
import LabelComponent from "@/shared/components/LabelComponent";
import type { FlowStepTemplateDto } from "@/shared/models/database/flow-step-template-dto";
import type { ImageSearchTestImageDto } from "@/shared/models/database/image-search-test-result-dto";
import { hitsOf, nearMissOf } from "@/features/flow-step/components/forms/search-image/image-search-test";

interface Props {
  name: string;
  image: ImageSearchTestImageDto;
  tested: FlowStepTemplateDto;
  // The accuracy on the form now, which a suggestion may have moved since the test.
  accuracy: number;
  isUsed: boolean;
  isSelected: boolean;
  onSelect: () => void;
}

// A score drawn on a 0 to 1 line. Below 0 is possible and means nothing alike, so it sits at 0.
const at = (score: number) => `${Math.min(1, Math.max(0, score)) * 100}%`;

export function FlowStepSearchImageTestRowComponent({
  name,
  image,
  tested,
  accuracy,
  isUsed,
  isSelected,
  onSelect,
}: Props) {
  const hits = hitsOf(image);
  const nearMiss = nearMissOf(image);
  const lowestHit = hits.length > 0 ? Math.min(...hits.map((x) => x.score)) : null;
  const isMoved = accuracy !== tested.accuracy;

  let verdict = <Tag severity="secondary" value="nothing close" />;
  if (lowestHit !== null)
    verdict = (
      <Tag
        severity="success"
        value={hits.length > 1 ? `${hits.length} found · ${image.bestScore.toFixed(2)}` : `found ${image.bestScore.toFixed(2)}`}
      />
    );
  else if (nearMiss !== null)
    verdict = <Tag severity="danger" value={`closest ${nearMiss.score.toFixed(2)}`} />;

  return (
    <div
      role="button"
      tabIndex={0}
      onClick={onSelect}
      onKeyDown={(e) => (e.key === "Enter" || e.key === " ") && onSelect()}
      className={classNames("flex gap-2 p-2 border-round cursor-pointer surface-card", {
        "border-2 border-primary": isSelected,
        "border-1 surface-border": !isSelected,
      })}
    >
      {tested.flowTemplate.templateImage ? (
        <img
          src={`data:image/png;base64,${tested.flowTemplate.templateImage}`}
          alt=""
          className="flex-shrink-0 border-round-sm"
          style={{ width: 44, height: 44, objectFit: "contain", imageRendering: "pixelated" }}
        />
      ) : (
        <IconComponent name="image" />
      )}

      <div className="flex flex-column flex-1 gap-1" style={{ minWidth: 0 }}>
        <div className="flex align-items-center gap-2">
          <span className="text-sm font-semibold white-space-nowrap overflow-hidden text-overflow-ellipsis">
            {name}
          </span>
          {isUsed && <Tag value="used" />}
        </div>

        <div className="flex align-items-center gap-2">
          {verdict}
          <LabelComponent
            text={`needs ${tested.accuracy.toFixed(2)}`}
            size="xs"
            color="secondary"
          />
        </div>

        {/* The gap between the lowest hit and the closest wrong place is where the bar is safe. */}
        <div
          className="relative border-round mt-2"
          style={{ height: 8, background: "var(--surface-200)" }}
        >
          {lowestHit !== null && nearMiss !== null && (
            <div
              className="absolute top-0 bottom-0 border-round"
              style={{
                left: at(nearMiss.score),
                width: `calc(${at(lowestHit)} - ${at(nearMiss.score)})`,
                background: "var(--green-100)",
              }}
            />
          )}
          <Tick score={tested.accuracy} colour="var(--text-color)" isBar />
          {isMoved && <Tick score={accuracy} colour="var(--primary-color)" isBar />}
          {nearMiss !== null && <Tick score={nearMiss.score} colour="var(--orange-400)" />}
          {hits.map((x, index) => (
            <Tick key={index} score={x.score} colour="var(--green-500)" />
          ))}
        </div>

        <div className="flex justify-content-between text-xs text-color-secondary">
          <span>0</span>
          <span>{isMoved ? `${accuracy.toFixed(2)} on the form, not tested yet` : `bar ${tested.accuracy.toFixed(2)}`}</span>
          <span>1</span>
        </div>
      </div>
    </div>
  );
}

interface TickProps {
  score: number;
  colour: string;
  isBar?: boolean;
}

// A bar is a thin line across the strip; a score is a short block on it.
function Tick({ score, colour, isBar = false }: TickProps) {
  return (
    <div
      className="absolute border-round-sm"
      style={{
        left: at(score),
        top: isBar ? -3 : -2,
        width: isBar ? 2 : 4,
        height: isBar ? 14 : 12,
        marginLeft: isBar ? -1 : -2,
        background: colour,
      }}
    />
  );
}
