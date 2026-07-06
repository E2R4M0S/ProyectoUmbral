import React from "react";
import { describe, it, test, expect, vi } from "vitest";
import { act } from "@testing-library/react";
import { render, screen, fireEvent } from "@testing-library/react";
import { ClueCard } from "../ClueCard";

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

  test("renders options and reacts to QuestionClosed event", () => {
    const clue = {
      questionId: "q1",
      title: "Pregunta",
      options: [
        { id: "a", text: "Opción A" },
        { id: "b", text: "Opción B" }
      ]
    };

    render(<ClueCard clue={clue} />);

    const buttonA = screen.getByText("Opción A");
    const buttonB = screen.getByText("Opción B");

    fireEvent.click(buttonA);
    expect(buttonA).toHaveClass("selected");

    act(() => {
      const ev = new CustomEvent("QuestionClosed", { detail: { questionId: "q1", correctAnswerId: "b" } });
      window.dispatchEvent(ev);
    });

    expect(buttonB).toHaveClass("correct");
    expect(buttonA).toHaveClass("wrong");
    expect(screen.getByText(/Error|¡Acierto!/)).toBeInTheDocument();
  });
});
