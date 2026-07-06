# Product Spec — UMBRAL v2

## Visión del Producto

UMBRAL es una plataforma web para operar en tiempo real **experiencias de investigación inmersiva** tipo escape room académico. Permite a un operador gestionar partidas en vivo donde equipos de participantes resuelven misiones de tipo Búsqueda del Tesoro o Trivia, con comunicación en tiempo real vía WebSockets.

**Proyecto académico:** UCAB — Desarrollo de Software, 2026  
**Equipo:** Eros Dos Ramos (30.371.156) · Adrián Cereijo (29.756.926)

---

## Actores y Casos de Uso Principales

### Administrador
- Crear/editar/activar misiones con etapas y pistas
- Crear quizzes de trivia con preguntas, opciones y respuesta correcta
- Gestionar operadores (crear, desactivar)
- Ver listado de todos los usuarios

### Operador
- Crear sesiones de juego (elige misiones del catálogo)
- Preparar sala de espera (comparte PIN con equipos)
- Iniciar, pausar, finalizar sesión
- Ver dashboard en tiempo real del progreso de cada equipo
- Liberar pistas manualmente a equipos específicos
- En trivia: lanzar preguntas, ver respuestas, ver ranking

### Participante (desde el teléfono)
- Registrarse en la plataforma
- Unirse a un equipo con JoinCode
- Unirse a una sesión con PIN
- Sala de espera → juego activo (pistas, timer) → resultados finales
- En trivia: responder preguntas, ver temporizador, ver respuesta correcta, ver ranking

---

## Flujos de Juego

### Flujo Búsqueda del Tesoro
```
1. Admin crea misión tipo Treasure con etapas y pistas
2. Operador crea sesión → obtiene PIN de 6 dígitos
3. Participantes se unen con PIN → WaitingRoom
4. Operador inicia → participantes ven ActiveGame
5. Operador monitorea progreso por equipo
6. Operador avanza etapas manualmente
7. Operador libera pistas (penaliza puntos)
8. Operador finaliza → participantes ven GameResults con ranking
```

### Flujo Trivia
```
1. Admin crea quiz con preguntas de opción múltiple (2-4 opciones, 1 correcta)
2. Admin crea misión tipo Trivia vinculada al quiz
3. Operador crea sesión → obtiene PIN
4. Participantes se unen → WaitingRoom
5. Operador inicia trivia
6. Operador lanza pregunta → aparece en todos los teléfonos con temporizador
7. Participantes responden (bloqueo inmediato tras selección)
8. Fin de tiempo → operador cierra pregunta → se muestra respuesta correcta
9. Ranking actualizado vía RabbitMQ → SignalR (automático)
10. Operador lanza siguiente pregunta → ...
11. Operador finaliza → podio con top 3
```

---

## Las 47 Historias de Usuario

> Ver `CLAUDE.md` en la raíz del proyecto para las HUs completas con criterios de aceptación.

### Estado de implementación

| Módulo | HUs | Estado |
|--------|-----|--------|
| Accesos y Usuarios | HU-01 a HU-06 | Completo |
| Misiones | HU-07 a HU-14 | Completo |
| Equipos | HU-15 a HU-19 | Completo |
| Sesiones en Vivo | HU-20 a HU-27 | Completo (incl. multi-misión) |
| Gestión de Trivias | HU-28 a HU-35 | Implementado (pendiente merge a develop) |
| Trivia en Vivo | HU-36 a HU-45 | Backend parcial — frontend pendiente |
| Resultados | HU-46 a HU-47 | Pendiente |

---

## Reglas de Negocio Clave

| Código | Regla |
|--------|-------|
| RB-01 | Una misión solo puede usarse en sesiones si está Activa |
| RB-02 | Una sesión no puede iniciar sin al menos un participante unido |
| RB-03 | No se aceptan respuestas si la sesión está Pausada, Finalizada o Cancelada |
| RB-04 | Una pista no se libera dos veces al mismo equipo en la misma etapa |
| RB-07 | El puntaje tiene trazabilidad: pistas usadas penalizan, tiempo afecta scoring en trivia |
| RB-08 | Ranking por puntaje descendente; tiempo como desempate |
| RB-09 | Los cambios de estado respetan transiciones válidas (State Pattern) |

---

## Constraints del Producto

- **Sin formulario de login propio** — siempre redirige a Keycloak
- **PIN numérico de 6 dígitos** para unirse a sesiones
- **JoinCode alfanumérico de 6 caracteres** para unirse a equipos
- **Trivia:** mínimo 2, máximo 4 opciones por pregunta; exactamente 1 respuesta correcta
- **UI sin librerías de componentes externas** (no Tailwind, no MUI)
- **Código y commits en inglés**

---

## Criterios de Calidad

- Cobertura de tests backend >= 90%
- Pipeline CI/CD con GitHub Actions (build + test)
- Reconexión automática de WebSockets
- Tiempo de respuesta de API < 500ms para operaciones normales
- Keycloak como única fuente de verdad de identidades
