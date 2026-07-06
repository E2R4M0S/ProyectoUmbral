# Frontend Spec — UMBRAL v2

## Stack

- **React 19** con hooks funcionales
- **TypeScript 6** — tipos explícitos, sin `any`
- **Vite 8** — bundler y dev server
- **react-router-dom v7** — navegación client-side
- **oidc-client-ts + react-oidc-context** — autenticación Keycloak
- **@microsoft/signalr** — WebSockets en tiempo real
- **Vitest + @testing-library/react** — tests de componentes

---

## Estructura de directorios

```
frontend/src/
├── main.tsx                  ← entry point, AuthProvider wrapping
├── App.tsx                   ← router y rutas protegidas
│
├── auth/
│   ├── AuthProvider.tsx      ← OidcProvider con config de Keycloak
│   ├── keycloak.ts           ← configuración OIDC (disablePKCE en dev)
│   ├── ProtectedRoute.tsx    ← redirige si no autenticado o sin rol
│   └── useAuth.ts            ← hook que expone user, token, logout
│
├── pages/
│   ├── admin/
│   │   ├── CatalogoMisiones.tsx   ← listado con filtros
│   │   ├── CrearMision.tsx        ← formulario de creación
│   │   ├── EditarMision.tsx       ← formulario de edición
│   │   ├── DetalleMision.tsx      ← detalle con etapas y pistas
│   │   ├── CrearSesion.tsx        ← selección de misión/es, genera PIN
│   │   ├── ListadoSesiones.tsx    ← sesiones con filtro por estado
│   │   ├── PanelSesion.tsx        ← dashboard en tiempo real del operador
│   │   ├── QuizBank.tsx           ← gestión de quizzes/preguntas
│   │   ├── CrearEquipo.tsx
│   │   ├── EditarEquipo.tsx
│   │   ├── EquipoDetalle.tsx
│   │   ├── ListadoEquipos.tsx
│   │   ├── CrearOperador.tsx
│   │   ├── DesactivarOperador.tsx
│   │   ├── ListadoUsuarios.tsx
│   │   └── DetalleUsuario.tsx
│   │
│   ├── operator/
│   │   ├── IniciarJuego.tsx       ← iniciar trivia, lanzar preguntas
│   │   └── QuestionResults.tsx    ← resultados de una pregunta
│   │
│   ├── participant/
│   │   ├── MiPerfil.tsx
│   │   ├── UnirseEquipo.tsx       ← ingresar JoinCode
│   │   ├── UnirseSesion.tsx       ← ingresar PIN de sesión
│   │   ├── NextStageRedirect.tsx
│   │   └── game/
│   │       ├── WaitingRoom.tsx    ← sala de espera (escucha SessionStatusChanged)
│   │       ├── ActiveGame.tsx     ← juego en curso (etapas, pistas, trivia)
│   │       ├── GameView.tsx       ← vista wrapper del juego
│   │       └── GameResults.tsx    ← resultados finales
│   │
│   └── public/
│       └── Registro.tsx           ← registro de nuevo participante
│
├── components/
│   └── game/
│       ├── ClueCard.tsx           ← tarjeta que muestra una pista liberada
│       ├── CountdownTimer.tsx     ← temporizador regresivo para preguntas de trivia
│       ├── QuestionCard.tsx       ← pregunta con opciones de respuesta
│       ├── RankingBoard.tsx       ← tabla de posiciones
│       └── Timer.tsx              ← temporizador general de sesión
│
├── services/
│   ├── api.ts                ← axios con interceptor Bearer token
│   ├── missionsApi.ts        ← CRUD misiones, etapas, pistas
│   ├── sessionsApi.ts        ← crear, transicionar, unirse, avanzar
│   ├── teamsApi.ts           ← equipos, unirse con código
│   ├── adminUsuariosApi.ts   ← usuarios via Teams.Service
│   ├── operadorApi.ts        ← crear/desactivar operadores
│   ├── perfilApi.ts          ← mi perfil (GET/PUT)
│   └── registroApi.ts        ← registro de participante
│
├── hooks/
│   └── useSignalR.ts         ← conecta a /hub/game, JoinSessionGroup
│
├── contexts/
│   └── GameContext.tsx        ← estado compartido durante la partida
│
└── types/
    ├── mission.ts             ← MissionDto, MissionDetailDto, StageDto, ClueDto
    ├── session.ts             ← SessionDto, SessionStatus, SessionProgressDto
    ├── team.ts                ← TeamDto, TeamMemberDto
    ├── game.ts                ← GameState, ClueEvent, QuestionEvent, LeaderboardEntry
    ├── perfil.ts              ← PerfilDto, UpdatePerfilRequest
    ├── usuario.ts             ← UserDto, UserDetailDto
    └── operador.ts            ← OperadorDto, CreateOperadorRequest
```

