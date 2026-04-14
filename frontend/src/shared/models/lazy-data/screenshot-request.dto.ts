export class ScreenshotRequestDto {
  flowSearchAreaId?: number;
  isFullScreen: boolean = false;
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
