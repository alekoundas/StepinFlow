export class DiscordBotDto {
  id: number = 0;

  name: string = "";
  webhookUrl: string = "";
  botName: string = "";
  avatarUrl: string = "";
  rateLimitSeconds: number = 10;
  createdOn?: string;
  updatedOn?: string | null;
  flowStepsCount: number = 0;

  constructor(data: Partial<DiscordBotDto> = {}) {
    Object.assign(this, { ...data });
  }
}

export const RATE_LIMIT_MIN_SECONDS = 2;
export const RATE_LIMIT_MAX_SECONDS = 300;
