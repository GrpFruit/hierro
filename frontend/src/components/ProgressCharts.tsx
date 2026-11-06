"use client";

import { useState, type PointerEvent } from "react";
import { fmtDate } from "@/lib/format";
import type { LiftProgress } from "@/lib/types";

const W = 320, H = 150, L = 38, R = 14, T = 12, B = 24;

function niceStep(raw: number) {
  for (const s of [5, 10, 20, 25, 50, 100, 200]) if (s >= raw) return s;
  return 500;
}

function LiftChart({ points }: { points: LiftProgress["points"] }) {
  const [hover, setHover] = useState<number | null>(null);

  const pts = points.map((p) => ({ ...p, t: Date.parse(`${p.date}T12:00:00`) }));
  let lo = Math.min(...pts.map((p) => p.weight));
  let hi = Math.max(...pts.map((p) => p.weight));
  if (hi - lo < 20) {
    const m = (hi + lo) / 2;
    lo = m - 10;
    hi = m + 10;
  }
  const step = niceStep((hi - lo) / 3);
  lo = Math.max(0, Math.floor(lo / step) * step);
  hi = Math.ceil(hi / step) * step;

  const t0 = pts[0].t, t1 = pts[pts.length - 1].t;
  const x = (t: number) => (t1 === t0 ? (L + W - R) / 2 : L + ((t - t0) / (t1 - t0)) * (W - L - R));
  const y = (v: number) => T + ((hi - v) / (hi - lo)) * (H - T - B);
  const placed = pts.map((p) => ({ ...p, x: x(p.t), y: y(p.weight) }));

  const ticks: number[] = [];
  for (let v = lo; v <= hi + 0.001; v += step) ticks.push(v);
  const line = placed.map((p, j) => `${j ? "L" : "M"}${p.x.toFixed(1)} ${p.y.toFixed(1)}`).join(" ");
  const area = placed.length > 1 ? `${line} L${x(t1).toFixed(1)} ${y(lo)} L${x(t0).toFixed(1)} ${y(lo)} Z` : null;

  function onMove(e: PointerEvent<SVGSVGElement>) {
    const rect = e.currentTarget.getBoundingClientRect();
    const vx = ((e.clientX - rect.left) / rect.width) * W;
    let best = 0;
    placed.forEach((p, j) => {
      if (Math.abs(p.x - vx) < Math.abs(placed[best].x - vx)) best = j;
    });
    setHover(best);
  }

  const h = hover == null ? null : placed[hover];
  return (
    <div className="chart-plot">
      <svg viewBox={`0 0 ${W} ${H}`} role="img" aria-label="Line chart of heaviest set per session" onPointerMove={onMove} onPointerLeave={() => setHover(null)}>
        {ticks.map((v) => (
          <g key={v}>
            <line className="g-grid" x1={L} x2={W - R} y1={y(v)} y2={y(v)} />
            <text className="g-axis" x={L - 6} y={y(v) + 3} textAnchor="end">{v}</text>
          </g>
        ))}
        <text className="g-axis" x={t1 === t0 ? x(t0) : L} y={H - 6} textAnchor={t1 === t0 ? "middle" : "start"}>{fmtDate(pts[0].date)}</text>
        {t1 !== t0 && <text className="g-axis" x={W - R} y={H - 6} textAnchor="end">{fmtDate(pts[pts.length - 1].date)}</text>}
        {area && <path className="g-area" d={area} />}
        <path className="g-line" d={line} />
        {placed.map((p, j) => (
          <circle key={j} className="g-dot" cx={p.x.toFixed(1)} cy={p.y.toFixed(1)} r={j === placed.length - 1 ? 5.5 : 4} />
        ))}
        {h && <line className="g-cross" y1={T} y2={H - B} x1={h.x} x2={h.x} />}
        <rect x={L} y={0} width={W - L - R} height={H} fill="transparent" />
      </svg>
      {h && (
        <div className="tooltip" style={{ left: `${(h.x / W) * 100}%`, top: `${(h.y / H) * 100}%` }}>
          {fmtDate(h.date, true)} · {h.weight} lb × {h.reps}
        </div>
      )}
    </div>
  );
}

export function ProgressCharts({ lifts }: { lifts: LiftProgress[] }) {
  return (
    <div className="charts">
      {lifts.map((lift) => {
        const pts = lift.points;
        if (!pts.length) {
          return (
            <div className="chart" key={lift.name}>
              <p className="eyebrow">{lift.label}</p>
              <div className="big">—</div>
              <p className="empty">No sessions yet. Log a {lift.label.toLowerCase()} day and your line starts here.</p>
            </div>
          );
        }
        const first = pts[0], last = pts[pts.length - 1], d = last.weight - first.weight;
        return (
          <div className="chart" key={lift.name}>
            <p className="eyebrow">{lift.label}</p>
            <div className="big">
              {last.weight} <small>lb × {last.reps}</small>
            </div>
            <p className="delta">
              {pts.length > 1 ? `${d >= 0 ? "+" : "−"}${Math.abs(d)} lb since ${fmtDate(first.date)}` : `First logged ${fmtDate(first.date)}`}
            </p>
            <LiftChart points={pts} />
          </div>
        );
      })}
    </div>
  );
}
