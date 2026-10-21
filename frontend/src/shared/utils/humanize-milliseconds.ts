/** Milliseconds are what gets stored; nobody reads 5400000 and thinks "an hour and a half". */
export const humanizeMilliseconds = (value: number | undefined | null): string => {
  if (!value || value <= 0) return "";

  const units: [number, string][] = [
    [86_400_000, "d"],
    [3_600_000, "h"],
    [60_000, "m"],
    [1_000, "s"],
  ];

  let remaining = value;
  const parts: string[] = [];

  for (const [size, suffix] of units) {
    const count = Math.floor(remaining / size);
    if (count > 0) {
      parts.push(`${count}${suffix}`);
      remaining -= count * size;
    }
  }

  if (remaining > 0) parts.push(`${remaining}ms`);

  return parts.join(" ");
};
