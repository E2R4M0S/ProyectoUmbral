import type { RankingEntry, MyTeam } from "../types/game";

// Team rows in the live ranking carry the team's own id in `userId` instead of a participant
// id (see GameNotifier.NotifyRankingUpdatedAsync / RealTimeHub's LeaderboardUpdated endpoint on
// the backend, which overload that field). Match case-insensitively (defensive against any GUID
// casing difference between services) and fall back to matching by team name — unique per
// session — so a participant on a team still gets recognized as "me" even if the id comparison
// ever fails to line up.
export function isMyRankingEntry(entry: RankingEntry, myUserId: string | null, myTeam: MyTeam | null): boolean {
  if (myUserId != null && entry.userId != null && entry.userId.toLowerCase() === myUserId.toLowerCase()) {
    return true;
  }
  if (myTeam != null) {
    if (entry.userId != null && entry.userId.toLowerCase() === myTeam.id.toLowerCase()) return true;
    if (entry.teamName === myTeam.name) return true;
  }
  return false;
}
