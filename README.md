# SQLDBEntityNotifier

A comprehensive Change Data Capture (CDC) platform for .NET that provides real-time database change monitoring with pluggable delivery targets.

## Features

- **Multi-Database Support**: SQLite, PostgreSQL
- **Pluggable Publishers**: Kafka, Webhooks
- **Developer-Friendly**: dotnet new templates
- **Production-Ready**: Metrics, health checks, replay capabilities

## Quick Start

### 1. Install the Template

```bash
dotnet new install SqlDbEntityNotifier.Templates
```

### 2. Create a New Worker

```bash
dotnet new sqldbnotifier-worker -n MyCdcWorker
cd MyCdcWorker
```

### 3. Run the Worker

```bash
dotnet run
```

## Architecture

The platform consists of:
- **Database Adapters**: Monitor changes in SQLite, PostgreSQL
- **Publishers**: Send events to Kafka, Webhooks
- **Core Engine**: Manages subscriptions, filtering, and lifecycle

## Configuration

Edit `appsettings.json` to configure your database and publisher:

```json
{
  "SqlDbEntityNotifier": {
    "Adapters": {
      "Sqlite": {
        "FilePath": "app.db",
        "Source": "my-app"
      }
    },
    "Publishers": {
      "Webhook": {
        "EndpointUrl": "http://localhost:8080/webhook"
      }
    }
  }
}
```

## Development

### Building

```bash
dotnet build
```

### Testing

```bash
# Unit tests
dotnet test

# Integration tests (requires Docker)
docker-compose up -d
dotnet test --filter Category=Integration
```

## Roadmap

- **Phase 1**: Core platform (✅ Complete)
- **Phase 2**: Additional publishers and serializers
- **Phase 3**: Operations and security features
- **Phase 4**: Enterprise features

## License

MIT License