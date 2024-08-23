# Apache Pulsar Producer and Consumer Projects

## Overview
This project demonstrates an implementation of Apache Pulsar Producer and Consumer with DotNet

## Prerequisites
- Docker Compose
- [Pulsar binaries](https://pulsar.apache.org/download/)

## Setup

### Run Pulsar with Docker Compose

Open terminal window in the repository root folder and run 
```
docker-compose up
```

## Configuration

### Pulsar Configuration
Ensure Pulsar is running and accessible
```
bin/pulsar-admin --admin-url http://localhost:8080 topics list public/default
```

### Build the project
```
dotnet build
```

## Running the Producer
To start publishing `Person` with IDs from 0 to X to the `persistent://public/default/persons` topic in the loop with 1 seconds delay between each message run:
```
dotnet run --project .\Pulsar.Producer\Pulsar.Producer.csproj <X>
```

## Running the Consumer
To start consuming `Person` messages with consumer named `consumer-X` run:
```
dotnet run --project .\Pulsar.Consumer\Pulsar.Consumer.csproj <X>
```

e.g. the following command will start consumer named `consumer-0`
```
dotnet run --project .\Pulsar.Consumer\Pulsar.Consumer.csproj 0
```

## Problem
Using shared key subscription means the message with the same key will be consumed by the same consumer.
With one producer sending messages with keys from `0-0` to `0-9` and two consumers messages consumed in random order, e.g. message with the same key can be consumed by both consumers