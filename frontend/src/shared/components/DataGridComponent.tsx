import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import type { LazyResponseDto } from "@/shared/models/lazy-data/lazy-response-dto";

import { useFlowStore } from "@/features/flow";
import { Paginator, type PaginatorPageChangeEvent } from "primereact/paginator";
import { useCallback, useEffect, useState, type ReactNode } from "react";

interface Props<T> {
  loadData: (params: LazyDto) => Promise<LazyResponseDto<T>>;
  itemTemplate: (item: T) => ReactNode;
  rowsPerPageOptions?: number[];
  enablePaging?: boolean;
  // className?: string;
  // gridClassName?: string;
}

export function DataGridComponent<T>({
  loadData,
  itemTemplate,
  rowsPerPageOptions = [6, 9, 12, 18, 24, 36],
  enablePaging = false,
  // gridClassName = "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4",
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

  const onPage = (event: PaginatorPageChangeEvent) => {
    setLazyParams((prev) => ({
      ...prev,
      first: event.first,
      rows: event.rows,
      page: event.page ?? 0,
    }));
  };

  return (
    <div className="flex flex-column">
      <div className="grid  ">
        {/* <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-3 xl:grid-cols-4  gap-2"> */}
        {data.map((x, index) => (
          <div
            key={index}
            className="col-12 md:col-6 lg:col-4 xl:col-2 "
          >
            {itemTemplate(x)}
          </div>
        ))}
      </div>

      {/* Paginator */}
      {totalRecords > 0 && enablePaging && (
        <div className="mt-4 flex justify-content-center">
          <Paginator
            first={lazyParams.first}
            rows={lazyParams.rows}
            totalRecords={totalRecords}
            rowsPerPageOptions={rowsPerPageOptions}
            onPageChange={onPage}
            template="FirstPageLink PrevPageLink PageLinks NextPageLink LastPageLink RowsPerPageDropdown CurrentPageReport"
            currentPageReportTemplate="Showing {first} to {last} of {totalRecords} items"
            className="border-none"
          />
        </div>
      )}
    </div>
  );
}
