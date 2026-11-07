import type { AutoUpdaterStatus } from "../../../../electron/shared/types";

const megabytes = (bytes: number): number => Math.round(bytes / 1024 / 1024);

/** "12 of 98 MB". */
export const downloadedOf = (status: AutoUpdaterStatus): string =>
  `${megabytes(status.transferredBytes)} of ${megabytes(status.totalBytes)} MB`;
