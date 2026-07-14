import { useNavigate, useParams } from "react-router-dom";

export function MisionCompletada() {
  const navigate = useNavigate();
  const { sessionId } = useParams<{ sessionId: string }>();

  function handleReturn() {
    if (sessionId) sessionStorage.removeItem(`joined_${sessionId}`);
    navigate("/", { replace: true });
  }

  return (
    <div className="mision-completada">
      <div className="mision-completada-icon">🏆</div>
      <h2>¡Misión Completada!</h2>
      <p>Encontraste todas las ubicaciones. ¡Excelente trabajo!</p>
      <button onClick={handleReturn} className="btn btn-primary">
        Volver al inicio
      </button>
    </div>
  );
}
