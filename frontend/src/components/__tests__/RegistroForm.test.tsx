import { describe, it, expect, vi, beforeEach } from "vitest";
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import { RegistroForm } from "../RegistroForm";

vi.mock("../../services/registroApi", () => ({
  registrarParticipante: vi.fn(),
  ApiError: class ApiError extends Error {
    constructor(public status: number, public body: string) {
      super(`API Error: ${status}`);
      this.name = "ApiError";
    }
  },
}));

import { registrarParticipante, ApiError } from "../../services/registroApi";

const mockRegistrar = vi.mocked(registrarParticipante);

describe("RegistroForm", () => {
  const onSuccess = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders all form fields and the submit button", () => {
    render(<RegistroForm onSuccess={onSuccess} />);
    expect(screen.getByLabelText("Nombre")).toBeInTheDocument();
    expect(screen.getByLabelText("Apellido")).toBeInTheDocument();
    expect(screen.getByLabelText("Nombre de usuario")).toBeInTheDocument();
    expect(screen.getByLabelText(/alias/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/email/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/contraseña/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /registrarse/i })).toBeInTheDocument();
  });

  it("shows validation errors when submitting empty form", async () => {
    render(<RegistroForm onSuccess={onSuccess} />);
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() => {
      expect(screen.getByText(/nombre es obligatorio/i)).toBeInTheDocument();
      expect(screen.getByText(/apellido es obligatorio/i)).toBeInTheDocument();
      expect(screen.getByText(/usuario es obligatorio/i)).toBeInTheDocument();
      expect(screen.getByText(/alias es obligatorio/i)).toBeInTheDocument();
      expect(screen.getByText(/email es obligatorio/i)).toBeInTheDocument();
      expect(screen.getByText(/contraseña es obligatoria/i)).toBeInTheDocument();
    });

    expect(mockRegistrar).not.toHaveBeenCalled();
  });

  it("shows alias length error when alias is too short", async () => {
    render(<RegistroForm onSuccess={onSuccess} />);
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "ab" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/mínimo 3 caracteres/i)).toBeInTheDocument()
    );
  });

  it("shows alias format error for invalid characters", async () => {
    render(<RegistroForm onSuccess={onSuccess} />);
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "user name!" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/solo letras, números y guiones bajos/i)).toBeInTheDocument()
    );
  });

  it("shows email format error for invalid email", async () => {
    render(<RegistroForm onSuccess={onSuccess} />);
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "notanemail" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/formato de email inválido/i)).toBeInTheDocument()
    );
  });

  it("shows password length error when password is too short", async () => {
    render(<RegistroForm onSuccess={onSuccess} />);
    fireEvent.change(screen.getByLabelText(/contraseña/i), { target: { value: "short" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/mínimo 8 caracteres/i)).toBeInTheDocument()
    );
  });

  it("calls registrarParticipante and onSuccess on valid submission", async () => {
    mockRegistrar.mockResolvedValue({ id: "user-123" });
    render(<RegistroForm onSuccess={onSuccess} />);

    fireEvent.change(screen.getByLabelText("Nombre"), { target: { value: "John" } });
    fireEvent.change(screen.getByLabelText("Apellido"), { target: { value: "Doe" } });
    fireEvent.change(screen.getByLabelText("Nombre de usuario"), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "jdoe123" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "john@test.com" } });
    fireEvent.change(screen.getByLabelText(/contraseña/i), { target: { value: "password123" } });

    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() => expect(onSuccess).toHaveBeenCalledOnce());
    expect(mockRegistrar).toHaveBeenCalledWith({
      firstName: "John",
      lastName: "Doe",
      username: "jdoe",
      alias: "jdoe123",
      email: "john@test.com",
      password: "password123",
    });
  });

  it("shows conflict error on 409 response", async () => {
    mockRegistrar.mockRejectedValue(new ApiError(409, JSON.stringify({ details: [{ field: "alias", message: "ya está registrado." }] })));
    render(<RegistroForm onSuccess={onSuccess} />);

    fireEvent.change(screen.getByLabelText("Nombre"), { target: { value: "John" } });
    fireEvent.change(screen.getByLabelText("Apellido"), { target: { value: "Doe" } });
    fireEvent.change(screen.getByLabelText("Nombre de usuario"), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "john@test.com" } });
    fireEvent.change(screen.getByLabelText(/contraseña/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/El alias ya está registrado/i)).toBeInTheDocument()
    );
    expect(onSuccess).not.toHaveBeenCalled();
  });

  it("shows generic error on non-409 ApiError", async () => {
    mockRegistrar.mockRejectedValue(new ApiError(500, "Internal server error"));
    render(<RegistroForm onSuccess={onSuccess} />);

    fireEvent.change(screen.getByLabelText("Nombre"), { target: { value: "John" } });
    fireEvent.change(screen.getByLabelText("Apellido"), { target: { value: "Doe" } });
    fireEvent.change(screen.getByLabelText("Nombre de usuario"), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "john@test.com" } });
    fireEvent.change(screen.getByLabelText(/contraseña/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/Error del servidor/i)).toBeInTheDocument()
    );
  });

  it("shows connection error on network failure", async () => {
    mockRegistrar.mockRejectedValue(new Error("Network error"));
    render(<RegistroForm onSuccess={onSuccess} />);

    fireEvent.change(screen.getByLabelText("Nombre"), { target: { value: "John" } });
    fireEvent.change(screen.getByLabelText("Apellido"), { target: { value: "Doe" } });
    fireEvent.change(screen.getByLabelText("Nombre de usuario"), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "john@test.com" } });
    fireEvent.change(screen.getByLabelText(/contraseña/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByText(/Error de conexión/i)).toBeInTheDocument()
    );
  });

  it("disables button while submitting", async () => {
    let resolve: (v: any) => void;
    mockRegistrar.mockReturnValue(new Promise((r) => (resolve = r)));
    render(<RegistroForm onSuccess={onSuccess} />);

    fireEvent.change(screen.getByLabelText("Nombre"), { target: { value: "John" } });
    fireEvent.change(screen.getByLabelText("Apellido"), { target: { value: "Doe" } });
    fireEvent.change(screen.getByLabelText("Nombre de usuario"), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/alias/i), { target: { value: "jdoe" } });
    fireEvent.change(screen.getByLabelText(/email/i), { target: { value: "john@test.com" } });
    fireEvent.change(screen.getByLabelText(/contraseña/i), { target: { value: "password123" } });
    fireEvent.click(screen.getByRole("button", { name: /registrarse/i }));

    await waitFor(() =>
      expect(screen.getByRole("button", { name: /registrando/i })).toBeDisabled()
    );

    resolve!({ id: "x" });
    await waitFor(() => expect(onSuccess).toHaveBeenCalled());
  });
});
