export interface AiChatAvailabilityDto {
  isAvailable: boolean;
  model: string;
  reason: string;
  isCloud: boolean;
  canSendScreenData: boolean;
}
