"use client";

import { useCallback, useEffect, useState } from "react";
import { api, ApiError } from "@/lib/api";
import { fmtDate, shortDayName, todayISO } from "@/lib/format";
import { buildEntries, type SessionDraft } from "@/lib/sheet";
import type { LiftProgress, Plan, SessionSummary, Sheet } from "@/lib/types";
import { ExerciseCard, type Field } from "./ExerciseCard";
import { HistoryList } from "./HistoryList";
import { ProgressCharts } from "./ProgressCharts";

type Status = "connecting" | "ready" | "offline";
type Msg = { text: string; tone?: "ok" | "err" };

const IDLE_MSG: Msg = { text: "Workouts save to your Hierro server." };

function readPref<T>(key: string, fallback: T): T {
  try {
    const v = localStorage.getItem(key);
    return v ? (JSON.parse(v) as T) : fallback;
  } catch {
    return fallback;
  }
}

function writePref(key: string, value: unknown) {
  try {
    localStorage.setItem(key, JSON.stringify(value));
  } catch {
    /* storage unavailable: the choice just isn't remembered */
  }
}

export function Tracker() {
  const [status, setStatus] = useState<Status>("connecting");
  const [plans, setPlans] = useState<Plan[]>([]);
  const [planId, setPlanId] = useState("3day");
  const [dayByPlan, setDayByPlan] = useState<Record<string, string>>({});
  const [date, setDate] = useState("");
  const [sheet, setSheet] = useState<Sheet | null>(null);
  const [sheetVersion, setSheetVersion] = useState(0);
  const [sessions, setSessions] = useState<SessionSummary[]>([]);
  const [progress, setProgress] = useState<LiftProgress[]>([]);
  const [drafts, setDrafts] = useState<Record<string, SessionDraft>>({});
  const [msg, setMsg] = useState<Msg>(IDLE_MSG);
  const [saving, setSaving] = useState(false);

  const plan = plans.find((p) => p.id === planId) ?? plans[0];
  const day = plan?.days.find((d) => d.id === dayByPlan[plan.id]) ?? plan?.days[0];
  const currentPlanId = plan?.id;
  const currentDayId = day?.id;

  useEffect(() => {
    let cancelled = false;
    Promise.all([api.plans(), api.sessions(), api.progress()])
      .then(([p, s, pr]) => {
        if (cancelled) return;
        setPlanId(readPref("hierro.plan", "3day"));
        setDayByPlan(readPref("hierro.days", {}));
        setDate(todayISO());
        setPlans(p);
        setSessions(s);
        setProgress(pr);
        setStatus("ready");
      })
      .catch(() => !cancelled && setStatus("offline"));
    return () => {
      cancelled = true;
    };
  }, []);

  useEffect(() => {
    if (!currentPlanId || !currentDayId || !date) return;
    const ctrl = new AbortController();
    api
      .sheet(currentPlanId, currentDayId, date, ctrl.signal)
      .then(setSheet)
      .catch(() => !ctrl.signal.aborted && setStatus("offline"));
    return () => ctrl.abort();
  }, [currentPlanId, currentDayId, date, sheetVersion]);

  const refresh = useCallback(async () => {
    const [s, pr] = await Promise.all([api.sessions(), api.progress()]);
    setSessions(s);
    setProgress(pr);
    setSheetVersion((v) => v + 1);
  }, []);

  const choosePlan = (id: string) => {
    setPlanId(id);
    writePref("hierro.plan", id);
    setMsg(IDLE_MSG);
  };

  const chooseDay = (id: string) => {
    if (!plan) return;
    const next = { ...dayByPlan, [plan.id]: id };
    setDayByPlan(next);
    writePref("hierro.days", next);
    setMsg(IDLE_MSG);
  };

  const current = sheet && sheet.planId === currentPlanId && sheet.dayId === currentDayId && sheet.date === date ? sheet : null;
  const draft = current ? drafts[current.sessionId] : undefined;

  const onField = (i: number, field: Field, s: number, value: string) => {
    if (!current) return;
    const key = current.sessionId;
    setDrafts((prev) => {
      const sd = { ...(prev[key] ?? {}) };
      const ed = { ...(sd[i] ?? {}) };
      if (field === "n") ed.n = value;
      else ed[field] = { ...(ed[field] ?? {}), [s]: value };
      sd[i] = ed;
      return { ...prev, [key]: sd };
    });
  };

  async function save() {
    if (!current) return;
    const entries = buildEntries(current.exercises, draft);
    if (!entries.some((e) => e.sets.length)) {
      setMsg({ text: "Enter reps for at least one set, then save.", tone: "err" });
      return;
    }
    setSaving(true);
    setMsg({ text: "Saving…" });
    try {
      await api.saveSession(current.date, current.dayId, current.planId, entries);
      setDrafts((prev) => {
        const next = { ...prev };
        delete next[current.sessionId];
        return next;
      });
      setMsg({ text: `Saved ${shortDayName(current.dayName)} for ${fmtDate(current.date)}.`, tone: "ok" });
      await refresh();
    } catch (err) {
      setMsg({
        text: err instanceof ApiError && err.status === 400 ? err.message : "Couldn't save. Check that the API is running and tap Save again.",
        tone: "err",
      });
    } finally {
      setSaving(false);
    }
  }

  async function remove(id: string) {
    await api.deleteSession(id);
    await refresh();
  }

  if (status === "offline" && !plans.length) {
    return (
      <div className="status off" role="alert">
        Can&apos;t reach the Hierro API. Start it with <code>dotnet run</code> in <code>backend/Hierro.Api</code>, then reload this page.
      </div>
    );
  }
  if (!plan || !day) {
    return <div className="status">Connecting to your workout log…</div>;
  }

  const lastDone = (dayId: string) => {
    const s = sessions.find((x) => x.dayId === dayId);
    return s ? `Last done ${fmtDate(s.date)}` : "Not logged yet";
  };

  return (
    <>
      <div className="plan-bar">
        <div className="seg" role="group" aria-label="Choose plan">
          {plans.map((p) => (
            <button key={p.id} type="button" aria-pressed={p.id === plan.id} onClick={() => choosePlan(p.id)}>
              {p.name}
            </button>
          ))}
        </div>
        <div className="split-why">
          <span>
            <b>{plan.subtitle}</b> · {plan.schedule}
          </span>
          {plan.highlights.map((h) => (
            <span key={h.title}>
              <b>{h.title}:</b> {h.detail}
            </span>
          ))}
        </div>
      </div>

      <div className="days-scroll">
        <div className="days" role="group" aria-label="Choose workout day">
          {plan.days.map((d) => (
            <button key={d.id} type="button" className="day-tab" aria-pressed={d.id === day.id} onClick={() => chooseDay(d.id)}>
              <span className="dn">{d.name}</span>
              <span className="df">{d.focus}</span>
              <span className="dl">{lastDone(d.id)}</span>
            </button>
          ))}
        </div>
      </div>

      {status === "offline" && (
        <div className="status off" role="alert">
          Lost the connection to the Hierro API. Changes won&apos;t save until it&apos;s back.
        </div>
      )}

      <section className="session" aria-labelledby="day-title">
        <div className="session-head">
          <div className="meta">
            <p className="eyebrow">
              {plan.name} · {day.focus}
            </p>
            <h2 id="day-title">{day.name}</h2>
          </div>
          <label className="date-field">
            Workout date
            <input
              type="date"
              value={date}
              onChange={(e) => {
                if (!e.target.value) return;
                setDate(e.target.value);
                setMsg(IDLE_MSG);
              }}
            />
          </label>
        </div>
        <p className="tip">
          Tap an exercise name to watch its demo. Gray numbers are the suggested target. Leave a weight blank to use the gray number, and only fill in reps.
        </p>
        <div className="exercises" aria-busy={!current}>
          {current ? (
            current.exercises.map((item, i) => <ExerciseCard key={item.exercise.name} item={item} index={i} draft={draft?.[i]} onField={onField} />)
          ) : (
            <p className="tip">Loading {day.name}…</p>
          )}
        </div>
        <div className="save-bar">
          <span className={`msg${msg.tone ? ` ${msg.tone}` : ""}`} role="status">
            {msg.text}
          </span>
          <button className="btn" type="button" disabled={!current || saving || status !== "ready"} onClick={save}>
            {current?.savedAtUtc ? "Update workout" : "Save workout"}
          </button>
        </div>
      </section>

      <section className="section" aria-labelledby="progress-title">
        <div className="section-head">
          <h2 id="progress-title">Main lift progress</h2>
          <p className="tip">Heaviest set each session, in lb</p>
        </div>
        <ProgressCharts lifts={progress} />
      </section>

      <section className="section" aria-labelledby="history-title">
        <div className="section-head">
          <h2 id="history-title">Workout history</h2>
          {sessions.length > 0 && (
            <p className="tip">
              {sessions.length} workout{sessions.length === 1 ? "" : "s"} logged
            </p>
          )}
        </div>
        <HistoryList sessions={sessions} onDelete={remove} />
      </section>
    </>
  );
}
