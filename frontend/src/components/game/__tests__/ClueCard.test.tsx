import React from "react";
import { render, screen, fireEvent } from "@testing-library/react";
import { ClueCard } from "../ClueCard";

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
  expect(buttonA).toHaveClass("bg-sky-600");

  // Simulate QuestionClosed event with correct answer = b
  const ev = new CustomEvent("QuestionClosed", { detail: { questionId: "q1", correctAnswerId: "b" } });
  window.dispatchEvent(ev);

  // After closed, correct (b) should be green and selected (a) red
  expect(buttonB).toHaveClass("bg-green-600");
  expect(buttonA).toHaveClass("bg-red-600");
  expect(screen.getByText(/Error|¡Acierto!/)).toBeInTheDocument();
});
