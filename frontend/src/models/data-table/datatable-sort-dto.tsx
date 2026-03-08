export interface DataTableSortDto {
  fieldName: string;
  order: 1 | 0 | -1;
}

export class DataTableSortDto implements DataTableSortDto {
  fieldName: string = "";
  order: 1 | 0 | -1 = 0;
}
