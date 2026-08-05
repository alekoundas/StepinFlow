/**
 * useViewTransform — zoom & pan state for the editor viewport.
 *
 *   viewport = image * scale + offset
 *   image    = (viewport - offset) / scale
 *
 * Zooming always keeps a chosen viewport anchor (usually the cursor) pinned to
 * the same image pixel, which is what makes wheel-zoom feel right.
 *
 * scale and offset live in one state object so every update is a single pure
 * reducer step (nested setState calls would double-apply under StrictMode).
 */

import { useCallback, useState } from "react";
import type { Point, Size, ViewTransform } from "@/windows/image-editor/types";
import { clamp } from "@/windows/image-editor/utils/canvas-utils";

export const MIN_SCALE = 0.02;
export const MAX_SCALE = 64;
const FIT_PADDING = 24;

const INITIAL: ViewTransform = { scale: 1, offset: { x: 0, y: 0 } };

/** Zoom around an anchor expressed in viewport pixels. */
function zoomAround(
  view: ViewTransform,
  nextScale: number,
  anchor: Point,
): ViewTransform {
  const scale = clamp(nextScale, MIN_SCALE, MAX_SCALE);
  if (scale === view.scale) return view;

  const ratio = scale / view.scale;
  return {
    scale,
    offset: {
      x: anchor.x - (anchor.x - view.offset.x) * ratio,
      y: anchor.y - (anchor.y - view.offset.y) * ratio,
    },
  };
}

export function useViewTransform() {
  const [view, setView] = useState<ViewTransform>(INITIAL);
  const { scale, offset } = view;

  const screenToImage = useCallback(
    (point: Point): Point => ({
      x: (point.x - offset.x) / scale,
      y: (point.y - offset.y) / scale,
    }),
    [offset.x, offset.y, scale],
  );

  const imageToScreen = useCallback(
    (point: Point): Point => ({
      x: point.x * scale + offset.x,
      y: point.y * scale + offset.y,
    }),
    [offset.x, offset.y, scale],
  );

  /** Absolute zoom level, keeping `anchor` (viewport px) fixed. */
  const zoomTo = useCallback((nextScale: number, anchor: Point) => {
    setView((prev) => zoomAround(prev, nextScale, anchor));
  }, []);

  /** Relative zoom (wheel, +/- buttons). */
  const zoomBy = useCallback((factor: number, anchor: Point) => {
    setView((prev) => zoomAround(prev, prev.scale * factor, anchor));
  }, []);

  const panBy = useCallback((dx: number, dy: number) => {
    setView((prev) => ({
      scale: prev.scale,
      offset: { x: prev.offset.x + dx, y: prev.offset.y + dy },
    }));
  }, []);

  /** Centre the viewport on an image-space point. */
  const centerOn = useCallback((imagePoint: Point, viewport: Size) => {
    setView((prev) => ({
      scale: prev.scale,
      offset: {
        x: viewport.width / 2 - imagePoint.x * prev.scale,
        y: viewport.height / 2 - imagePoint.y * prev.scale,
      },
    }));
  }, []);

  /** Scale the whole image into the viewport and centre it. */
  const fit = useCallback((image: Size, viewport: Size) => {
    if (!image.width || !image.height || !viewport.width || !viewport.height) {
      return;
    }

    const scaleToFit = clamp(
      Math.min(
        (viewport.width - FIT_PADDING * 2) / image.width,
        (viewport.height - FIT_PADDING * 2) / image.height,
      ),
      MIN_SCALE,
      MAX_SCALE,
    );

    setView({
      scale: scaleToFit,
      offset: {
        x: (viewport.width - image.width * scaleToFit) / 2,
        y: (viewport.height - image.height * scaleToFit) / 2,
      },
    });
  }, []);

  /** 1:1 zoom, centred. */
  const actualSize = useCallback((image: Size, viewport: Size) => {
    setView({
      scale: 1,
      offset: {
        x: (viewport.width - image.width) / 2,
        y: (viewport.height - image.height) / 2,
      },
    });
  }, []);

  /** Zoom to an absolute level around the middle of the viewport. */
  const zoomToCenter = useCallback((nextScale: number, viewport: Size) => {
    const anchor = { x: viewport.width / 2, y: viewport.height / 2 };
    setView((prev) => zoomAround(prev, nextScale, anchor));
  }, []);

  return {
    scale,
    offset,
    screenToImage,
    imageToScreen,
    zoomTo,
    zoomBy,
    zoomToCenter,
    panBy,
    centerOn,
    fit,
    actualSize,
  };
}
