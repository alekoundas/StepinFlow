import type {
  ColumnBodyOptions,
  ColumnFilterElementTemplateOptions,
} from "primereact/column";
import type { JSX } from "react";

export interface DataTableColumnDto<TEntity> {
  field: string;
  header: string;
  style: React.CSSProperties;
  sortable: boolean;

  filter: boolean;
  filterPlaceholder: string;
  filterTemplate?: (
    options: ColumnFilterElementTemplateOptions,
  ) => JSX.Element | undefined;

  body?: ((rowData: TEntity, options: ColumnBodyOptions) => any) | undefined;
  // body?: (rowData: TEntity, options?: ColumnBodyOptions) => JSX.Element;

  // editor?: boolean; // enables row edit
  // cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
  // onCellEditInit?: (event: ColumnEvent) => void;
  // onCellEditComplete?: (event: ColumnEvent) => void;
}

export class DataTableColumnDto<
  TEntity,
> implements DataTableColumnDto<TEntity> {
  field: string = "";
  header: string = "";
  style: React.CSSProperties = {};
  sortable: boolean = false;

  filter: boolean = false;
  filterPlaceholder: string = "";
  filterTemplate?: (
    options: ColumnFilterElementTemplateOptions,
  ) => JSX.Element | undefined;

  body?: ((rowData: TEntity, options: ColumnBodyOptions) => any) | undefined;
  // body?: (rowData: TEntity, options?: ColumnBodyOptions) => JSX.Element;

  // editor?: boolean; // enables row edit
  // cellEditor?: (options: ColumnEditorOptions) => React.ReactNode;
  // onCellEditInit?: (event: ColumnEvent) => void;
  // onCellEditComplete?: (event: ColumnEvent) => void;
}
