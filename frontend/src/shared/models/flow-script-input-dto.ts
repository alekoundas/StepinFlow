/** An input as a script declares it. A secret has no value in the file. */
export interface FlowScriptInputDto {
  name: string;
  defaultValue: string;
  isSecret: boolean;
}
