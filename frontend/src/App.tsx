import { BrowserRouter, Routes, Route } from "react-router-dom";
import { ProtectedRoute } from "./auth/ProtectedRoute";
import { Registro } from "./pages/public/Registro";
import { CrearOperador } from "./pages/admin/CrearOperador";

function Home() {
  return <h1>UMBRAL — Página pública</h1>;
}

function Dashboard() {
  return <h1>Dashboard — Autenticado</h1>;
}

function AdminPanel() {
  return <h1>Panel de Administración</h1>;
}

function OperatorPanel() {
  return <h1>Panel de Operador</h1>;
}

function ParticipantPanel() {
  return <h1>Panel de Participante</h1>;
}

function Callback() {
  return <div>Completando inicio de sesión...</div>;
}

function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/callback" element={<Callback />} />
        <Route path="/registro" element={<Registro />} />
        <Route element={<ProtectedRoute />}>
          <Route path="/dashboard" element={<Dashboard />} />
        </Route>
        <Route element={<ProtectedRoute requiredRole="admin" />}>
          <Route path="/admin" element={<AdminPanel />} />
          <Route path="/admin/operadores/nuevo" element={<CrearOperador />} />
        </Route>
        <Route element={<ProtectedRoute requiredRole="operator" />}>
          <Route path="/operator" element={<OperatorPanel />} />
        </Route>
        <Route element={<ProtectedRoute requiredRole="participant" />}>
          <Route path="/participant" element={<ParticipantPanel />} />
        </Route>
      </Routes>
    </BrowserRouter>
  );
}

export default App;
