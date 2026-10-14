/** Clicks that hit the same picture: one template finds every one of them. */
export interface ClickGroupDto {
  /** The clicks and drags in it, by their index among the recording's actions, in order. */
  actionIndexes: number[];

  /** In different places - the same icon on several rows - rather than one place clicked again. */
  isMoving: boolean;
}
