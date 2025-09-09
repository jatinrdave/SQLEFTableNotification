# SQLDBEntityNotifier

A comprehensive Change Data Capture (CDC) platform for .NET that provides real-time database change monitoring with pluggable delivery targets.

## Features

- **Multi-Database Support**: SQLite, PostgreSQL
- **Pluggable Publishers**: Kafka, RabbitMQ, Azure Event Hubs, Webhooks
- **Multiple Serializers**: JSON, Protobuf (with Avro planned)
- **Advanced Filtering**: LINQ expression filters with compilation
- **Developer-Friendly**: dotnet new templates, code generation
- **Production-Ready**: Prometheus metrics, health checks, replay capabilities
- **Enterprise Security**: PII masking, HMAC signing, OAuth2 support

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
- **Phase 2**: Additional publishers and serializers (✅ 80% Complete)
  - ✅ RabbitMQ publisher
  - ✅ Azure Event Hubs publisher  
  - ✅ Protobuf serializer
  - ✅ LINQ filter engine
  - 📋 Avro serializer (planned)
  - 📋 Code generator (planned)
- **Phase 3**: Operations and security features (✅ 75% Complete)
  - ✅ Prometheus metrics
  - ✅ Health checks
  - ✅ Replay management
  - ✅ PII masking
  - 📋 Schema change detection (planned)
  - 📋 OpenTelemetry tracing (planned)
- **Phase 4**: Enterprise features (📋 Planned)
  - 📋 MySQL and Oracle adapters
  - 📋 Multi-tenant support
  - 📋 Advanced monitoring dashboards

## License

MIT License