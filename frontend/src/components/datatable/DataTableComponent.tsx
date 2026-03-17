import type { DataTableStateEvent } from "primereact/datatable";
import type { DataTableDto } from "@/models/data-table/datatable-dto";
import type { DataTableColumnDto } from "@/models/data-table/datatable-column-dto";
import type { DataTableResponseDto } from "@/models/data-table/datatable-response-dto";

import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { useState, useEffect, useCallback } from "react";
import { ProgressBar } from "primereact/progressbar";

// export type DataTableLazyParams = {
//   first: number;
//   rows: number;
//   page: number;
//   sortField?: string;
//   sortOrder?: 1 | -1 | null;
//   filters?: Record<string, any>;
// };

// export type DataTableResponse<T> = {
//   data: T[];
//   totalRecords: number;
// };

// type ColumnConfig<T> = {
//   field: keyof T | string;
//   header: string;
//   sortable?: boolean;
//   filter?: boolean; // enables text filter (matchMode: "contains")
//   body?: (row: T) => React.ReactNode;
// };

type Props<T> = {
  columns: DataTableColumnDto<T>[];
  loadData: (params: DataTableDto) => Promise<DataTableResponseDto<T>>;
  actionsColumn?: DataTableColumnDto<T>; // optional extra column for edit/delete/etc.
  className?: string;
};

export function DataTableComponent<T>({
  columns,
  loadData,
  actionsColumn,
  className = "",
}: Props<T>) {
  const [data, setData] = useState<T[]>([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [loading, setLoading] = useState(false);

  const [lazyParams, setLazyParams] = useState<DataTableDto>({
    first: 0,
    rows: 10,
    page: 0,
    sortField: undefined,
    sortOrder: null,
    filters: {},
  });

  const loadLazyData = useCallback(async () => {
    setLoading(true);
    try {
      const response = await loadData(lazyParams);
      setData(response.data);
      setTotalRecords(response.totalRecords);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [lazyParams, loadData]);

  useEffect(() => {
    loadLazyData();
  }, [loadLazyData]);

  const onPage = (event: DataTableStateEvent) => {
    setLazyParams((prev) => ({
      ...prev,
      first: event.first,
      rows: event.rows,
      page: event.page ?? 0,
    }));
  };

  const onSort = (event: DataTableStateEvent) => {
    setLazyParams((prev) => ({
      ...prev,
      sortField: event.sortField,
      sortOrder: event.sortOrder,
    }));
  };

  const onFilter = (event: DataTableStateEvent) => {
    setLazyParams((prev) => ({ ...prev, filters: event.filters }));
  };

  const allColumns = [...columns, ...(actionsColumn ? [actionsColumn] : [])];

  return (
    <div className={className}>
      {loading && (
        <ProgressBar
          mode="indeterminate"
          style={{ height: "3px" }}
        />
      )}

      <DataTable
        value={data}
        lazy
        paginator
        rows={lazyParams.rows}
        first={lazyParams.first}
        totalRecords={totalRecords}
        onPage={onPage}
        onSort={onSort}
        onFilter={onFilter}
        sortField={lazyParams.sortField}
        sortOrder={lazyParams.sortOrder}
        filters={lazyParams.filters}
        filterDisplay="row"
        loading={loading}
        stripedRows
        showGridlines
        rowsPerPageOptions={[5, 10, 20, 50]}
        emptyMessage="No flows found."
      >
        {allColumns.map((col) => (
          <Column
            key={String(col.field)}
            field={String(col.field)}
            header={col.header}
            sortable={col.sortable}
            filter={col.filter}
            body={col.body}
          />
        ))}
      </DataTable>
    </div>
  );
}
