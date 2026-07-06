import { describe, it, expect, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";

vi.mock("../../../services/registroApi", () => ({
  registrarParticipante: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) { super(body); }
  },
}));

import { Registro } from "../Registro";

function wrap(ui: React.ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

describe("Registro (página pública)", () => {
  it("renders the registration form", () => {
    wrap(<Registro />);
    expect(screen.getByRole("button", { name: /registrarse/i })).toBeInTheDocument();
  });

  it("contains name, alias, email and password fields", () => {
    wrap(<Registro />);
    expect(screen.getByLabelText(/nombre/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/alias/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument();
  });
});
