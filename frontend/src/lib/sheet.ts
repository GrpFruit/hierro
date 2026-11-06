import type { SessionEntry, SheetExercise } from "./types";

/** Unsaved edits to one exercise card: weight and reps by set index, plus the note. */
export interface ExerciseDraft {
  w?: Record<number, string>;
  r?: Record<number, string>;
  n?: string;
}

export type SessionDraft = Record<number, ExerciseDraft>;

export interface SetRow {
  w: string;
  r: string;
  /** Gray weight shown when the field is blank: the coach's target, then the last weight typed above. */
  placeholder: string;
  /** The weight that gets saved for this set. */
  effectiveWeight: number;
}

export function setRows(item: SheetExercise, draft?: ExerciseDraft): SetRow[] {
  const { exercise: ex, saved } = item;
  const bodyweight = ex.kind === "bodyweight";
  let carry = item.coach.weight == null ? "" : String(item.coach.weight);
  return Array.from({ length: ex.sets }, (_, s) => {
    const sv = saved?.sets[s];
    const w = draft?.w?.[s] ?? (sv && !bodyweight && sv.w ? String(sv.w) : "");
    const r = draft?.r?.[s] ?? (sv ? String(sv.r) : "");
    const placeholder = carry;
    if (w !== "") carry = w;
    const effectiveWeight = bodyweight ? 0 : Math.max(0, parseFloat(w !== "" ? w : placeholder) || 0);
    return { w, r, placeholder, effectiveWeight };
  });
}

export const noteValue = (item: SheetExercise, draft?: ExerciseDraft) => draft?.n ?? item.saved?.note ?? "";

/** Turns the on-screen sheet into the entries the API stores. Sets without reps are skipped. */
export function buildEntries(exercises: SheetExercise[], draft: SessionDraft | undefined): SessionEntry[] {
  return exercises.flatMap((item, i) => {
    const sets = setRows(item, draft?.[i]).flatMap((row) => {
      const r = parseInt(row.r, 10);
      return r > 0 ? [{ w: row.effectiveWeight, r }] : [];
    });
    const note = noteValue(item, draft?.[i]).trim().slice(0, 300);
    return sets.length || note ? [{ name: item.exercise.name, sets, note }] : [];
  });
}
