import type { TreeNodeDto } from "@/shared/models/tree-node-dto";

/**
 * Tree key for an entity id. Must stay in sync with TreeNodeDto.BuildKey on the backend.
 *
 * Flow ids and FlowStep ids are separate sequences, so a raw id would make Flow 5 and FlowStep 5
 * the same node as far as PrimeReact selection and expansion are concerned.
 */
const buildTreeNodeKey = (id: number, isFlow: boolean): string =>
  isFlow ? `flow-${id}` : `step-${id}`;

/** The rows under a disabled step: skipped with it and dimmed with it, though not disabled themselves. */
const keysUnderDisabled = (
  nodes: TreeNodeDto[],
  isUnder = false,
  keys = new Set<string>(),
): Set<string> => {
  for (const node of nodes) {
    if (isUnder) keys.add(node.key);
    keysUnderDisabled(node.children ?? [], isUnder || node.isDisabled, keys);
  }

  return keys;
};

export const TreeNodeKeys = {
  buildTreeNodeKey,
  keysUnderDisabled,
};
