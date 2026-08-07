// Flow ids and FlowStep ids are separate sequences, so isFlow says which one `id` is.
export interface TreeNodeRequestDto {
  id: number;
  isFlow: boolean;
}
