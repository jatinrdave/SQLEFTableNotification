# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2025-01-09

### Added
- Initial release of SQLDBEntityNotifier CDC platform
- Core abstractions and interfaces (`IEntityNotifier`, `IChangePublisher`, `IDbAdapter`)
- SQLite adapter with change log table and trigger-based monitoring
- PostgreSQL adapter with logical replication support (wal2json plugin)
- Kafka publisher with Confluent.Kafka integration and retry policies
- Webhook publisher with HMAC signing and OAuth2 support
- JSON serializer with System.Text.Json
- Dependency injection extensions and configuration support
- Integration tests with Docker Compose setup
- dotnet new template for quick project scaffolding
- Sample worker application demonstrating usage
- Comprehensive documentation and README

### Features
- **Multi-Database Support**: SQLite and PostgreSQL adapters
- **Pluggable Publishers**: Kafka and Webhook publishers
- **Developer Experience**: dotnet new template, configuration-based setup
- **Production Ready**: Retry policies, error handling, structured logging
- **Security**: HMAC signing, OAuth2 support, HTTPS enforcement
- **Observability**: Structured logging, health checks, metrics support

### Technical Details
- Target Framework: .NET 8.0
- Async/await patterns throughout
- CancellationToken support
- Comprehensive XML documentation
- Unit and integration test coverage
- Docker Compose for local development and testing

## [Unreleased]

### Planned
- RabbitMQ publisher
- Azure Event Hubs publisher
- Protobuf and Avro serializers
- LINQ filter engine
- Code generator for DTOs
- Prometheus metrics integration
- Schema change detection
- PII masking capabilities
- MySQL and Oracle adapters
- Advanced monitoring dashboards