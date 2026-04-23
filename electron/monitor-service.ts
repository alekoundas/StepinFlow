import { Rectangle, screen } from "electron";
import path from "path";
import { fileURLToPath } from "url";
import { MonitorInfo, SystemMonitor } from "./shared/types.js";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

export function MonitorService() {
  const getMonitorsInfo = (): SystemMonitor => {
    // Sort left-to-right so we can accumulate physical X correctly.
    // We CANNOT compute physicalX as (logicalX * scaleFactor) because that only
    // works if all monitors share the same scaleFactor. The correct formula:
    //   physicalX[N] = sum of (logicalWidth[i] * scaleFactor[i]) for i < N
    const allDisplaysSorted = screen
      .getAllDisplays()
      .sort((a, b) => a.bounds.x - b.bounds.x);

    // Calculate virtual screen
    const logicalRect: Rectangle = getLogicalVirtualScreen(allDisplaysSorted);

    let accumPhysicalX = 0;
    const displays: MonitorInfo[] = allDisplaysSorted.map((d) => {
      d.id;
      const physicalW = Math.round(d.bounds.width * d.scaleFactor);
      const physicalH = Math.round(d.bounds.height * d.scaleFactor);
      const physicalY = Math.round(
        (d.bounds.y - logicalRect.y) * d.scaleFactor,
      );

      const info: MonitorInfo = {
        deviceId: d.label,
        logicalBounds: {
          x: d.bounds.x - logicalRect.x,
          y: d.bounds.y - logicalRect.y,
          width: d.bounds.width,
          height: d.bounds.height,
        },
        physicalBounds: {
          x: accumPhysicalX,
          y: physicalY,
          width: physicalW,
          height: physicalH,
        },
        scaleFactor: d.scaleFactor,
      };
      accumPhysicalX += physicalW;
      return info;
    });

    const physicalVirtualWidth = accumPhysicalX;
    const physicalVirtualHeight = Math.max(
      ...allDisplaysSorted.map((d) =>
        Math.round(d.bounds.height * d.scaleFactor),
      ),
    );

    return {
      displays,
      physicalVirtualWidth,
      physicalVirtualHeight,
      minVirtualX: logicalRect.x,
      minVirtualY: logicalRect.y,
      logicalVirtualWidth: logicalRect.width,
      logicalVirtualHeight: logicalRect.height,
    };
  };

  function getLogicalVirtualScreen(displays?: Electron.Display[]): Rectangle {
    if (!displays) {
      displays = screen.getAllDisplays();
    }

    const rect: Rectangle = {
      x: Math.min(...displays.map((d) => d.bounds.x)),
      y: Math.min(...displays.map((d) => d.bounds.y)),
      height: 0,
      width: 0,
    };
    const maxRight = Math.max(
      ...displays.map((d) => d.bounds.x + d.bounds.width),
    );
    const maxBottom = Math.max(
      ...displays.map((d) => d.bounds.y + d.bounds.height),
    );
    rect.width = maxRight - rect.x;
    rect.height = maxBottom - rect.y;

    return rect;
  }
  return { getMonitorsInfo, getLogicalVirtualScreen };
}
