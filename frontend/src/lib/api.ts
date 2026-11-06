import type { LiftProgress, Plan, SessionEntry, SessionSummary, Sheet } from "./types";

// Requests go to /api on this origin; next.config.ts rewrites them to the .NET backend.

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: { Accept: "application/json", ...(init?.body ? { "Content-Type": "application/json" } : {}) },
  });
  if (!res.ok) throw new ApiError(res.status, await problemMessage(res));
  return (res.status === 204 ? undefined : await res.json()) as T;
}

/** Pulls the first readable message out of an ASP.NET Core problem-details response. */
async function problemMessage(res: Response): Promise<string> {
  try {
    const body = await res.json();
    const first = body?.errors && Object.values(body.errors as Record<string, string[]>)[0]?.[0];
    return first || body?.detail || body?.title || res.statusText;
  } catch {
    return res.statusText || `Request failed (${res.status})`;
  }
}

export const api = {
  plans: () => request<Plan[]>("/api/plans"),
  sheet: (planId: string, dayId: string, date: string, signal?: AbortSignal) =>
    request<Sheet>(`/api/plans/${encodeURIComponent(planId)}/days/${encodeURIComponent(dayId)}/sheet?date=${date}`, { signal }),
  sessions: () => request<SessionSummary[]>("/api/sessions"),
  progress: () => request<LiftProgress[]>("/api/progress"),
  saveSession: (date: string, dayId: string, planId: string, entries: SessionEntry[]) =>
    request<SessionSummary>(`/api/sessions/${date}/${encodeURIComponent(dayId)}`, {
      method: "PUT",
      body: JSON.stringify({ planId, entries }),
    }),
  deleteSession: (id: string) => request<void>(`/api/sessions/${encodeURIComponent(id)}`, { method: "DELETE" }),
};
