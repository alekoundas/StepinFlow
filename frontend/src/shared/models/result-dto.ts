export interface ResultDto<T = any> {
  isSuccess: boolean;
  data?: T | null;
  errorMessage?: string;
  errors?: string[];
}