---

## Autenticación con Keycloak

```tsx
// auth/keycloak.ts
export const oidcConfig = {
  authority: `http://${window.location.hostname}:8080/realms/umbral`,
  client_id: 'umbral-frontend',
  redirect_uri: `${window.location.origin}/callback`,
  post_logout_redirect_uri: window.location.origin,
  response_type: 'code',
  scope: 'openid profile email',
  // PKCE requiere HTTPS — desactivado en dev HTTP
  disablePKCE: true,
};

// main.tsx
<AuthProvider {...oidcConfig}>
  <App />
</AuthProvider>

// useAuth.ts
export function useAuth() {
  const auth = useOidcAuth();
  return {
    user: auth.user,
    token: auth.user?.access_token,
    isAuthenticated: auth.isAuthenticated,
    roles: auth.user?.profile?.realm_access?.roles ?? [],
    logout: () => auth.removeUser(),
  };
}
```

---

## Cliente HTTP — api.ts

```tsx
// Agrega automáticamente el Bearer token a todos los requests
import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL || '',
});

api.interceptors.request.use((config) => {
  const user = JSON.parse(sessionStorage.getItem('oidc.user:...') || '{}');
  if (user?.access_token) {
    config.headers.Authorization = `Bearer ${user.access_token}`;
  }
  return config;
});

export default api;
```

---

## Routing por rol

```tsx
// App.tsx
<Routes>
  <Route path="/" element={<Landing />} />
  <Route path="/registro" element={<Registro />} />

  <Route path="/admin/*" element={
    <ProtectedRoute requiredRole="admin">
      <AdminRoutes />
    </ProtectedRoute>
  } />

  <Route path="/operator/*" element={
    <ProtectedRoute requiredRole="operator">
      <OperatorRoutes />
    </ProtectedRoute>
  } />

  <Route path="/participant/*" element={
    <ProtectedRoute requiredRole="participant">
      <ParticipantRoutes />
    </ProtectedRoute>
  } />

  <Route path="/juego/:sessionId" element={
    <ProtectedRoute>
      <GameView />
    </ProtectedRoute>
  } />
</Routes>
```

---

## Vite proxy — desarrollo local

```typescript
// vite.config.ts
server: {
  proxy: {
    '/api': {
      target: 'http://localhost:5000',
      changeOrigin: true,
    },
    '/hub': {
      target: 'http://localhost:5005',
      ws: true,
      changeOrigin: true,
    }
  }
}
```

---

## Tipos TypeScript importantes

```typescript
// session.ts
export type SessionStatus = 'Scheduled' | 'Preparing' | 'Active' | 'Paused' | 'Finished' | 'Cancelled';

export interface SessionDto {
  id: string;
  name: string;
  pin: string;
  status: SessionStatus;
  currentStageOrder: number;
  startedAt: string | null;
  endedAt: string | null;
  stages: SessionStageDto[];
  participants: SessionParticipantDto[];
}

// game.ts
export interface LeaderboardEntry {
  teamId: string;
  teamName: string;
  score: number;
  position: number;
}

export interface QuestionEvent {
  questionId: string;
  text: string;
  answers: { id: string; text: string }[];
  timeLimitSeconds: number;
}
```

---

## Consideraciones de UX

- **IP dinámica:** `window.location.hostname` en la config de Keycloak. Si cambias de red, agregar la nueva IP en los Redirect URIs del cliente `umbral-frontend` en Keycloak.
- **Participantes usan el teléfono:** las vistas `/participant/game/*` deben ser mobile-first.
- **Sin librerías UI:** todo con CSS plano. Evitar dependencias de componentes externos.
- **SignalR reconnect:** `withAutomaticReconnect()` ya configurado en `useSignalR.ts`.
