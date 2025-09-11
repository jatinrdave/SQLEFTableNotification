# SQLDBEntityNotifier - Installation Guide

## Table of Contents

1. [Prerequisites](#prerequisites)
2. [Installation Methods](#installation-methods)
3. [Database Setup](#database-setup)
4. [Publisher Setup](#publisher-setup)
5. [Configuration](#configuration)
6. [Verification](#verification)
7. [Troubleshooting](#troubleshooting)

## Prerequisites

### System Requirements

- **.NET 6.0 or later** (.NET 7.0+ recommended)
- **Windows 10/11, macOS 10.15+, or Linux** (Ubuntu 18.04+, CentOS 7+)
- **Minimum 2GB RAM** (4GB+ recommended)
- **Minimum 1GB disk space** (5GB+ recommended for development)

### Database Requirements

#### PostgreSQL
- **Version**: 10.0 or later (12.0+ recommended)
- **Extensions**: `pgoutput` (included by default)
- **Privileges**: `REPLICATION` privilege for the user
- **Configuration**: `wal_level = logical`

#### MySQL
- **Version**: 5.7 or later (8.0+ recommended)
- **Configuration**: `log_bin = ON`, `binlog_format = ROW`
- **Privileges**: `REPLICATION SLAVE` privilege for the user

#### SQLite
- **Version**: 3.7.0 or later (3.35.0+ recommended)
- **Configuration**: WAL mode enabled
- **File permissions**: Read/write access to database file

#### Oracle
- **Version**: 11g or later (19c+ recommended)
- **Configuration**: Supplemental logging enabled
- **Privileges**: `SELECT` on `V_$DATABASE`, `V_$LOG`, etc.

### Publisher Requirements

#### Kafka
- **Version**: 2.8.0 or later (3.0+ recommended)
- **Java**: OpenJDK 11 or later
- **Memory**: Minimum 1GB heap size

#### RabbitMQ
- **Version**: 3.8.0 or later (3.11+ recommended)
- **Erlang**: 23.0 or later
- **Memory**: Minimum 512MB

#### Webhook
- **HTTP/HTTPS endpoint** accessible from the application
- **SSL/TLS certificate** (for HTTPS)
- **Authentication** (if required)

#### Azure Event Hubs
- **Azure subscription** with Event Hubs namespace
- **Connection string** with appropriate permissions
- **Event Hub** created in the namespace

## Installation Methods

### Method 1: NuGet Package Manager

#### Visual Studio
1. Open your project in Visual Studio
2. Right-click on your project in Solution Explorer
3. Select "Manage NuGet Packages"
4. Go to the "Browse" tab
5. Search for "SqlDbEntityNotifier"
6. Select the desired packages and click "Install"

#### Package Manager Console
```powershell
# Core package
Install-Package SqlDbEntityNotifier.Core

# Database adapters
Install-Package SqlDbEntityNotifier.Adapters.Postgres
Install-Package SqlDbEntityNotifier.Adapters.Sqlite
Install-Package SqlDbEntityNotifier.Adapters.MySQL
Install-Package SqlDbEntityNotifier.Adapters.Oracle

# Publishers
Install-Package SqlDbEntityNotifier.Publisher.Kafka
Install-Package SqlDbEntityNotifier.Publisher.RabbitMQ
Install-Package SqlDbEntityNotifier.Publisher.Webhook
Install-Package SqlDbEntityNotifier.Publisher.AzureEventHubs

# Serializers
Install-Package SqlDbEntityNotifier.Serializers.Json
Install-Package SqlDbEntityNotifier.Serializers.Protobuf
Install-Package SqlDbEntityNotifier.Serializers.Avro

# Advanced features
Install-Package SqlDbEntityNotifier.MultiTenant
Install-Package SqlDbEntityNotifier.Transactional
Install-Package SqlDbEntityNotifier.Delivery

# Monitoring
Install-Package SqlDbEntityNotifier.Monitoring
Install-Package SqlDbEntityNotifier.Tracing
```

### Method 2: .NET CLI

```bash
# Core package
dotnet add package SqlDbEntityNotifier.Core

# Database adapters
dotnet add package SqlDbEntityNotifier.Adapters.Postgres
dotnet add package SqlDbEntityNotifier.Adapters.Sqlite
dotnet add package SqlDbEntityNotifier.Adapters.MySQL
dotnet add package SqlDbEntityNotifier.Adapters.Oracle

# Publishers
dotnet add package SqlDbEntityNotifier.Publisher.Kafka
dotnet add package SqlDbEntityNotifier.Publisher.RabbitMQ
dotnet add package SqlDbEntityNotifier.Publisher.Webhook
dotnet add package SqlDbEntityNotifier.Publisher.AzureEventHubs

# Serializers
dotnet add package SqlDbEntityNotifier.Serializers.Json
dotnet add package SqlDbEntityNotifier.Serializers.Protobuf
dotnet add package SqlDbEntityNotifier.Serializers.Avro

# Advanced features
dotnet add package SqlDbEntityNotifier.MultiTenant
dotnet add package SqlDbEntityNotifier.Transactional
dotnet add package SqlDbEntityNotifier.Delivery

# Monitoring
dotnet add package SqlDbEntityNotifier.Monitoring
dotnet add package SqlDbEntityNotifier.Tracing
```

### Method 3: PackageReference in .csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net7.0</TargetFramework>
  </PropertyGroup>

  <ItemGroup>
    <!-- Core package -->
    <PackageReference Include="SqlDbEntityNotifier.Core" Version="1.0.0" />
    
    <!-- Database adapters -->
    <PackageReference Include="SqlDbEntityNotifier.Adapters.Postgres" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Adapters.Sqlite" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Adapters.MySQL" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Adapters.Oracle" Version="1.0.0" />
    
    <!-- Publishers -->
    <PackageReference Include="SqlDbEntityNotifier.Publisher.Kafka" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Publisher.RabbitMQ" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Publisher.Webhook" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Publisher.AzureEventHubs" Version="1.0.0" />
    
    <!-- Serializers -->
    <PackageReference Include="SqlDbEntityNotifier.Serializers.Json" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Serializers.Protobuf" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Serializers.Avro" Version="1.0.0" />
    
    <!-- Advanced features -->
    <PackageReference Include="SqlDbEntityNotifier.MultiTenant" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Transactional" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Delivery" Version="1.0.0" />
    
    <!-- Monitoring -->
    <PackageReference Include="SqlDbEntityNotifier.Monitoring" Version="1.0.0" />
    <PackageReference Include="SqlDbEntityNotifier.Tracing" Version="1.0.0" />
  </ItemGroup>

</Project>
```

### Method 4: Docker

#### Dockerfile
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["MyApp.csproj", "MyApp/"]
RUN dotnet restore "MyApp/MyApp.csproj"
COPY . .
WORKDIR "/src/MyApp"
RUN dotnet build "MyApp.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MyApp.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

#### Docker Compose
```yaml
version: '3.8'

services:
  app:
    build: .
    ports:
      - "8080:80"
    environment:
      - ConnectionStrings__PostgreSQL=Host=postgres;Database=mydb;Username=user;Password=pass
      - Kafka__BootstrapServers=kafka:9092
    depends_on:
      - postgres
      - kafka

  postgres:
    image: postgres:15
    environment:
      POSTGRES_DB: mydb
      POSTGRES_USER: user
      POSTGRES_PASSWORD: pass
    ports:
      - "5432:5432"
    volumes:
      - postgres_data:/var/lib/postgresql/data

  kafka:
    image: confluentinc/cp-kafka:latest
    environment:
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092
      KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR: 1
    ports:
      - "9092:9092"
    depends_on:
      - zookeeper

  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
      ZOOKEEPER_TICK_TIME: 2000
    ports:
      - "2181:2181"

volumes:
  postgres_data:
```

## Database Setup

### PostgreSQL Setup

#### 1. Install PostgreSQL
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install postgresql postgresql-contrib

# CentOS/RHEL
sudo yum install postgresql-server postgresql-contrib
sudo postgresql-setup initdb
sudo systemctl enable postgresql
sudo systemctl start postgresql

# macOS
brew install postgresql
brew services start postgresql

# Windows
# Download and install from https://www.postgresql.org/download/windows/
```

#### 2. Configure PostgreSQL
```bash
# Edit postgresql.conf
sudo nano /etc/postgresql/15/main/postgresql.conf

# Add or modify these settings:
wal_level = logical
max_wal_senders = 10
max_replication_slots = 10
```

#### 3. Create Database and User
```sql
-- Connect to PostgreSQL
sudo -u postgres psql

-- Create database
CREATE DATABASE mydb;

-- Create user
CREATE USER myuser WITH PASSWORD 'mypassword';

-- Grant privileges
GRANT ALL PRIVILEGES ON DATABASE mydb TO myuser;
GRANT REPLICATION TO myuser;

-- Connect to the database
\c mydb

-- Create publication
CREATE PUBLICATION my_publication FOR ALL TABLES;

-- Create replication slot
SELECT pg_create_logical_replication_slot('my_slot', 'pgoutput');
```

#### 4. Test Connection
```bash
# Test connection
psql -h localhost -U myuser -d mydb -c "SELECT version();"
```

### MySQL Setup

#### 1. Install MySQL
```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install mysql-server

# CentOS/RHEL
sudo yum install mysql-server
sudo systemctl enable mysqld
sudo systemctl start mysqld

# macOS
brew install mysql
brew services start mysql

# Windows
# Download and install from https://dev.mysql.com/downloads/mysql/
```

#### 2. Configure MySQL
```bash
# Edit my.cnf
sudo nano /etc/mysql/mysql.conf.d/mysqld.cnf

# Add or modify these settings:
[mysqld]
log-bin=mysql-bin
binlog-format=ROW
server-id=1
gtid-mode=ON
enforce-gtid-consistency=ON
```

#### 3. Create Database and User
```sql
-- Connect to MySQL
mysql -u root -p

-- Create database
CREATE DATABASE mydb;

-- Create user
CREATE USER 'myuser'@'%' IDENTIFIED BY 'mypassword';

-- Grant privileges
GRANT ALL PRIVILEGES ON mydb.* TO 'myuser'@'%';
GRANT REPLICATION SLAVE ON *.* TO 'myuser'@'%';
FLUSH PRIVILEGES;
```

#### 4. Test Connection
```bash
# Test connection
mysql -h localhost -u myuser -p mydb -e "SELECT VERSION();"
```

### SQLite Setup

#### 1. Install SQLite
```bash
# Ubuntu/Debian
sudo apt-get install sqlite3

# CentOS/RHEL
sudo yum install sqlite

# macOS
brew install sqlite

# Windows
# Download from https://www.sqlite.org/download.html
```

#### 2. Create Database
```bash
# Create database file
sqlite3 mydb.db

-- Enable WAL mode
PRAGMA journal_mode = WAL;

-- Create test table
CREATE TABLE test_table (
    id INTEGER PRIMARY KEY,
    name TEXT,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);

-- Exit
.quit
```

#### 3. Test Connection
```bash
# Test connection
sqlite3 mydb.db "SELECT sqlite_version();"
```

### Oracle Setup

#### 1. Install Oracle Database
```bash
# Download Oracle Database from https://www.oracle.com/database/technologies/oracle-database-software-downloads.html
# Follow the installation guide for your platform
```

#### 2. Configure Oracle
```sql
-- Connect as SYSDBA
sqlplus / as sysdba

-- Enable supplemental logging
ALTER DATABASE ADD SUPPLEMENTAL LOG DATA;

-- Create user
CREATE USER myuser IDENTIFIED BY mypassword;
GRANT CONNECT, RESOURCE TO myuser;
GRANT SELECT ON V_$DATABASE TO myuser;
GRANT SELECT ON V_$LOG TO myuser;
GRANT SELECT ON V_$LOGFILE TO myuser;
GRANT SELECT ON V_$ARCHIVED_LOG TO myuser;
GRANT SELECT ON V_$ARCHIVE_DEST_STATUS TO myuser;

-- Create tablespace
CREATE TABLESPACE mydb_data
DATAFILE '/opt/oracle/oradata/mydb_data.dbf'
SIZE 100M AUTOEXTEND ON;

-- Grant tablespace quota
ALTER USER myuser QUOTA UNLIMITED ON mydb_data;
```

#### 3. Test Connection
```bash
# Test connection
sqlplus myuser/mypassword@localhost:1521/XE
```

## Publisher Setup

### Kafka Setup

#### 1. Install Kafka
```bash
# Download Kafka from https://kafka.apache.org/downloads
wget https://downloads.apache.org/kafka/2.8.1/kafka_2.13-2.8.1.tgz
tar -xzf kafka_2.13-2.8.1.tgz
cd kafka_2.13-2.8.1
```

#### 2. Start Kafka
```bash
# Start Zookeeper
bin/zookeeper-server-start.sh config/zookeeper.properties

# Start Kafka (in another terminal)
bin/kafka-server-start.sh config/server.properties
```

#### 3. Create Topic
```bash
# Create topic
bin/kafka-topics.sh --create --topic my-topic --bootstrap-server localhost:9092 --partitions 3 --replication-factor 1

# List topics
bin/kafka-topics.sh --list --bootstrap-server localhost:9092
```

#### 4. Test Kafka
```bash
# Produce message
echo "Hello, Kafka!" | bin/kafka-console-producer.sh --topic my-topic --bootstrap-server localhost:9092

# Consume message
bin/kafka-console-consumer.sh --topic my-topic --from-beginning --bootstrap-server localhost:9092
```

### RabbitMQ Setup

#### 1. Install RabbitMQ
```bash
# Ubuntu/Debian
sudo apt-get install rabbitmq-server

# CentOS/RHEL
sudo yum install rabbitmq-server
sudo systemctl enable rabbitmq-server
sudo systemctl start rabbitmq-server

# macOS
brew install rabbitmq
brew services start rabbitmq

# Windows
# Download from https://www.rabbitmq.com/download.html
```

#### 2. Configure RabbitMQ
```bash
# Enable management plugin
sudo rabbitmq-plugins enable rabbitmq_management

# Create user
sudo rabbitmqctl add_user myuser mypassword
sudo rabbitmqctl set_user_tags myuser administrator
sudo rabbitmqctl set_permissions -p / myuser ".*" ".*" ".*"
```

#### 3. Create Exchange
```bash
# Create exchange
sudo rabbitmqctl eval 'rabbit_exchange:declare({resource, <<"/">>, exchange, <<"my-exchange">>}, topic, true, false, false, []).'
```

#### 4. Test RabbitMQ
```bash
# Check status
sudo rabbitmqctl status

# Access management UI
# http://localhost:15672 (guest/guest)
```

### Webhook Setup

#### 1. Create Webhook Endpoint
```csharp
[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ReceiveWebhook([FromBody] ChangeEvent changeEvent)
    {
        // Process the change event
        Console.WriteLine($"Received change: {changeEvent.Operation} on {changeEvent.Table}");
        
        return Ok();
    }
}
```

#### 2. Test Webhook
```bash
# Test webhook endpoint
curl -X POST http://localhost:5000/api/webhook \
  -H "Content-Type: application/json" \
  -d '{"source":"test","schema":"public","table":"test_table","operation":"INSERT","offset":"1","data":{},"metadata":{},"timestamp":"2024-01-15T10:00:00Z"}'
```

### Azure Event Hubs Setup

#### 1. Create Event Hubs Namespace
```bash
# Create resource group
az group create --name myResourceGroup --location eastus

# Create Event Hubs namespace
az eventhubs namespace create --resource-group myResourceGroup --name myNamespace --location eastus

# Create Event Hub
az eventhubs eventhub create --resource-group myResourceGroup --namespace-name myNamespace --name myHub
```

#### 2. Get Connection String
```bash
# Get connection string
az eventhubs namespace authorization-rule keys list --resource-group myResourceGroup --namespace-name myNamespace --name RootManageSharedAccessKey
```

#### 3. Test Event Hubs
```bash
# Send test event
az eventhubs eventhub send --resource-group myResourceGroup --namespace-name myNamespace --name myHub --message "Hello, Event Hubs!"
```

## Configuration

### appsettings.json
```json
{
  "ConnectionStrings": {
    "PostgreSQL": "Host=localhost;Database=mydb;Username=myuser;Password=mypassword;Port=5432;",
    "MySQL": "Server=localhost;Database=mydb;Uid=myuser;Pwd=mypassword;Port=3306;",
    "SQLite": "Data Source=mydb.db;",
    "Oracle": "Data Source=localhost:1521/XE;User Id=myuser;Password=mypassword;"
  },
  "PostgresAdapter": {
    "ConnectionString": "Host=localhost;Database=mydb;Username=myuser;Password=mypassword;Port=5432;",
    "SlotName": "my_slot",
    "PublicationName": "my_publication",
    "HeartbeatInterval": "00:00:30",
    "MaxReplicationLag": "00:05:00"
  },
  "MySQLAdapter": {
    "ConnectionString": "Server=localhost;Database=mydb;Uid=myuser;Pwd=mypassword;Port=3306;",
    "ServerId": 1,
    "HeartbeatInterval": "00:00:30",
    "MaxReplicationLag": "00:05:00"
  },
  "SQLiteAdapter": {
    "ConnectionString": "Data Source=mydb.db;",
    "EnableWAL": true,
    "HeartbeatInterval": "00:00:30"
  },
  "OracleAdapter": {
    "ConnectionString": "Data Source=localhost:1521/XE;User Id=myuser;Password=mypassword;",
    "HeartbeatInterval": "00:00:30",
    "MaxReplicationLag": "00:05:00"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "Topic": "my-topic",
    "Acks": "all",
    "RetryBackoffMs": 100,
    "MaxRetries": 3
  },
  "RabbitMQ": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "myuser",
    "Password": "mypassword",
    "Exchange": "my-exchange",
    "RoutingKey": "my-routing-key"
  },
  "Webhook": {
    "BaseUrl": "http://localhost:5000",
    "Endpoint": "/api/webhook",
    "Timeout": "00:00:30",
    "RetryCount": 3,
    "RetryDelay": "00:00:01"
  },
  "AzureEventHubs": {
    "ConnectionString": "Endpoint=sb://myNamespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=myKey",
    "EventHubName": "myHub",
    "BatchSize": 100,
    "FlushInterval": "00:00:01"
  },
  "JsonSerializer": {
    "PropertyNamingPolicy": "CamelCase",
    "WriteIndented": false
  },
  "ProtobufSerializer": {
    "UseCompression": true,
    "CompressionLevel": "Optimal"
  },
  "AvroSerializer": {
    "SchemaRegistryUrl": "http://localhost:8081",
    "UseCompression": true,
    "CompressionLevel": "Optimal"
  },
  "MultiTenant": {
    "EnableMultiTenancy": true,
    "DefaultTenantId": "default"
  },
  "Throttling": {
    "EnableThrottling": true,
    "DefaultRateLimit": 1000,
    "DefaultBurstLimit": 2000
  },
  "Transactional": {
    "MaxTransactionSize": 10000,
    "TransactionTimeout": "00:05:00",
    "CleanupInterval": "00:01:00"
  },
  "Delivery": {
    "EnableExactlyOnce": true,
    "DeliveryTimeout": "00:05:00",
    "CleanupInterval": "00:01:00"
  },
  "BulkOperations": {
    "BatchSize": 1000,
    "BatchTimeout": "00:00:05",
    "MinBatchSize": 10
  },
  "Monitoring": {
    "EnableMetrics": true,
    "MetricsInterval": "00:00:10"
  },
  "HealthChecks": {
    "EnableHealthChecks": true,
    "HealthCheckInterval": "00:00:30"
  },
  "Tracing": {
    "EnableTracing": true,
    "TraceLevel": "Information"
  }
}
```

### Program.cs
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SqlDbEntityNotifier.Core.Interfaces;
using SqlDbEntityNotifier.Adapters.Postgres;
using SqlDbEntityNotifier.Publisher.Kafka;
using SqlDbEntityNotifier.Serializers.Json;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                
                // Database adapter
                services.AddPostgresAdapter(options =>
                {
                    options.ConnectionString = configuration.GetConnectionString("PostgreSQL");
                    options.SlotName = configuration["PostgresAdapter:SlotName"];
                    options.PublicationName = configuration["PostgresAdapter:PublicationName"];
                    options.HeartbeatInterval = TimeSpan.Parse(configuration["PostgresAdapter:HeartbeatInterval"]);
                    options.MaxReplicationLag = TimeSpan.Parse(configuration["PostgresAdapter:MaxReplicationLag"]);
                });

                // Publisher
                services.AddKafkaPublisher(options =>
                {
                    options.BootstrapServers = configuration["Kafka:BootstrapServers"];
                    options.Topic = configuration["Kafka:Topic"];
                    options.Acks = configuration["Kafka:Acks"];
                    options.RetryBackoffMs = int.Parse(configuration["Kafka:RetryBackoffMs"]);
                    options.MaxRetries = int.Parse(configuration["Kafka:MaxRetries"]);
                });

                // Serializer
                services.AddJsonSerializer(options =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.WriteIndented = false;
                });

                // Register your service
                services.AddHostedService<ChangeEventProcessor>();
            });
    }
}
```

## Verification

### 1. Test Database Connection
```csharp
[Test]
public async Task TestDatabaseConnection()
{
    var connectionString = "Host=localhost;Database=mydb;Username=myuser;Password=mypassword;";
    using var connection = new NpgsqlConnection(connectionString);
    await connection.OpenAsync();
    
    using var command = new NpgsqlCommand("SELECT version();", connection);
    var version = await command.ExecuteScalarAsync();
    
    Assert.IsNotNull(version);
    Console.WriteLine($"PostgreSQL version: {version}");
}
```

### 2. Test Publisher Connection
```csharp
[Test]
public async Task TestKafkaConnection()
{
    var config = new ProducerConfig
    {
        BootstrapServers = "localhost:9092"
    };
    
    using var producer = new ProducerBuilder<string, string>(config).Build();
    
    var message = new Message<string, string>
    {
        Key = "test-key",
        Value = "test-message"
    };
    
    var result = await producer.ProduceAsync("test-topic", message);
    
    Assert.IsTrue(result.Status == PersistenceStatus.Persisted);
    Console.WriteLine($"Message sent to partition {result.Partition} at offset {result.Offset}");
}
```

### 3. Test End-to-End Flow
```csharp
[Test]
public async Task TestEndToEndFlow()
{
    var host = CreateHost();
    var notifier = host.Services.GetRequiredService<IEntityNotifier>();
    var publisher = host.Services.GetRequiredService<IChangePublisher>();
    
    var receivedEvents = new List<ChangeEvent>();
    
    await notifier.StartAsync(async (changeEvent, cancellationToken) =>
    {
        receivedEvents.Add(changeEvent);
        await publisher.PublishAsync(changeEvent);
    }, CancellationToken.None);
    
    // Wait for events
    await Task.Delay(TimeSpan.FromSeconds(10));
    
    Assert.IsTrue(receivedEvents.Count > 0);
    Console.WriteLine($"Received {receivedEvents.Count} events");
}
```

## Troubleshooting

### Common Installation Issues

#### Issue 1: Package Not Found
**Problem**: NuGet package not found during installation.

**Solutions**:
1. Check package name spelling
2. Verify package version exists
3. Clear NuGet cache: `dotnet nuget locals all --clear`
4. Restore packages: `dotnet restore`

#### Issue 2: Version Conflicts
**Problem**: Package version conflicts with existing dependencies.

**Solutions**:
1. Update all packages to latest versions
2. Use package resolution: `dotnet add package PackageName --version 1.0.0`
3. Check compatibility matrix
4. Remove conflicting packages

#### Issue 3: Database Connection Issues
**Problem**: Cannot connect to database after installation.

**Solutions**:
1. Verify database is running
2. Check connection string format
3. Test connection manually
4. Check firewall settings
5. Verify user permissions

#### Issue 4: Publisher Connection Issues
**Problem**: Cannot connect to publisher (Kafka, RabbitMQ, etc.).

**Solutions**:
1. Verify publisher is running
2. Check configuration settings
3. Test connection manually
4. Check network connectivity
5. Verify authentication credentials

### Performance Issues

#### Issue 1: High Memory Usage
**Solutions**:
1. Reduce batch sizes
2. Enable compression
3. Increase cleanup frequency
4. Monitor memory usage

#### Issue 2: Slow Processing
**Solutions**:
1. Increase connection pool size
2. Use async operations
3. Implement batching
4. Optimize serialization

#### Issue 3: High CPU Usage
**Solutions**:
1. Reduce polling frequency
2. Use efficient serialization
3. Limit concurrent operations
4. Optimize algorithms

### Configuration Issues

#### Issue 1: Configuration Not Loading
**Solutions**:
1. Check configuration file format
2. Verify section names
3. Use configuration binding
4. Check environment variables

#### Issue 2: Environment Variables Not Working
**Solutions**:
1. Check variable naming convention
2. Verify variable values
3. Check environment loading
4. Use configuration providers

### Monitoring Issues

#### Issue 1: No Logs Generated
**Solutions**:
1. Configure logging providers
2. Set log levels
3. Check log output
4. Use structured logging

#### Issue 2: Metrics Not Available
**Solutions**:
1. Enable metrics collection
2. Configure metrics endpoints
3. Check metrics configuration
4. Verify metrics providers

#### Issue 3: Health Checks Failing
**Solutions**:
1. Enable health checks
2. Configure health check endpoints
3. Check health check logic
4. Verify dependencies

This installation guide provides comprehensive instructions for setting up SQLDBEntityNotifier in various environments. Follow the steps carefully and refer to the troubleshooting section if you encounter any issues.