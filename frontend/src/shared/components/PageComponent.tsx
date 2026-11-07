import type { ReactNode } from "react";
import { classNames } from "primereact/utils";

import { PAGE_MAX_WIDTHS, type PageWidth } from "@/shared/components/page-widths";

interface Props {
  width: PageWidth;

  /** The content's own classes, such as its gap. */
  className?: string;

  /** For a page whose content fills the window's height and scrolls inside, rather than the page. */
  isFullHeight?: boolean;
  children: ReactNode;
}

/**
 * A page beside the rail. Its content grows with the window up to its width, then stays that wide in
 * the middle. The editor and an execution fill the window and do not use it.
 */
export default function PageComponent({ width, className, isFullHeight = false, children }: Props) {
  return (
    <div className={classNames("p-4", { "h-full": isFullHeight })}>
      <div
        className={classNames("mx-auto", { "h-full": isFullHeight }, className)}
        style={{ maxWidth: PAGE_MAX_WIDTHS[width] }}
      >
        {children}
      </div>
    </div>
  );
}
