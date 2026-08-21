import type { AppSettingKeyEnum } from "@/shared/enums/backend/app-setting-key-enum";

export interface AppSettingDto {
  key: AppSettingKeyEnum;
  label: string;
  description: string;

  value: string;
  minimum?: number | null;
  maximum?: number | null;
}
