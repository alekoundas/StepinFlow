export const gigabytes = (bytes: number): string => `${(bytes / 1024 / 1024 / 1024).toFixed(1)} GB`;

export const percentOf = (completed: number, total: number): number => {
  if (total <= 0) return 0;

  return Math.round((completed / total) * 100);
};
