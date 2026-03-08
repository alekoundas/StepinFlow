// import { Button } from "primereact/button";
// import { Column, ColumnBodyOptions } from "primereact/column";
// import {
//   DataTable,
//   DataTableFilterMeta,
//   DataTableRowEditCompleteEvent,
//   DataTableSelectionSingleChangeEvent,
//   DataTableValue,
//   DataTableValueArray,
// } from "primereact/datatable";
// import React, { useState } from "react";
// import { DataTableDto } from "../../models/data-table/datatable-dto";
// import { DataTableColumnDto } from "../../models/data-table/datatable-column-dto";
// import { ButtonTypeEnum } from "../../models/enums/button-type-enum";

// interface IField<TEntity> {
//   controller: string;
//   dataTableDto: DataTableDto<TEntity>;
//   setDataTableDto: (dataTableDto: DataTableDto<TEntity>) => void;
//   dataTableColumns: DataTableColumnDto<TEntity>[];
//   // formMode: FormMode;
//   // editMode?: DataTableEditModeEnum;
//   // filterDisplay?: DataTableFilterDisplayEnum;
//   // authorize?: boolean;
//   // loadDataOnInit?: boolean;
//   availableGridRowButtons?: ButtonTypeEnum[];
//   // onRowEditInit?: (e: any) => void;
//   // onRowEditComplete?: (e: DataTableRowEditCompleteEvent) => void;
//   // onRowEditCancel?: (e: any) => void;
//   onButtonClick: (buttonType: ButtonTypeEnum, rowData?: TEntity) => void;
//   onAfterDataLoaded?: (
//     data: DataTableDto<TEntity> | null
//   ) => DataTableDto<TEntity> | null;
//   triggerRefreshData?: React.MutableRefObject<
//     ((dto: DataTableDto<TEntity>) => void) | undefined
//   >;

//   onSelect?: (
//     e: DataTableSelectionSingleChangeEvent<DataTableValueArray>
//   ) => void;
//   selectedObject?: TEntity | undefined;
// }

// export default function DataTableComponent<TEntity extends DataTableValue>({
//   controller,
//   dataTableDto,
//   setDataTableDto,
//   dataTableColumns,
//   // formMode,
//   // editMode,
//   // filterDisplay,
//   // authorize = false,
//   // loadDataOnInit = true,
//   availableGridRowButtons = [],
//   // onRowEditInit,
//   // onRowEditComplete,
//   // onRowEditCancel,
//   onButtonClick,
//   onAfterDataLoaded,
//   triggerRefreshData,
//   onSelect,
//   selectedObject,
// }: IField<TEntity>) {
//   // const { t } = useTranslator();
//   const [loading, setLoading] = useState(true);

//   const afterDataLoaded = (
//     data: DataTableDto<TEntity> | null
//   ): DataTableDto<TEntity> | null => {
//     let updateData: DataTableDto<TEntity> | null = data;

//     // if parent has set the onAfterDataLoaded, call parent
//     if (onAfterDataLoaded) {
//       updateData = onAfterDataLoaded(data);
//     }

//     if (updateData)
//       setDataTableDto({
//         ...dataTableDto,
//         data: updateData.data,
//         totalRecords: updateData.totalRecords,
//         pageCount: updateData.pageCount,
//         page: updateData.page,
//         first: updateData.first,
//         rows: updateData.rows,
//       });

//     return updateData;
//   };

//   // UPDATED: Use hook with memoized afterDataLoaded
//   const {
//     loadData,
//     onSort,
//     onFilter,
//     onPage,
//     onCellEditComplete,
//     onRowEditComplete: serviceOnRowEditComplete,
//     refreshData,
//   } = useDataTableService<TEntity>({
//     controller,
//     setLoading,
//     defaultUrlSearchQuery: null,
//     formMode,
//     afterDataLoaded,
//   });

//   // Initialize
//   React.useEffect(() => {
//     if (formMode === FormMode.ADD) {
//       setLoading(false);
//     }

//     if (loadDataOnInit) {
//       refreshData(dataTableDto);
//     } else {
//       setLoading(false);
//     }
//   }, []);

//   const isInitialMount = React.useRef(true);
//   const refreshAllData = async (dto: DataTableDto<TEntity>) => {
//     if (triggerRefreshData && !isInitialMount.current) {
//       return await refreshData(dto);
//     }
//   };

//   React.useEffect(() => {
//     if (triggerRefreshData) {
//       triggerRefreshData.current = refreshAllData;
//     }
//     isInitialMount.current = false;
//   }, [triggerRefreshData]);

//   // Log if they have changed
//   // React.useEffect(() => {
//   //   console.log("data updated:", JSON.stringify(dataTableDto.data));
//   // }, [dataTableDto.data]);

//   const getDataTableColumns = () => {
//     const columns = dataTableColumns;

