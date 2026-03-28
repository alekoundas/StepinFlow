import type { DataTableStateEvent, DataTableValue } from "primereact/datatable";
import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";

import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { useState, useEffect, useCallback } from "react";
import { ProgressBar } from "primereact/progressbar";
import { useFlowStore } from "@/features/flow/store/flow-store";

interface Props<T extends DataTableValue> {
  columns: DataTableColumnDto<T>[];
  loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  className?: string;
}

export function DataTableComponent<T extends DataTableValue>({
  columns,
  loadData,
  className = "",
}: Props<T>) {
  const [data, setData] = useState<T[]>([]);
  const [totalRecords, setTotalRecords] = useState(0);
  const [loading, setLoading] = useState(false);
  const { version } = useFlowStore();

  const [lazyParams, setLazyParams] = useState<LazyDto>({
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
  }, [loadLazyData, version]);

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
