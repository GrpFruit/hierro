export function todayISO(): string {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, "0")}-${String(d.getDate()).padStart(2, "0")}`;
}

export function fmtDate(iso: string, withYear = false): string {
  const d = new Date(`${iso}T12:00:00`);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleDateString(undefined, withYear ? { month: "short", day: "numeric", year: "numeric" } : { month: "short", day: "numeric" });
}

/** "Day 2: Pull" → "Pull" */
export const shortDayName = (name: string) => name.replace(/^Day \d+:\s*/, "");

/** "8-10" → "8–10" */
export const dashes = (s: string) => s.replace("-", "–");
