// Single source of truth for "how much time is left in the current Treasure mission",
// used by both the operator's PanelSesion and each participant's Timer so every viewer
// counts down from the exact same numbers instead of drifting local clocks.
//
// missionElapsedSeconds is anchored server-side to when the CURRENT mission actually started
// (Session.CurrentMissionStartedAt) — not a running sum of each prior mission's declared
// duration, which drifts (and can make the next mission's timer look frozen) whenever a
// mission finishes earlier or later than its estimated TimeMinutes.
export function computeMissionRemaining(
  missionTimeMinutes: number | null | undefined,
  missionElapsedSeconds: number,
): number | null {
  if (missionTimeMinutes == null) return null;
  const missionTotal = missionTimeMinutes === -1 ? 10 : Math.max(0, missionTimeMinutes) * 60;
  return missionTotal > 0 ? Math.max(0, missionTotal - missionElapsedSeconds) : null;
}
