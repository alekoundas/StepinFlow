/**
 * What deleting one step takes with it.
 *
 * Neither number is on the tree node, and both change the answer to "are you sure": a step shows
 * its direct children only, so a collapsed branch of twenty reads the same as one of three.
 */
export interface FlowStepDeleteImpactDto {
  /** Everything nested below it, to any depth. The database cascade takes all of it. */
  descendantCount: number;

  /** Steps that survive but lose the result they were reading. */
  referencingStepCount: number;
}
