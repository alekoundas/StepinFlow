export const AppSettingKeyEnum = {
  RECORDING_CAPTURE_WIDTH: "RECORDING_CAPTURE_WIDTH",
  RECORDING_CAPTURE_HEIGHT: "RECORDING_CAPTURE_HEIGHT",
} as const;

export type AppSettingKeyEnum =
  (typeof AppSettingKeyEnum)[keyof typeof AppSettingKeyEnum];
