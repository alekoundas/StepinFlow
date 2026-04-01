import LabelComponent from "@/shared/components/LabelComponent";

import { classNames } from "primereact/utils";
import { useController, type FieldValues, type Path } from "react-hook-form";
import {
  Dropdown,
  type DropdownFilterEvent,
  type DropdownProps,
} from "primereact/dropdown";
import { useQuery } from "@tanstack/react-query";
import { useMemo, useState, type ReactNode } from "react";

interface BaseProps<TForm extends FieldValues, T> {
  fieldName: Path<TForm>;
  labelText: string;
  placeholderText?: string;
  hintText?: string;
  isDisabled?: boolean;
  isRequired?: boolean;
  className?: string;

  // Display & value extraction
  optionLabel?: keyof T | ((item: T) => string);
  optionValue?: keyof T | ((item: T) => any);

  itemTemplate?: (item: T) => ReactNode;
  valueTemplate?: (option: T | null, props: DropdownProps) => ReactNode;

  // Filtering
  filter?: boolean;
  filterBy?: string;
  showClear?: boolean;
}

type LocalProps<TForm extends FieldValues, T> = BaseProps<TForm, T> & {
  mode: "local";
  options: T[];
  queryKey?: never;
  queryFn?: never;
};

type RemoteProps<TForm extends FieldValues, T> = BaseProps<TForm, T> & {
  mode: "remote";
  options?: never;
  queryKey: readonly unknown[];
  queryFn: (filter?: string) => Promise<T[]>;
};

export function FormDropdownComponent<TForm extends FieldValues, T>({
  fieldName,
  labelText,
  placeholderText = "Select...",
  hintText,
  isDisabled = false,
  isRequired = false,
  filter = true,
  filterBy,
  showClear = true,
  optionLabel,
  optionValue,
  // Actionns
  itemTemplate,
  valueTemplate,

  ...props // Rest of props includes either local or remote specific props
}: LocalProps<TForm, T> | RemoteProps<TForm, T>) {
  const [filterValue, setFilterValue] = useState("");

  const {
    field: { value, onChange, onBlur, ref },
    fieldState: { invalid, error },
  } = useController<TForm>({ name: fieldName });

  // Local or Remote data
  const isRemote = "mode" in props && props.mode === "remote";

  const { data: remoteData, isLoading } = useQuery({
    queryKey: isRemote ? [...props.queryKey, filterValue] : ["dummy"],
    queryFn: isRemote
      ? () => props.queryFn(filterValue)
      : () => Promise.resolve([]),
    enabled: isRemote,
  });

  const options = useMemo(() => {
    if (!isRemote) return (props as LocalProps<any, any>).options;
    return remoteData ?? [];
  }, [isRemote, props, remoteData]);

  const onFilter = (e: DropdownFilterEvent) => {
    setFilterValue(e.filter ?? "");
  };

  // Helper to get display value
  const getOptionLabel = (item: T): string => {
    if (!item) return "";
    if (typeof optionLabel === "function") return optionLabel(item);
    if (typeof optionLabel === "string" && typeof item === "object")
      return (item as any)[optionLabel] ?? String(item);
    return String(item);
  };

  const getOptionValue = (item: T) => {
    if (typeof optionValue === "function") return optionValue(item);
    if (typeof optionValue === "string" && typeof item === "object")
      return (item as any)[optionValue];
    return item;
  };

  return (
    <>
      <div className="field">
        <LabelComponent
          text={labelText}
          weight="bold"
          isRequired={isRequired}
        />

        <Dropdown
          ref={ref}
          value={value}
          onChange={(e) => onChange(e.value)}
          onBlur={onBlur}
          options={options}
          optionLabel={
            typeof optionLabel === "string" ? optionLabel : undefined
          }
          optionValue={
            typeof optionValue === "string" ? optionValue : undefined
          }
          disabled={isDisabled}
          placeholder={placeholderText}
          className={classNames("w-full", { "p-invalid": invalid })}
          itemTemplate={itemTemplate}
          valueTemplate={valueTemplate}
          loading={isRemote ? isLoading : false}
          filter={filter}
          filterDelay={300}
          onFilter={onFilter}
          filterBy={filterBy}
          showClear={showClear}
          // PrimeReact RHF recommendation
          focusInputRef={ref as any}
        />
        <LabelComponent
          text={hintText ?? ""}
          weight="bold"
          size="xs"
          hidden={hintText === undefined}
        />
        <LabelComponent
          text={error?.message ?? ""}
          weight="normal"
          size="sm"
          hidden={error?.message === undefined}
          className="p-error mt-1 block"
        />
      </div>
    </>
  );
}
