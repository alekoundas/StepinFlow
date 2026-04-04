import type { LazyDto } from "@/shared/models/lazy-data/lazy-dto";
import { useState, type ReactNode } from "react";
import { useQuery } from "@tanstack/react-query";

import { Paginator, type PaginatorPageChangeEvent } from "primereact/paginator";

interface Props<T> {
  queryKey: readonly unknown[];
  queryFn: (params: LazyDto) => Promise<any>;
  itemTemplate: (item: T) => ReactNode;
  rowsPerPageOptions?: number[];
  enablePaging?: boolean;
  // className?: string;
  // gridClassName?: string;
}

export function DataGridComponent<T>({
  queryKey,
  queryFn,
  itemTemplate,
  rowsPerPageOptions = [6, 9, 12, 18, 24, 36],
  enablePaging = false,
  // gridClassName = "grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4",
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
  const { data, isLoading } = useQuery({
    queryKey: [...queryKey, lazyParams], // important: include lazyParams in key
    queryFn: () => queryFn(lazyParams),
  });
  const response = data ?? { data: [] as T[], totalRecords: 0 };

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
      {/* <div className="grid"> */}
      <div
        style={{
          display: "grid",
          gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))",
          gap: "0.75rem",
        }}
      >
        {response.data.map((x: T, index: number) => (
          <div
            key={index}
            // className="col-12 md:col-6 lg:col-4 xl:col-2"
            className="m-3 border-round-xl surface-card cursor-pointer transition-all transition-duration-150"
            onMouseEnter={(e) =>
              (e.currentTarget.style.boxShadow =
                "0 4px 12px rgba(66, 121, 208, 0.95)")
            }
            onMouseLeave={(e) => (e.currentTarget.style.boxShadow = "none")}
          >
            {itemTemplate(x)}
          </div>
        ))}
      </div>

      {isLoading && <div className="mt-4">Loading...</div>}

      {enablePaging && response.totalRecords > 0 && (
        <div className="mt-4 flex justify-content-center">
          <Paginator
            first={lazyParams.first}
            rows={lazyParams.rows}
            totalRecords={response.totalRecords}
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
