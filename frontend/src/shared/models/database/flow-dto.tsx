import type { FlowAreaDto } from "@/shared/models/database/flow-area-dto";
import type { FlowPointDto } from "@/shared/models/database/flow-point-dto";
import type { FlowStepDto } from "@/shared/models/database/flow-step-dto";
import { FlowViewportDto } from "@/shared/models/database/flow-viewport-dto";
import type { FlowCsvColumnDto } from "@/shared/models/database/flow-csv-column-dto";

export class FlowDto {
  id: number = 0;
  name: string = "";

  /** What it does, in as many lines as it takes. The list shows the first. */
  description: string = "";

  /** A flow meant to be called by another rather than started on its own. One way, never unset. */
  isSubFlow: boolean = false;

  /** A QA test rather than an automation, chosen at the recording setup. */
  isQaTest: boolean = false;

  createdOn?: string;
  updatedOn?: string | null;

  // Projected for the list. Zero on anything the form loaded.
  stepCount: number = 0;
  areaCount: number = 0;
  pointCount: number = 0;

  /** Sub-flows only: how many distinct flows invoke this one. */
  callerCount: number = 0;

  flowSteps: FlowStepDto[] = [];
  flowAreas: FlowAreaDto[] = [];
  flowPoints: FlowPointDto[] = [];

  /** The sizes this flow is expected to pass at. One execution per size. */
  flowViewports: FlowViewportDto[] = [];

  /** The values its steps type in, written {{name}}. */
  flowCsvColumns: FlowCsvColumnDto[] = [];

  constructor(data: Partial<FlowDto> = {}) {
    Object.assign(this, {
      ...data,
    });
  }
}
