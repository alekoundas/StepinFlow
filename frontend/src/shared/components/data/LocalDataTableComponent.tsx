import type { DataTableStateEvent, DataTableValue } from "primereact/datatable";
import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";

import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { useMemo, useState } from "react";

interface Props<T extends DataTableValue> {
  value: T[];
  columns: DataTableColumnDto<T>[];
  className?: string;
  initialRows?: number;
  emptyMessage?: string;
}

export function DataTableComponent<T extends DataTableValue>({
  value,
  columns,
  className,
  initialRows = 10,
  emptyMessage,
}: Props<T>) {
  const [lazyParams, setLazyParams] = useState<LazyDto>({
    first: 0,
    rows: initialRows,
    page: 0,
    sortField: undefined as string | undefined,
    sortOrder: null as 1 | -1 | null,
    filters: {} as any,
  });

  // Client-side filtering + sorting + pagination
  const processedData = useMemo(() => {
    let data = [...value];

    // Apply filters
    if (lazyParams.filters) {
      Object.entries(lazyParams.filters).forEach(([field, filter]) => {
        const filterValue = (filter as any)?.value?.toString().toLowerCase();
        if (filterValue) {
          data = data.filter((item) =>
            String((item as any)[field])
              .toLowerCase()
              .includes(filterValue),
          );
        }
      });
    }

    // Apply sorting
    if (lazyParams.sortField && lazyParams.sortOrder) {
      data.sort((a, b) => {
        const aVal = (a as any)[lazyParams.sortField!];
        const bVal = (b as any)[lazyParams.sortField!];
        return lazyParams.sortOrder! * (aVal > bVal ? 1 : aVal < bVal ? -1 : 0);
      });
    }

    return data;
  }, [value, lazyParams]);

  const paginatedData = useMemo(() => {
    const start = lazyParams.first;
    const end = start + lazyParams.rows;
    return processedData.slice(start, end);
  }, [processedData, lazyParams.first, lazyParams.rows]);

  const onPage = (e: DataTableStateEvent) => {
    setLazyParams((prev) => ({ ...prev, first: e.first, rows: e.rows }));
  };

  const onSort = (e: DataTableStateEvent) => {
    setLazyParams((prev) => ({
      ...prev,
      first: 0,
      sortField: e.sortField,
      sortOrder: e.sortOrder,
    }));
  };

  const onFilter = (e: DataTableStateEvent) => {
    setLazyParams((prev) => ({ ...prev, filters: e.filters, first: 0 }));
  };

  return (
    <div className={className}>
      <DataTable
        value={paginatedData}
        totalRecords={processedData.length}
        loading={false}
        lazy={false}
        first={lazyParams.first}
        rows={lazyParams.rows}
        sortField={lazyParams.sortField}
        sortOrder={lazyParams.sortOrder}
        filters={lazyParams.filters}
        onPage={onPage}
        onSort={onSort}
        onFilter={onFilter}
        className={className}
        emptyMessage={emptyMessage}
      >
        {columns.map((col) => (
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
