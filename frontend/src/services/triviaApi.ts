import { fetchWithAuth } from "./api";
import type { RankingEntry } from "../types/game";

export interface QuizSummary {
  id: string;
  title: string;
}

export async function listQuizzes(): Promise<QuizSummary[]> {
  const response = await fetchWithAuth("/api/quizzes", { method: "GET" });
  if (!response.ok) throw new Error(`listQuizzes: ${response.status}`);
  return response.json();
}

export async function getRankingByQuiz(quizId: string): Promise<RankingEntry[]> {
  const response = await fetchWithAuth(`/api/trivia/ranking/${quizId}`, { method: "GET" });
  if (!response.ok) return [];
  return response.json();
}
