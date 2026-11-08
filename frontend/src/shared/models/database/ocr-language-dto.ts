export interface OcrLanguageDto {
  // The reader's id, which is what a step saves.
  tag: string;
  displayName: string;

  // The languages the reader reads.
  languages: string;
  megabytes: number;

  // Only an installed reader can be read with. The rest are offers to download.
  isInstalled: boolean;

  // Shipped with the app, so never downloaded.
  isBuiltIn: boolean;
}
