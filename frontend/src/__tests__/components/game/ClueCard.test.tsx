import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { ClueCard } from "../../../components/game/ClueCard";

describe("ClueCard", () => {
  it("renders empty state for null clue", () => {
    render(<ClueCard clue={null} />);
    expect(screen.getByText("Pista vacía")).toBeInTheDocument();
  });

  it("renders text clue", () => {
    render(<ClueCard clue={{ text: "Busca bajo el puente" }} />);
    expect(screen.getByText("Busca bajo el puente")).toBeInTheDocument();
  });

  it("renders number clue", () => {
    render(<ClueCard clue={{ number: 42 }} />);
    expect(screen.getByText("42")).toBeInTheDocument();
  });

  it("renders image clue", () => {
    render(<ClueCard clue={{ imageUrl: "https://example.com/img.png" }} />);
    const img = screen.getByAltText("Pista");
    expect(img).toBeInTheDocument();
    expect(img).toHaveAttribute("src", "https://example.com/img.png");
  });

  it("renders location clue", () => {
    render(<ClueCard clue={{ latitude: -34.6, longitude: -58.4 }} />);
    expect(screen.getByText("-34.6, -58.4")).toBeInTheDocument();
  });

  it("renders string clue", () => {
    render(<ClueCard clue="simple string clue" />);
    expect(screen.getByText("simple string clue")).toBeInTheDocument();
  });

  it("renders JSON fallback for unknown object", () => {
    render(<ClueCard clue={{ foo: "bar", num: 123 }} />);
    expect(screen.getByText(/foo/)).toBeInTheDocument();
    expect(screen.getByText(/bar/)).toBeInTheDocument();
  });
});