//     // In case editMode is set, add Edit inline button
//     if (editMode === DataTableEditModeEnum.ROW)
//       columns.push({
//         field: "",
//         header: t("Actions"),
//         sortable: false,
//         filter: false,
//         filterPlaceholder: "",
//         style: { width: "10%" },
//         body: undefined,
//         editor: true, // enables row edit
//       });

//     // In case editMode is NOT set, add View,Edit,Delete buttons
//     if (availableGridRowButtons.length > 0)
//       columns.push({
//         field: "",
//         header: t("Actions"),
//         sortable: false,
//         filter: false,
//         filterPlaceholder: "",
//         style: { width: "10%" },
//         body: gridRowActions,
//       });
//     return columns;
//   };

//   const gridRowActions = (rowData: TEntity, _options: ColumnBodyOptions) => (
//     <DataTableGridRowActionsComponent
//       rowData={rowData}
//       onButtonClick={onButtonClick}
//       authorize={authorize}
//       controller={controller}
//       availableGridRowButtons={availableGridRowButtons}
//     />
//   );

//   const renderHeader = () => {
//     dataTableDto.data;
//     let isVisible: boolean = availableGridRowButtons.some(
//       (x) => x === ButtonTypeEnum.ADD
//     );

//     if (authorize)
//       isVisible = isVisible && TokenService.isUserAllowed(controller + "_Add");

//     if (!isVisible) return;

//     return (
//       <div className="flex justify-content-between">
//         <div></div>
//         <Button
//           type="button"
//           icon="pi pi-plus"
//           label={t("Add")}
//           outlined
//           visible={isVisible}
//           onClick={() => {
//             onButtonClick(ButtonTypeEnum.ADD);
//           }}
//         />
//       </div>
//     );
//   };

// //   const dataTableFilters = () => {
// //     const dataTableFilters: DataTableFilterMeta = dataTableDto.filters.reduce(
// //       (accumulator, currentValue) => {
// //         if (currentValue.fieldName && currentValue.filterType) {
// //           accumulator[currentValue.fieldName] = {
// //             value: currentValue.value ?? currentValue.values,
// //             matchMode: currentValue.filterType,
// //           };
// //         }
// //         return accumulator;
// //       },
// //       {} as DataTableFilterMeta
// //     );
// //     return dataTableFilters;
// //   };

//   return (
//     <>
//       <DataTable
//         className="w-full h-full"
//         value={dataTableDto.data}
//         size="small"
//         // key={"id"}
//         lazy={true}
//         stripedRows
//         emptyMessage={t("No data found.")}

//         // Row selction.
//         selectionMode="single"
//         selection={selectedObject ?? undefined}
//         onSelectionChange={onSelect}
//         // Loading.
//         loading={loading}

//         // Pagging.
//         paginator
//         first={dataTableDto.first}
//         rows={dataTableDto.rows}
//         totalRecords={dataTableDto.totalRecords}
//         onPage={(x) => onPage(dataTableDto, x)}
//         rowsPerPageOptions={[5, 10, 25, 50, 100]}
//         paginatorRight={
//           <>
//             {dataTableDto.first + 1} to{" "}
//             {dataTableDto.rows * (dataTableDto.page + 1)} out of{" "}
//             {dataTableDto.totalRecords}
//           </>
//         }
//         paginatorLeft={<></>}

//         // Filter.
//         // filterDisplay={filterDisplay}
//         // filters={dataTableFilters()}
//         // onFilter={(x) => onFilter(dataTableDto, x)}

//         // Sort.
//         // removableSort
//         // sortMode="multiple"
//         // onSort={(x) => onSort(dataTableDto, x)}
//         // multiSortMeta={dataTableDto.dataTableSorts}
//         // header={renderHeader()}

//         // Edit row/column.
//         // editMode={editMode}
//         // onRowEditInit={onRowEditInit}
//         // onRowEditCancel={onRowEditCancel}
//         // onRowEditComplete={(x) =>
//         //   onRowEditComplete
//         //     ? onRowEditComplete(x)
//         //     : serviceOnRowEditComplete(dataTableDto, x)
//         // }
//       >
//         {/* {selectedObject && (
//           <Column
//             selectionMode="single"
//             headerStyle={{ width: "3rem" }}
//           ></Column>
//         )} */}

//         {getDataTableColumns().map((col, _i) => (
//           <Column
//             key={col.field}
//             field={col.field}
//             header={col.header}
//             sortable={col.sortable}
//             filter={col.filter}
//             filterPlaceholder={col.filterPlaceholder}
//             filterElement={col.filterTemplate}
//             style={col.style}
//             body={col.body}
//             showFilterMenu={false}
//             editor={col.cellEditor}
//             onCellEditComplete={
//               col.onCellEditComplete
//                 ? col.onCellEditComplete
//                 : onCellEditComplete
//             }
//             onCellEditInit={col.onCellEditInit}
//             rowEditor={col.editor}
//           />
//         ))}
//       </DataTable>
//     </>
//   );
// }
