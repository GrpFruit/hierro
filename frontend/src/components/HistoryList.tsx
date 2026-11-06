"use client";

import { useEffect, useState } from "react";
import { fmtDate, shortDayName } from "@/lib/format";
import type { SessionSummary } from "@/lib/types";

interface Props {
  sessions: SessionSummary[];
  onDelete: (id: string) => Promise<void>;
}

const SHOWN = 15;

export function HistoryList({ sessions, onDelete }: Props) {
  // Delete takes two taps: the first arms the button for 4 seconds.
  const [armed, setArmed] = useState<string | null>(null);
  const [busy, setBusy] = useState<string | null>(null);
  const [failed, setFailed] = useState<string | null>(null);

  useEffect(() => {
    if (!armed) return;
    const t = setTimeout(() => setArmed(null), 4000);
    return () => clearTimeout(t);
  }, [armed]);

  if (!sessions.length) {
    return (
      <div className="history">
        <p className="h-empty">Your saved workouts show up here, newest first. Fill in today&apos;s sets above and tap Save workout.</p>
      </div>
    );
  }

  async function click(id: string) {
    if (armed !== id) {
      setArmed(id);
      setFailed(null);
      return;
    }
    setArmed(null);
    setBusy(id);
    try {
      await onDelete(id);
    } catch {
      setFailed(id);
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="history">
      {sessions.slice(0, SHOWN).map((s) => (
        <div className="h-row" key={s.id}>
          <span className="h-date">{fmtDate(s.date, true)}</span>
          <div className="h-what">
            <b>{shortDayName(s.dayName)}</b>
            <span>
              {s.mainLift ? `${s.mainLift.label} top set ${s.mainLift.topWeight} lb · ` : ""}
              {s.setCount} sets
            </span>
          </div>
          <button type="button" className="h-del" data-armed={armed === s.id ? "1" : "0"} disabled={busy === s.id} onClick={() => click(s.id)}>
            {failed === s.id ? "Couldn't delete" : armed === s.id ? "Tap to confirm" : "Delete"}
          </button>
        </div>
      ))}
    </div>
  );
}
