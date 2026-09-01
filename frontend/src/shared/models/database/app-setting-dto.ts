import type {
  AppSettingKeyEnum,
  AppSettingKindEnum,
} from "@/shared/enums/backend/app-setting-key-enum";

export interface AppSettingDto {
  key: AppSettingKeyEnum;
  kind: AppSettingKindEnum;
  label: string;
  description: string;

  value: string;
  minimum?: number | null;
  maximum?: number | null;

  /** CHOICE only: what the dropdown offers. */
  options: string[];

  /** SECRET only: whether one is stored. The value itself never comes back. */
  isSet: boolean;
}
