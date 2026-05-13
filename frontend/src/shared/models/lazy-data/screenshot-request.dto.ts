import type { ScreenshotFormatEnum } from "@/shared/enums/backend/business/screenshot-format-enum";

export class ScreenshotRequestDto {
  formatType: ScreenshotFormatEnum = "JPEG";
  jpegQuality: number = 0;

  flowSearchAreaId?: number;
  captureVirtualScreen: boolean = false;
  captureMonitor: string = "";
  captureAppWindow: string = "";

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
