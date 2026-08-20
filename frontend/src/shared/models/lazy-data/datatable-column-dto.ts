import type { ColumnFilterElementTemplateOptions } from "primereact/column";
import type { JSX } from "react";

export interface DataTableColumnDto<T> {
  field: keyof T | string;
  header: string;
  isHidden?: boolean;
  sortable?: boolean;
  filter?: boolean; // enables text filter (matchMode: "contains")
  filterPlaceholder?: string;
  filterTemplate?: (
    options: ColumnFilterElementTemplateOptions,
  ) => JSX.Element | undefined;
  // body?: (row: T) => React.ReactNode;
  body?: (
    row: T,
    options?: { rowIndex: number; [key: string]: any },
  ) => React.ReactNode;
};
