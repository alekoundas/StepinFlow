import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { BroadcastTypeEnum } from "../../../../../electron/shared/types";

import { ElectronApiService } from "@/shared/services/electron-api-service";
import { backendApiService } from "@/shared/services/backend-api-service";
import { useWorkflowStore } from "@/features/workflow/store/workflow-store";
import { GitChangeKindEnum } from "@/shared/enums/backend/git-change-kind-enum";
import type { RepositoryCommitRequestDto, RepositoryConnectRequestDto } from "@/shared/models/repository-dtos";

export const repositoryKeys = {
  all: ["repository"] as const,
  status: () => ["repository", "status"] as const,
  changes: () => ["repository", "changes"] as const,
  flowChanges: (flowId: number) => ["repository", "flowChanges", flowId] as const,
  flowHistory: (flowId: number) => ["repository", "flowHistory", flowId] as const,
  flowCommit: (flowId: number, sha: string) => ["repository", "flowCommit", flowId, sha] as const,
  branches: () => ["repository", "branches"] as const,
} as const;

/**
 * The branch and the flows with changes not committed yet. The repository is made at startup,
 * in the background, so the first asks can fail; the broadcast when it is ready asks again.
 */
export function useRepositoryStatus() {
  return useQuery({
    queryKey: repositoryKeys.status(),
    queryFn: () => backendApiService.Repository.getStatus(),
    retry: 5,
    retryDelay: 1000,
  });
}

/** Every file not committed yet. Only while the commit dialog is open. */
export function useRepositoryChanges(enabled: boolean) {
  return useQuery({
    queryKey: repositoryKeys.changes(),
    queryFn: () => backendApiService.Repository.getChanges(),
    enabled,
    staleTime: 0,
  });
}

export function useFlowRepositoryChanges(flowId: number) {
  return useQuery({
    queryKey: repositoryKeys.flowChanges(flowId),
    queryFn: () => backendApiService.Repository.getFlowChanges(flowId),
    staleTime: 0,
  });
}

export function useFlowRepositoryHistory(flowId: number) {
  return useQuery({
    queryKey: repositoryKeys.flowHistory(flowId),
    queryFn: () => backendApiService.Repository.getFlowHistory(flowId),
  });
}

/** A commit never changes, so it is fetched once. */
export function useFlowRepositoryCommit(flowId: number, sha: string | null) {
  return useQuery({
    queryKey: sha ? repositoryKeys.flowCommit(flowId, sha) : ["repository", "flowCommit", "none"],
    queryFn: () => backendApiService.Repository.getFlowCommit({ flowId, sha: sha! }),
    enabled: !!sha,
    staleTime: Infinity,
  });
}

export function useCommitRepository() {
  const queryClient = useQueryClient();

  // Awaited, so the dialog closes on a status that already says Committed.
  return useMutation({
    mutationFn: (dto: RepositoryCommitRequestDto) => backendApiService.Repository.commit(dto),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: repositoryKeys.all }),
  });
}

/** Discard and Restore. */
export function useBringBackFlow(flowId: number) {
  const reload = useReloadImported();

  const discard = useMutation({
    mutationFn: () => backendApiService.Repository.discard(flowId),
    onSuccess: reload,
  });

  const restore = useMutation({
    mutationFn: (sha: string) => backendApiService.Repository.restore({ flowId, sha }),
    onSuccess: reload,
  });

  return { discard, restore };
}

/** Only while the branch menu is open. */
export function useRepositoryBranches(enabled: boolean) {
  return useQuery({
    queryKey: repositoryKeys.branches(),
    queryFn: () => backendApiService.Repository.getBranches(),
    enabled,
    staleTime: 0,
  });
}

export function useCreateBranch() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (name: string) => backendApiService.Repository.createBranch(name),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: repositoryKeys.all }),
  });
}

/** A flow the branch does not have is hidden, so the editor leaves it for the list it was in. */
export function useSwitchBranch(flowId: number | undefined) {
  const reload = useReloadImported();
  const navigate = useNavigate();

  return useMutation({
    mutationFn: (name: string) => backendApiService.Repository.switchBranch(name),
    onSuccess: async (result) => {
      const hidden = result.flows.find((x) => x.flowId === flowId && x.change === GitChangeKindEnum.DELETED);
      if (hidden) navigate(hidden.isSubFlow ? "/sub-flows" : "/flows");

      await reload();
    },
  });
}

/** A remote with history comes back with what a pull would do, for the person to agree to. */
export function useConnectRemote() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (dto: RepositoryConnectRequestDto) => backendApiService.Repository.connect(dto),
    onSuccess: () => queryClient.invalidateQueries(),
  });
}

/** Asks the network, so it is a mutation: it runs when Pull is pressed, never on its own. */
export function usePreviewPull() {
  return useMutation({
    mutationFn: () => backendApiService.Repository.previewPull(),
  });
}

export function usePull() {
  const reload = useReloadImported();

  return useMutation({
    mutationFn: (remoteSha: string) => backendApiService.Repository.pull(remoteSha),
    onSuccess: reload,
  });
}

export function usePush() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => backendApiService.Repository.push(),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: repositoryKeys.all }),
  });
}

/**
 * After flows are imported over themselves - a discard, a restore, a switch, a pull - every step,
 * area and template they show is a new row: everything is read again, and the tree starts over with
 * nothing selected or copied.
 */
function useReloadImported() {
  const queryClient = useQueryClient();
  const { setSelectedTreeNode, setTreeRefreshTrigger, setCopiedStep } = useWorkflowStore();

  return async () => {
    setSelectedTreeNode(undefined);
    setCopiedStep(undefined);
    setTreeRefreshTrigger({ id: -1, isFlow: true });
    await queryClient.invalidateQueries();
  };
}

/**
 * Everything shown about the repository is read again once the backend says it changed: after it
 * writes the flows a save touched, half a second later, and after a commit. Mounted once, in the
 * app's layout.
 */
export function useRepositoryEvents() {
  const queryClient = useQueryClient();

  useEffect(() => {
    const unsubscribe = ElectronApiService.backendApi.OnBroadcast((message) => {
      if (message.type !== BroadcastTypeEnum.REPOSITORY_CHANGED) return;

      queryClient.invalidateQueries({ queryKey: repositoryKeys.all });
    });

    return () => {
      unsubscribe?.();
    };
  }, [queryClient]);
}
