export type PageWidth = "form" | "wide";

/** A form reads worse the wider it gets; a list or a dashboard has a use for more room. */
export const PAGE_MAX_WIDTHS: Record<PageWidth, number> = { form: 1200, wide: 1600 };
