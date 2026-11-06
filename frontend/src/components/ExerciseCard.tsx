import { dashes, fmtDate } from "@/lib/format";
import { noteValue, setRows, type ExerciseDraft } from "@/lib/sheet";
import type { Exercise, SetLog, SheetExercise } from "@/lib/types";

export type Field = "w" | "r" | "n";

interface Props {
  item: SheetExercise;
  index: number;
  draft: ExerciseDraft | undefined;
  onField: (index: number, field: Field, setIndex: number, value: string) => void;
}

const fmtSet = (ex: Exercise, s: SetLog) => {
  const m = ex.unit === "meters" ? " m" : "";
  return ex.kind === "bodyweight" ? `${s.r}${m}` : `${s.w}×${s.r}${m}`;
};

export function ExerciseCard({ item, index: i, draft, onField }: Props) {
  const { exercise: ex, coach, last, platesPerSide, warmUps } = item;
  const bodyweight = ex.kind === "bodyweight";
  const [lo, hi] = ex.reps.split("-").map(Number);
  const repPlaceholder = hi && hi !== lo ? `${lo}–${hi}` : String(lo);
  const repWord = ex.unit === "meters" ? "meters" : ex.perSide ? "reps/side" : "reps";
  const weightUnit = ex.kind === "dumbbell" || ex.unit === "meters" ? "lb each" : "lb";
  const notePlaceholder = /Split Squat/.test(ex.name) ? "Right vs left: e.g. right leg felt weaker on set 3" : "Notes (optional)";
  const rows = setRows(item, draft);

  return (
    <article className={`ex${ex.main ? " main" : ""}`}>
      <div className="ex-head">
        <span className="ex-num" aria-hidden="true">{i + 1}</span>
        <div className="ex-title">
          <h3>
            <a href={ex.video} target="_blank" rel="noopener noreferrer">
              {ex.name}
              <span className="vid">demo ↗</span>
            </a>
          </h3>
          <div className="ex-meta">
            {ex.main && <span className="tag-main">Main lift</span>}
            <span>{ex.target}</span>
            <span>{ex.equipment}</span>
          </div>
        </div>
        <div className="ex-rx">
          <b>
            {ex.sets} × {dashes(ex.reps)}
            {ex.unit === "meters" ? " m" : ""}
            {ex.perSide ? "/side" : ""}
          </b>
          <span>rest {dashes(ex.rest)}</span>
        </div>
      </div>

      {ex.note && <p className="ex-note">{ex.note}</p>}

      <div className="coach">
        <div className="coach-line">
          <span className={`pill ${coach.tone}`}>{coach.label}</span>
          <span>{coach.text}</span>
        </div>
        {(last || platesPerSide) && (
          <div className="sub">
            {last && (
              <span>
                Last · {fmtDate(last.date)}: {last.sets.map((s) => fmtSet(ex, s)).join(", ")}
              </span>
            )}
            {platesPerSide && platesPerSide.length > 0 && (
              <>
                <span>Load per side:</span>
                <span className="plates">
                  {platesPerSide.map((p, k) => (
                    <span key={k} className={`plate p${String(p).replace(".", "")}`}>{p}</span>
                  ))}
                </span>
              </>
            )}
            {platesPerSide && platesPerSide.length === 0 && <span>Empty bar (45 lb)</span>}
          </div>
        )}
        {warmUps && warmUps.length > 0 && (
          <div className="sub">
            <span>Warm-up: {warmUps.join(" → ")}</span>
          </div>
        )}
      </div>

      <div className="sets">
        {rows.map((row, s) => (
          <div className="set" key={s}>
            <span className="set-n">Set {s + 1}</span>
            {!bodyweight && (
              <>
                <label className="fld">
                  <input
                    type="number"
                    inputMode="decimal"
                    min={0}
                    step={2.5}
                    value={row.w}
                    placeholder={row.placeholder}
                    aria-label={`${ex.name} set ${s + 1} weight in pounds`}
                    onChange={(e) => onField(i, "w", s, e.target.value)}
                  />
                  {weightUnit}
                </label>
                <span className="x">×</span>
              </>
            )}
            <label className="fld">
              <input
                type="number"
                inputMode="numeric"
                min={0}
                step={1}
                value={row.r}
                placeholder={repPlaceholder}
                aria-label={`${ex.name} set ${s + 1} ${repWord}`}
                onChange={(e) => onField(i, "r", s, e.target.value)}
              />
              {repWord}
            </label>
          </div>
        ))}
      </div>

      <input
        className="note"
        type="text"
        maxLength={300}
        value={noteValue(item, draft)}
        placeholder={notePlaceholder}
        aria-label={`${ex.name} notes`}
        onChange={(e) => onField(i, "n", 0, e.target.value)}
      />
    </article>
  );
}
