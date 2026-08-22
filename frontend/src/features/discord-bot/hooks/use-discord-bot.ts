import { useMutation, useQueryClient } from "@tanstack/react-query";

import { backendApiService } from "@/shared/services/backend-api-service";
import type { DiscordBotDto } from "@/shared/models/database/discord-bot-dto";

export function useDiscordBotMutations() {
  const queryClient = useQueryClient();

  // The step form reads the same rows through a lookup, and its hint text quotes the rate limit,
  // so an edit here has to reach both.
  const invalidate = () => {
    queryClient.invalidateQueries({ queryKey: ["discordBots", "list"] });
    queryClient.invalidateQueries({ queryKey: ["lookup", "discordBot"] });
  };

  const createDiscordBotMutation = useMutation({
    mutationFn: (dto: DiscordBotDto) => backendApiService.DiscordBot.create(dto),
    onSuccess: invalidate,
  });

  const updateDiscordBotMutation = useMutation({
    mutationFn: (dto: DiscordBotDto) => backendApiService.DiscordBot.update(dto),
    onSuccess: invalidate,
  });

  const deleteDiscordBotMutation = useMutation({
    mutationFn: (id: number) => backendApiService.DiscordBot.delete(id),
    onSuccess: invalidate,
  });

  return {
    createDiscordBotMutation,
    updateDiscordBotMutation,
    deleteDiscordBotMutation,
  };
}
