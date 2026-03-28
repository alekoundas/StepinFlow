import type { DataTableStateEvent, DataTableValue } from "primereact/datatable";
import type { DataTableColumnDto } from "@/shared/models/lazy-data/datatable-column-dto";
import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";

import { DataTable } from "primereact/datatable";
import { Column } from "primereact/column";
import { useState } from "react";
import { ProgressBar } from "primereact/progressbar";
import { useQuery } from "@tanstack/react-query";

interface Props<T extends DataTableValue> {
  columns: DataTableColumnDto<T>[];
  queryKey: readonly unknown[];
  queryFn: (params: LazyDto) => Promise<any>;
  className?: string;
}

export function DataTableComponent<T extends DataTableValue>({
  columns,
  queryKey,
  queryFn,
  className = "",
}: Props<T>) {
  const [lazyParams, setLazyParams] = useState<LazyDto>({
    first: 0,
    rows: 10,
    page: 0,
    sortField: undefined,
    sortOrder: null,
    filters: {},
  });

  // React Query – automatic caching per page/sort/filter
  // const { data, isLoading, error } = useFlows(lazyParams);
  const { data, isLoading, error } = useQuery({
    queryKey: [...queryKey, lazyParams], // important: include lazyParams in key
    queryFn: () => queryFn(lazyParams),
  });
  const response = data ?? { data: [] as T[], totalRecords: 0 };

  // const loadLazyData = useCallback(async () => {
  //   setLoading(true);
  //   try {
  //     const response = await loadData(lazyParams);
  //     setData(response.data);
  //     setTotalRecords(response.totalRecords);
  //   } catch (err) {
  //     console.error(err);
  //   } finally {
  //     setLoading(false);
  //   }
  // }, [lazyParams, loadData]);

  // useEffect(() => {
  //   loadLazyData();
  // }, [loadLazyData, version]);

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

  if (error) {
    return <div className="p-4 text-red-500">Failed to load flows</div>;
  }

  return (
    <div className={className}>
      {isLoading && (
        <ProgressBar
          mode="indeterminate"
          style={{ height: "3px" }}
        />
      )}

      <DataTable
        value={response.data}
        lazy
        paginator
        rows={lazyParams.rows}
        first={lazyParams.first}
        totalRecords={response.totalRecords}
        onPage={onPage}
        onSort={onSort}
        onFilter={onFilter}
        sortField={lazyParams.sortField}
        sortOrder={lazyParams.sortOrder}
        filters={lazyParams.filters}
        filterDisplay="row"
        loading={isLoading}
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
