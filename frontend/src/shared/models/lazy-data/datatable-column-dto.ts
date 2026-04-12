// import type {
//   ColumnBodyOptions,
//   ColumnFilterElementTemplateOptions,
// } from "primereact/column";
// import type { JSX } from "react";

import type { ColumnFilterElementTemplateOptions } from "primereact/column";
import type { JSX } from "react";

// export interface IDataTableColumnDto<TEntity> {
//   field: string;
//   header: string;
//   style: React.CSSProperties;
//   sortable: boolean;

//   filter: boolean;
//   filterPlaceholder: string;
//   filterTemplate?: (
//     options: ColumnFilterElementTemplateOptions,
//   ) => JSX.Element | undefined;

//   body?: ((rowData: TEntity, options: ColumnBodyOptions) => any) | undefined;
//   // body?: (rowData: TEntity, options?: ColumnBodyOptions) => JSX.Element;

//   // editor?: boolean; // enables row edit
//   // cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
//   // onCellEditInit?: (event: ColumnEvent) => void;
//   // onCellEditComplete?: (event: ColumnEvent) => void;
// }

// export class DataTableColumnDto<
//   TEntity,
// > implements IDataTableColumnDto<TEntity> {
//   field: string = "";
//   header: string = "";
//   style: React.CSSProperties = {};
//   sortable: boolean = false;

//   filter: boolean = false;
//   filterPlaceholder: string = "";
//   filterTemplate?: (
//     options: ColumnFilterElementTemplateOptions,
//   ) => JSX.Element | undefined;

//   body?: ((rowData: TEntity, options: ColumnBodyOptions) => any) | undefined;
//   // body?: (rowData: TEntity, options?: ColumnBodyOptions) => JSX.Element;

//   // editor?: boolean; // enables row edit
//   // cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
//   // onCellEditInit?: (event: ColumnEvent) => void;
//   // onCellEditComplete?: (event: ColumnEvent) => void;
// }

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
