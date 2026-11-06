// Shapes returned by the Hierro API (backend/Hierro.Api/Endpoints/Contracts.cs).

export type ExerciseKind = "barbell" | "ezBar" | "dumbbell" | "bodyweight";

export interface Exercise {
  name: string;
  kind: ExerciseKind;
  sets: number;
  reps: string;
  rest: string;
  unit: "reps" | "meters";
  main: boolean;
  perSide: boolean;
  note: string | null;
  progressionTip: string | null;
  target: string;
  equipment: string;
  video: string;
}

export interface WorkoutDay {
  id: string;
  name: string;
  focus: string;
  exercises: Exercise[];
}

export interface Plan {
  id: string;
  name: string;
  subtitle: string;
  schedule: string;
  highlights: { title: string; detail: string }[];
  days: WorkoutDay[];
}

export interface SetLog {
  w: number;
  r: number;
}

export interface CoachAdvice {
  tone: "first" | "up" | "stay" | "drop";
  label: string;
  text: string;
  weight: number | null;
}

export interface SessionEntry {
  name: string;
  sets: SetLog[];
  note: string;
}

export interface SheetExercise {
  exercise: Exercise;
  coach: CoachAdvice;
  last: { date: string; sets: SetLog[] } | null;
  platesPerSide: number[] | null;
  warmUps: string[] | null;
  saved: SessionEntry | null;
}

export interface Sheet {
  planId: string;
  planName: string;
  dayId: string;
  dayName: string;
  focus: string;
  date: string;
  sessionId: string;
  savedAtUtc: string | null;
  exercises: SheetExercise[];
}

export interface SessionSummary {
  id: string;
  date: string;
  planId: string;
  dayId: string;
  dayName: string;
  savedAtUtc: string;
  setCount: number;
  mainLift: { label: string; topWeight: number } | null;
}

export interface LiftProgress {
  name: string;
  label: string;
  points: { date: string; weight: number; reps: number }[];
}
