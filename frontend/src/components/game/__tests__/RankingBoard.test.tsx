import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { GameProvider } from "../../../contexts/GameContext";
import { RankingBoard } from "../RankingBoard";
import type { RankingEntry } from "../../../types/game";

function wrapper({ children }: { children: ReactNode }) {
  return <GameProvider>{children}</GameProvider>;
}

const makeRanking = (n: number): RankingEntry[] =>
  Array.from({ length: n }, (_, i) => ({
    position: i + 1,
    teamName: `Equipo ${i + 1}`,
    score: (n - i) * 100,
  }));

describe("RankingBoard", () => {
  it("renders nothing for empty ranking", () => {
    const { container } = render(<RankingBoard ranking={[]} />, { wrapper });
    expect(container.firstChild).toBeNull();
  });

  it("renders heading and all entries", () => {
    render(<RankingBoard ranking={makeRanking(3)} />, { wrapper });

    expect(screen.getByText("Ranking en vivo")).toBeInTheDocument();
    expect(screen.getByText(/Equipo 1/)).toBeInTheDocument();
    expect(screen.getByText(/Equipo 2/)).toBeInTheDocument();
    expect(screen.getByText(/Equipo 3/)).toBeInTheDocument();
  });

  it("shows gold medal for first place", () => {
    render(<RankingBoard ranking={makeRanking(1)} />, { wrapper });
    expect(screen.getByText(/🥇/)).toBeInTheDocument();
  });

  it("shows silver medal for second place", () => {
    render(<RankingBoard ranking={makeRanking(2)} />, { wrapper });
    expect(screen.getByText(/🥈/)).toBeInTheDocument();
  });

  it("shows bronze medal for third place", () => {
    render(<RankingBoard ranking={makeRanking(3)} />, { wrapper });
    expect(screen.getByText(/🥉/)).toBeInTheDocument();
  });

  it("shows numeric position for places beyond top 3", () => {
    render(<RankingBoard ranking={makeRanking(5)} />, { wrapper });
    expect(screen.getByText(/#4/)).toBeInTheDocument();
    expect(screen.getByText(/#5/)).toBeInTheDocument();
  });

  it("renders score with pts suffix", () => {
    const ranking: RankingEntry[] = [{ position: 1, teamName: "Alpha", score: 420 }];
    render(<RankingBoard ranking={ranking} />, { wrapper });
    expect(screen.getByText("420 pts")).toBeInTheDocument();
  });

  it("applies podium class to top-3 rows", () => {
    const { container } = render(<RankingBoard ranking={makeRanking(4)} />, { wrapper });
    const rows = container.querySelectorAll(".ranking-row");

    expect(rows[0].classList.contains("podium")).toBe(true);
    expect(rows[1].classList.contains("podium")).toBe(true);
    expect(rows[2].classList.contains("podium")).toBe(true);
    expect(rows[3].classList.contains("podium")).toBe(false);
  });

  it("applies first/second/third classes to podium positions", () => {
    const { container } = render(<RankingBoard ranking={makeRanking(3)} />, { wrapper });
    const rows = container.querySelectorAll(".ranking-row");

    expect(rows[0].classList.contains("first")).toBe(true);
    expect(rows[1].classList.contains("second")).toBe(true);
    expect(rows[2].classList.contains("third")).toBe(true);
  });

  it("renders large rankings without crashing", () => {
    render(<RankingBoard ranking={makeRanking(20)} />, { wrapper });
    expect(screen.getByText(/Equipo 20/)).toBeInTheDocument();
    expect(screen.getByText(/#20/)).toBeInTheDocument();
  });
});
