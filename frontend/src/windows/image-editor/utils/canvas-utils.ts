/**
 * Canvas Utilities - Helper functions for canvas operations
 *
 * Provides reusable functions for:
 *  - Image data conversions (DataURL ↔ Uint8Array)
 *  - Canvas state management
 *  - Image processing helpers
 */

/**
 * Convert data URL to Uint8Array (PNG bytes)
 * Used for exporting edited image
 */
export function dataURLtoUint8Array(dataUrl: string): Uint8Array {
  const bstr = atob(dataUrl.split(",")[1]);
  const n = bstr.length;
  const u8arr = new Uint8Array(n);

  for (let i = 0; i < n; i++) {
    u8arr[i] = bstr.charCodeAt(i);
  }

  return u8arr;
}

/**
 * Convert Uint8Array to data URL
 * Used for displaying binary image data
 */
export function uint8ArrayToDataURL(
  imageData: Uint8Array,
  mimeType = "image/png",
): string {
  // Convert Uint8Array → binary string efficiently
  const binaryString = Array.from(imageData)
    .map((byte) => String.fromCharCode(byte))
    .join("");

  const base64 = btoa(binaryString);
  const result = `data:${mimeType};base64,${base64}`;
  return result;
}

export function base64ToUint8Array(base64: string): Uint8Array {
  const binaryString = atob(base64);
  const len = binaryString.length;
  const bytes = new Uint8Array(len);
  
  for (let i = 0; i < len; i++) {
    bytes[i] = binaryString.charCodeAt(i);
  }
  return bytes;
}

/**
 * Get canvas as PNG data URL (for quick preview)
 */
export function canvasToDataURL(canvas: HTMLCanvasElement): string {
  return canvas.toDataURL("image/png");
}

/**
 * Create a copy of ImageData for safe manipulation
 */
export function copyImageData(imageData: ImageData): ImageData {
  const newImageData = new ImageData(imageData.width, imageData.height);
  newImageData.data.set(imageData.data);
  return newImageData;
}

/**
 * Get pixel color from ImageData (RGBA)
 */
export function getPixel(
  imageData: ImageData,
  x: number,
  y: number,
): [r: number, g: number, b: number, a: number] {
  const idx = (y * imageData.width + x) * 4;
  const data = imageData.data;
  return [data[idx], data[idx + 1], data[idx + 2], data[idx + 3]];
}

/**
 * Set pixel color in ImageData (RGBA)
 */
export function setPixel(
  imageData: ImageData,
  x: number,
  y: number,
  r: number,
  g: number,
  b: number,
  a: number,
): void {
  const idx = (y * imageData.width + x) * 4;
  const data = imageData.data;
  data[idx] = r;
  data[idx + 1] = g;
  data[idx + 2] = b;
  data[idx + 3] = a;
}

/**
 * Flood fill algorithm (used for fill-based crop operations)
 * Returns set of pixel indices that match the start color
 */
export function floodFill(
  imageData: ImageData,
  startX: number,
  startY: number,
  tolerance = 0,
): Set<number> {
  const width = imageData.width;
  const height = imageData.height;
  const visited = new Set<number>();
  const queue: Array<{ x: number; y: number }> = [];

  const startColor = getPixel(imageData, startX, startY);
  queue.push({ x: startX, y: startY });

  while (queue.length > 0) {
    const { x, y } = queue.shift()!;
    const idx = y * width + x;

    if (visited.has(idx) || x < 0 || x >= width || y < 0 || y >= height) {
      continue;
    }

    const pixelColor = getPixel(imageData, x, y);
    const diff =
      Math.abs(pixelColor[0] - startColor[0]) +
      Math.abs(pixelColor[1] - startColor[1]) +
      Math.abs(pixelColor[2] - startColor[2]);

    if (diff <= tolerance) {
      visited.add(idx);
      queue.push({ x: x + 1, y });
      queue.push({ x: x - 1, y });
      queue.push({ x, y: y + 1 });
      queue.push({ x, y: y - 1 });
    }
  }

  return visited;
}

/**
 * Blur effect (simple box blur)
 * radius: blur radius in pixels
 */
export function blurImageData(imageData: ImageData, radius: number): ImageData {
  const result = copyImageData(imageData);
  const width = imageData.width;
  const height = imageData.height;
  const data = result.data;
  const origData = imageData.data;

  for (let y = radius; y < height - radius; y++) {
    for (let x = radius; x < width - radius; x++) {
      let r = 0,
        g = 0,
        b = 0,
        a = 0,
        count = 0;

      for (let dy = -radius; dy <= radius; dy++) {
        for (let dx = -radius; dx <= radius; dx++) {
          const idx = ((y + dy) * width + (x + dx)) * 4;
          r += origData[idx];
          g += origData[idx + 1];
          b += origData[idx + 2];
          a += origData[idx + 3];
          count++;
        }
      }

      const idx = (y * width + x) * 4;
      data[idx] = Math.round(r / count);
      data[idx + 1] = Math.round(g / count);
      data[idx + 2] = Math.round(b / count);
      data[idx + 3] = Math.round(a / count);
    }
  }

  return result;
}

/**
 * Invert colors (RGB only, preserve alpha)
 */
export function invertImageData(imageData: ImageData): ImageData {
  const result = copyImageData(imageData);
  const data = result.data;

  for (let i = 0; i < data.length; i += 4) {
    data[i] = 255 - data[i]; // R
    data[i + 1] = 255 - data[i + 1]; // G
    data[i + 2] = 255 - data[i + 2]; // B
    // data[i + 3] = alpha unchanged
  }

  return result;
}

/**
 * Adjust brightness
 * factor: 0-2 (0.5 = darker, 1 = normal, 2 = brighter)
 */
export function adjustBrightness(
  imageData: ImageData,
  factor: number,
): ImageData {
  const result = copyImageData(imageData);
  const data = result.data;

  for (let i = 0; i < data.length; i += 4) {
    data[i] = Math.min(255, Math.round(data[i] * factor));
    data[i + 1] = Math.min(255, Math.round(data[i + 1] * factor));
    data[i + 2] = Math.min(255, Math.round(data[i + 2] * factor));
  }

  return result;
}

/**
 * Check if a point is inside a polygon (ray casting algorithm)
 */
export function isPointInPolygon(
  point: { x: number; y: number },
  polygon: Array<{ x: number; y: number }>,
): boolean {
  const x = point.x;
  const y = point.y;
  let inside = false;

  for (let i = 0, j = polygon.length - 1; i < polygon.length; j = i++) {
    const xi = polygon[i].x,
      yi = polygon[i].y;
    const xj = polygon[j].x,
      yj = polygon[j].y;

    const intersect =
      yi > y !== yj > y && x < ((xj - xi) * (y - yi)) / (yj - yi) + xi;
    if (intersect) inside = !inside;
  }

  return inside;
}
