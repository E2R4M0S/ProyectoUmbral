RabbitMQ local setup

1. Start RabbitMQ with Docker Compose

   docker compose up -d

   This will start a RabbitMQ broker with the management UI available at http://localhost:15672 (user: guest / pass: guest).

2. Application configuration

   The Sessions.Service reads RabbitMQ settings from configuration keys under `RabbitMq`:

   - RabbitMq:Host (default: localhost)
   - RabbitMq:Port (default: 5672)
   - RabbitMq:User (default: guest)
   - RabbitMq:Password (default: guest)
   - RabbitMq:Exchange (default: trivia.exchange)

   You can add those keys to `appsettings.Development.json` for local runs.

3. Testing the flow

   - Ensure the Sessions.Api / Sessions.Service is running and its configuration points to the broker above.
   - Trigger an action that publishes an event (for example finish a session via the API). The publisher uses a Topic exchange `trivia.exchange` and routing keys like `session.status.changed`.
