export interface LookupRequestDto {
  searchText?: string;
  excludedIds?: number[];

  // Scope filters. Lookup.flowLocation uses flowId, Lookup.flowStep walks up from flowStepId.
  flowId?: number;
  flowStepId?: number;
}
