# EDCL IDCS Seeder

IDCS Seeder is a .NET Console Application simulation project designed to mimic the behavior of the legacy IDCS system by performing direct Data Manipulation (Insertions) into the IDCS database tables.

This project is crucial for **End-to-End (E2E) Testing** of the **Change Data Capture (CDC)** mechanism using **Debezium**, which connects to **RabbitMQ** (via MassTransit) and is consumed by the backend `EDCL.Worker.Ingestion` service.

## 🎯 Key Capabilities

- **Initialize IDCS Database:** Automatically create the database, tables, and enable SQL Server Change Data Capture (CDC) at both the database and table levels.
- **Simulate Real-time Inserts:** Insert random, realistic data (Manifests, Parts, Kanbans, Skids) mirroring the legacy IDCS data structure.
- **Bulk Seeding:** Generate large volumes of data (e.g., 200 Manifests) for load testing and CDC performance evaluation.
- **Edge-Case Simulation:**
  - **Out of Order:** Simulates inserting child records (parts/kanbans) before their parent manifest, triggering EDCL's retry mechanisms and SSE fault events.
  - **Race Conditions:** Simulates high-concurrency race conditions where related inserts are delayed.
- **Master Data Seeding:** Automatically injects master data directly into the EDCL database.

## 🛠 Prerequisites

This project comes with its own isolated SQL Server container for IDCS simulation (`docker-compose-idcs.yml`).
1. Make sure you run `make idcs-up` in this directory to start the IDCS SQL Server on port 1466.
2. The EDCL core infrastructure must also be running.

## 🚀 Usage (Local Makefile)

The `idcs-seeder` directory contains a dedicated `Makefile` to quickly run commands. From within the `idcs-seeder` directory, you can run:

### Infrastructure Commands
| Command | Description |
|---|---|
| `make idcs-up` | Starts the IDCS SQL Server container using `docker-compose-idcs.yml`. |
| `make idcs-down` | Stops and removes the IDCS SQL Server container. |

### Initialization & Reset
| Command | Description |
|---|---|
| `make seed-init` | Initializes the IDCS database, creates all necessary tables, and enables CDC via `sys.sp_cdc_enable_db`. **Must be run first.** |
| `make seed-reset` | Truncates and resets all Manifest transactional data in **both** IDCS and EDCL databases. Use this to clear data safely while maintaining referential integrity. |

### Master Data Seeding
| Command | Description |
|---|---|
| `make seed-edcl-master` | Seeds core EDCL master data, specifically Transporters (`Hikari Logistics`), Trucks (`B 9607 PXT`), and basic Driver users with hashed PINs. |
| `make seed-supplier` | Inserts mock Supplier data into both EDCL and IDCS databases. |

### Transactional Data Seeding
| Command | Description |
|---|---|
| `make seed-bulk` | Performs a heavy insert of 200 Manifests with realistic Parts, Kanbans, and Skids to test CDC backpressure and load test the Ingestion Worker. |
| `make seed-manifest` | Inserts 1 random Manifest record into the IDCS database. |
| `make seed-part` | Inserts 1 random Part attached to the latest Manifest. |
| `make seed-kanban` | Inserts 1 Kanban code attached to the latest Part. |
| `make seed-skid` | Inserts 1 Skid code attached to the latest Manifest. |

### Edge Case Simulations
| Command | Description |
|---|---|
| `make seed-out-of-order` | Inserts a Part with a non-existent `ManifestId`. Used to test Dead Letter Queues (DLQ) and Retry mechanisms in the Ingestion Worker. |
| `make seed-race-condition`| Deliberately delays the insertion of a Manifest *after* its Parts have been inserted to observe handling of Race Conditions by the CDC Worker. |
| `make seed-k6-clean` | Cleans up the environment specifically for K6 performance testing. |

## 🏗 How it Works (Under the Hood)

1. The seeder connects directly to the SQL Server instances using `Dapper` (`1466` for IDCS, `1444` for EDCL).
2. When performing IDCS inserts (e.g., `make seed-bulk`), the Debezium Server container listens to the transaction log of the `IDCS` database.
3. Debezium detects the new rows and publishes JSON payloads to RabbitMQ.
4. The `EDCL.Worker.Ingestion` service consumes these messages from RabbitMQ and transforms them into EDCL's normalized tables (`edcl.ingestion.manifests`, etc).
