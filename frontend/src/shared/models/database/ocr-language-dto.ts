export interface OcrLanguageDto {
  tag: string;
  displayName: string;

  // Only an installed language can be read. The rest are offers to install.
  isInstalled: boolean;
}
