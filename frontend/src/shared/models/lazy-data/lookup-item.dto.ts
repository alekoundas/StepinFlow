export interface LookupItemDto {
  value: string; // Stored value
  label: string; // Displayed value
  description?: string; // Optional extra info
  extraData?: any; // ExtraData = new { Index = i, IsPrimary = s.Primary, Bounds = s.Bounds }
}
