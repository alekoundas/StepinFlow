/**
 * Canvas Utilities
 *
 * Small, dependency free helpers shared by the image editor:
 *  - base64 <-> Blob/Image conversions (the screenshot arrives as base64 from .Net)
 *  - canvas creation / cloning / thumbnails
 *  - rectangle + polygon math
 */

import type { Point, Rect, Size } from "@/windows/image-editor/types";

// ============================================================================
// Encoding helpers
// ============================================================================

/**
 * Decode base64 into a Blob without building one giant JS string.
 * A full desktop screenshot can be several MB, so decode in slices.
 */
export function base64ToBlob(base64: string, mimeType = "image/png"): Blob {
  const sliceSize = 512 * 1024;
  const binary = atob(base64);
  const parts: Uint8Array[] = [];

  for (let offset = 0; offset < binary.length; offset += sliceSize) {
    const slice = binary.slice(offset, offset + sliceSize);
    const bytes = new Uint8Array(slice.length);
    for (let i = 0; i < slice.length; i++) bytes[i] = slice.charCodeAt(i);
    parts.push(bytes);
  }

  return new Blob(parts as BlobPart[], { type: mimeType });
}

/** Encode a Blob as raw base64 (no `data:` prefix) — this is what .Net expects. */
export async function blobToBase64(blob: Blob): Promise<string> {
  const buffer = new Uint8Array(await blob.arrayBuffer());
  const chunkSize = 32 * 1024;
  let binary = "";

  for (let offset = 0; offset < buffer.length; offset += chunkSize) {
    binary += String.fromCharCode(
      ...buffer.subarray(offset, offset + chunkSize),
    );
  }

  return btoa(binary);
}

/** Accepts raw base64 or a `data:` URL and resolves a decoded bitmap. */
export function loadImage(source: string): Promise<HTMLImageElement> {
  const url = source.startsWith("data:")
    ? source
    : URL.createObjectURL(base64ToBlob(source));

  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => {
      if (url.startsWith("blob:")) URL.revokeObjectURL(url);
      resolve(img);
    };
    img.onerror = () => {
      if (url.startsWith("blob:")) URL.revokeObjectURL(url);
      reject(new Error("[ImageEditor] Failed to decode image"));
    };
    img.src = url;
  });
}

/** PNG encode a canvas and return raw base64 (transparency preserved). */
export function canvasToPngBase64(canvas: HTMLCanvasElement): Promise<string> {
  return new Promise((resolve, reject) => {
    canvas.toBlob((blob) => {
      if (!blob) {
        reject(new Error("[ImageEditor] PNG encode failed"));
        return;
      }
      blobToBase64(blob).then(resolve).catch(reject);
    }, "image/png");
  });
}

// ============================================================================
// Canvas helpers
// ============================================================================

export function createCanvas(width: number, height: number): HTMLCanvasElement {
  const canvas = document.createElement("canvas");
  canvas.width = Math.max(1, Math.round(width));
  canvas.height = Math.max(1, Math.round(height));
  return canvas;
}

/** 2D context with the settings the editor always wants (no smoothing, readable). */
export function get2dContext(canvas: HTMLCanvasElement): CanvasRenderingContext2D {
  const ctx = canvas.getContext("2d", {
    willReadFrequently: true,
  }) as CanvasRenderingContext2D;
  ctx.imageSmoothingEnabled = false;
  return ctx;
}

export function cloneCanvas(source: HTMLCanvasElement): HTMLCanvasElement {
  const copy = createCanvas(source.width, source.height);
  const ctx = get2dContext(copy);
  ctx.drawImage(source, 0, 0);
  return copy;
}

/** Downscaled PNG data URL used by the history list. */
export function canvasToThumbnail(
  source: HTMLCanvasElement,
  maxWidth = 72,
  maxHeight = 48,
): string {
  const scale = Math.min(
    maxWidth / source.width,
    maxHeight / source.height,
    1,
  );
  const width = Math.max(1, Math.round(source.width * scale));
  const height = Math.max(1, Math.round(source.height * scale));

  const thumb = createCanvas(width, height);
  const ctx = thumb.getContext("2d")!;
  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = "low";
  ctx.drawImage(source, 0, 0, width, height);

  return thumb.toDataURL("image/png");
}

/** Approximate memory cost of a snapshot (RGBA). */
export function canvasByteSize(canvas: HTMLCanvasElement): number {
  return canvas.width * canvas.height * 4;
}

// ============================================================================
// Geometry helpers
// ============================================================================

/** Build a positive-size rect from two arbitrary corners. */
export function rectFromPoints(a: Point, b: Point): Rect {
  return {
    x: Math.min(a.x, b.x),
    y: Math.min(a.y, b.y),
    width: Math.abs(b.x - a.x),
    height: Math.abs(b.y - a.y),
  };
}

/** Snap to whole pixels and clip against the image bounds. */
export function clampRectToImage(rect: Rect, size: Size): Rect {
  const left = Math.max(0, Math.floor(rect.x));
  const top = Math.max(0, Math.floor(rect.y));
  const right = Math.min(size.width, Math.ceil(rect.x + rect.width));
  const bottom = Math.min(size.height, Math.ceil(rect.y + rect.height));

  return {
    x: left,
    y: top,
    width: Math.max(0, right - left),
    height: Math.max(0, bottom - top),
  };
}

export function polygonBounds(points: Point[]): Rect {
  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;

  for (const point of points) {
    minX = Math.min(minX, point.x);
    minY = Math.min(minY, point.y);
    maxX = Math.max(maxX, point.x);
    maxY = Math.max(maxY, point.y);
  }

  return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
}

export function distance(a: Point, b: Point): number {
  return Math.hypot(b.x - a.x, b.y - a.y);
}

export function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}
