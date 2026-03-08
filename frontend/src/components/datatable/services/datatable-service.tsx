// useDataTableService.ts (hook with preserved logic, no loops)
import { useCallback } from "react";
import {
  DataTableFilterEvent,
  DataTableFilterMetaData,
  DataTablePageEvent,
  DataTableRowEditCompleteEvent,
  DataTableSortEvent,
  DataTableSortMeta,
} from "primereact/datatable";
import { ColumnEvent } from "primereact/column";
import { DataTableDto } from "../../../models/data-table/datatable-dto";

interface UseDataTableServiceParams<TEntity> {
  controller: string;
  setLoading: (loading: boolean) => void;
  defaultUrlSearchQuery: string | null;
  //   formMode: FormMode;
  afterDataLoaded?: (
    data: DataTableDto<TEntity> | null,
  ) => DataTableDto<TEntity> | null;
}

export const useDataTableService = <TEntity,>({
  controller,
  setLoading,
  defaultUrlSearchQuery,
  //   formMode,
  afterDataLoaded,
}: UseDataTableServiceParams<TEntity>) => {
  const apiService = useApiService();

  const setUrlSearchQuery = useCallback(
    (response: DataTableDto<TEntity> | null) => {
      if (!response || !defaultUrlSearchQuery) return;
      // Preserved logic (uncomment if needed)
      // const queryData = { ...response, data: [] };
      // const searchQuery = btoa(JSON.stringify(queryData));
      // if (searchQuery !== defaultUrlSearchQuery) {
      //   window.history.replaceState(null, "New Page Title", `/${controller}?search=${searchQuery}`);
      // }
    },
    [controller, defaultUrlSearchQuery],
  );

  const refreshData = useCallback(
    async (
      dataTableDto: DataTableDto<TEntity>,
    ): Promise<DataTableDto<TEntity> | null> => {
      if (formMode === FormMode.ADD) {
        return afterDataLoaded ? afterDataLoaded(dataTableDto) : dataTableDto;
      } else {
        setLoading(true);
        dataTableDto.data = [];
        const response = await apiService.getDataGrid<TEntity>(
          controller,
          dataTableDto,
        );
        const processed = afterDataLoaded
          ? afterDataLoaded(response)
          : response;
        setUrlSearchQuery(processed);
        setLoading(false);
        return processed;
      }
    },
    [
      formMode,
      afterDataLoaded,
      apiService,
      controller,
      setUrlSearchQuery,
      setLoading,
    ],
  );

  const loadData = useCallback(
    async (
      dataTableDto: DataTableDto<TEntity>,
      urlQuery: string | null,
    ): Promise<DataTableDto<TEntity> | null> => {
      // Preserved logic (uncomment if needed)
      // if (urlQuery) {
      //   try {
      //     const decodedQuery: DataTableDto<TEntity> = JSON.parse(atob(urlQuery));
      //     Object.assign(dataTableDto, decodedQuery);
      //   } catch (e: any) {
      //     console.log(e.message);
      //   }
      // }
      return refreshData(dataTableDto);
    },
    [refreshData],
  );

  const onSort = useCallback(
    (dataTableDto: DataTableDto<TEntity>, event: DataTableSortEvent) => {
      dataTableDto.sorts = [];
      if (event.multiSortMeta) {
        dataTableDto.dataTableSorts = event.multiSortMeta;
        event.multiSortMeta.forEach(
          (value: DataTableSortMeta, index: number) => {
            dataTableDto.sorts?.push({
              fieldName: value.field as string,
              order: value.order,
            });
          },
        );
      }
      refreshData(dataTableDto);
    },
    [refreshData],
  );

  const onFilter = useCallback(
    (dataTableDto: DataTableDto<TEntity>, event: DataTableFilterEvent) => {
      dataTableDto.filters = [];

      Object.entries(event.filters ?? {}).map(([value, index]) => {
        const filterField = event.filters[value] as DataTableFilterMetaData;

        const filter = {
          fieldName: value,
          filterType: filterField.matchMode,
        } as DataTableFilterDto;

        if (Array.isArray(filterField.value)) {
          filter.values = filterField.value ?? [];
        } else {
          filter.value = filterField.value;
        }

        dataTableDto.filters?.push(filter);
      });

      refreshData(dataTableDto);
    },
    [refreshData],
  );

  const onPage = useCallback(
    (dataTableDto: DataTableDto<TEntity>, event: DataTablePageEvent) => {
      dataTableDto.page = event.page ?? 0;
      dataTableDto.rows = event.rows;
      dataTableDto.first = event.first;

      refreshData(dataTableDto);
    },
    [refreshData],
  );

  const onCellEditComplete = useCallback((e: ColumnEvent) => {
    let { rowData, newValue, field, originalEvent: event } = e;
    (rowData as any)[field as string] = newValue;
    event?.preventDefault();
  }, []);

  const onRowEditComplete = useCallback(
    (dataTableDto: DataTableDto<TEntity>, e: DataTableRowEditCompleteEvent) => {
      let { newData, field, index } = e;

      dataTableDto.data[index] = newData as TEntity;
      refreshData(dataTableDto);
    },
    [refreshData],
  );

  return {
    loadData,
    onSort,
    onFilter,
    onPage,
    onCellEditComplete,
    onRowEditComplete,
    refreshData,
  };
};
