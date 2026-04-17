import type { ScreenshotFormatEnum } from "@/shared/enums/backend/business/screenshot-format-enum";

export class ScreenshotRequestDto {
  flowSearchAreaId?: number;
  isVirtualScreen: boolean = false;
  formatType: ScreenshotFormatEnum = "JPEG";
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
