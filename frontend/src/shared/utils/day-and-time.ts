/** Calendar days, so ten past midnight is "Yesterday" for something done at eleven. */
export const daysAgo = (date: Date): number => {
  const today = new Date();
  today.setHours(0, 0, 0, 0);

  const day = new Date(date);
  day.setHours(0, 0, 0, 0);

  return Math.round((today.getTime() - day.getTime()) / 86_400_000);
};

export const shortDate = (date: Date): string =>
  date.toLocaleDateString(undefined, { day: "numeric", month: "short" });

/** "Today, 18:04", "Yesterday, 23:10", "20 Oct, 16:03". */
export const dayAndTime = (value: string): string => {
  const date = new Date(value);
  const time = date.toLocaleTimeString(undefined, {
    hour: "2-digit",
    minute: "2-digit",
  });
  const days = daysAgo(date);

  if (days === 0) return `Today, ${time}`;
  if (days === 1) return `Yesterday, ${time}`;

  return `${shortDate(date)}, ${time}`;
};
