export class ScreenshotRequestDto {
  flowSearchAreaId?: number;
  isVirtualScreen: boolean = false;
  isJPEG: boolean = false;
  jpegQuality: number = 0;
  locationX: number = 0;
  locationY: number = 0;
  width: number = 0;
  height: number = 0;

  constructor(data: Partial<ScreenshotRequestDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
