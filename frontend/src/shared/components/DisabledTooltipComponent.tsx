import { useRef } from "react";
import type { ReactNode } from "react";
import { Tooltip } from "primereact/tooltip";
import type { TooltipOptions } from "primereact/tooltip/tooltipoptions";

interface Props {
  /** Why the control is off. Null shows nothing. */
  tooltip: string | null | undefined;
  position?: TooltipOptions["position"];
  children: ReactNode;
}

/**
 * A tooltip that shows over a disabled control too.
 *
 * PrimeReact's showOnDisabled moves the button into a div of its own, behind React's back, and
 * React then cannot remove the button when its panel changes: "Failed to execute 'removeChild' on
 * 'Node'". This wrapper is React's own, so nothing is moved. A disabled PrimeReact control takes no
 * pointer events, so hovering it reaches the wrapper.
 */
export default function DisabledTooltipComponent({ tooltip, position = "bottom", children }: Props) {
  const wrapper = useRef<HTMLSpanElement>(null);

  return (
    <>
      <span
        ref={wrapper}
        className="inline-flex"
      >
        {children}
      </span>
      {tooltip && (
        <Tooltip
          target={wrapper}
          content={tooltip}
          position={position}
        />
      )}
    </>
  );
}
