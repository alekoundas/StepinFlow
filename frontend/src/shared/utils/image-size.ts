/**
 * Width and height out of a PNG header.
 *
 * A png says its size in the IHDR chunk at a fixed offset: eight bytes of signature, four of length
 * and four of "IHDR", then the two dimensions as big-endian 32-bit numbers. Reading those 24 bytes
 * beats decoding the image, and beats asking the image editor to start reporting a size.
 */
export const readPngSize = (
  base64: string,
): { width: number; height: number } => {
  try {
    // 64 base64 characters is 48 bytes: well past the 24 needed, and a clean multiple of four.
    const header = atob(base64.slice(0, 64));

    const byteAt = (index: number): number => header.charCodeAt(index);
    const numberAt = (offset: number): number =>
      byteAt(offset) * 0x1000000 +
      (byteAt(offset + 1) << 16) +
      (byteAt(offset + 2) << 8) +
      byteAt(offset + 3);

    // Not a png, so the offsets would be nonsense.
    if (byteAt(1) !== 0x50 || byteAt(2) !== 0x4e || byteAt(3) !== 0x47)
      return { width: 0, height: 0 };

    return { width: numberAt(16), height: numberAt(20) };
  } catch {
    return { width: 0, height: 0 };
  }
};

/**
 * Where to click on a template when nobody has said.
 *
 * The click point is an offset from the top-left corner of whatever the search matched, so left at
 * 0,0 that corner is what gets clicked - the edge of a bounding box, which is a border or blank
 * space far more often than it is the thing itself. The middle is the only sane default, and it is
 * what someone drawing a box around something means by drawing it.
 */
export const centreClickOffset = (
  base64: string,
): { clickOffsetX: number; clickOffsetY: number } => {
  const size = readPngSize(base64);

  return {
    clickOffsetX: Math.round(size.width / 2),
    clickOffsetY: Math.round(size.height / 2),
  };
};
