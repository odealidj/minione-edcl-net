.PHONY: help \
        fe-install fe-start fe-test fe-e2e fe-e2e-open \
        be-up be-down be-infra-up be-infra-down be-run-all be-stop-all be-test be-load-test be-metrics \
        seed-master-one reset-master-one seed-all seed-reset-all seed-init seed-transaction \
        seed-logistic-partner seed-reset-logistic-partner seed-route seed-reset-route \
        seed-driver seed-reset-driver seed-truck seed-reset-truck seed-supplier seed-reset-supplier \
        seed-manifest seed-part seed-kanban seed-skid seed-bulk seed-out-of-order seed-race-condition \
        seed-reset-idcs seed-trigger-reject

help:
	@echo "================================================================="
	@echo "🚚 EDCL Mini - Unified Developer CLI"
	@echo "================================================================="
	@echo ""
	@echo "Frontend Commands (Angular 19):"
	@echo "  make fe-install      - Install dependencies for fe/web via pnpm"
	@echo "  make fe-start        - Start Angular dev server (http://localhost:4200)"
	@echo "  make fe-test         - Run frontend unit tests (Vitest)"
	@echo "  make fe-e2e          - Run Cypress E2E tests headless"
	@echo "  make fe-e2e-open     - Open Cypress UI for interactive E2E testing"
	@echo ""
	@echo "Backend Commands (.NET 10 & Docker):"
	@echo "  make be-infra-up     - Start Infra (SQL Server, Redis, RabbitMQ, Debezium, Jaeger, Prometheus)"
	@echo "  make be-infra-down   - Stop only infrastructure containers"
	@echo "  make be-up           - Start backend infrastructure with containerized API & Workers"
	@echo "  make be-down         - Stop all backend infrastructure and containers"
	@echo "  make be-run-all      - Run API Host, Gateway, and all Workers locally (.NET run)"
	@echo "  make be-stop-all     - Stop all local background backend services"
	@echo "  make be-test         - Run backend unit and integration test suites"
	@echo "  make be-load-test    - Execute Grafana k6 End-to-End driver journey load test"
	@echo "  make be-metrics      - Inspect OpenTelemetry Prometheus metrics endpoint"
	@echo ""
	@echo "Simulation & Seeder Commands (IDCS & Master Data):"
	@echo "  make seed-master-one - [RECOMMENDED] Seed full master set + 1 manifest + PO for E2E demo"
	@echo "  make reset-master-one- [RECOMMENDED] Safe 4-step reset for entire simulation state"
	@echo "  make seed-all        - Seed all master data (Partners, GPS, Suppliers, Routes, Drivers, Trucks)"
	@echo "  make seed-reset-all  - Reset/delete all master data"
	@echo "  make seed-init       - Initialize IDCS DB schema & CDC snapshot"
	@echo "  make seed-manifest   - Insert 1 manifest to IDCS"
	@echo "  make seed-part       - Insert 1 part to IDCS"
	@echo "  make seed-kanban     - Insert 1 kanban to IDCS"
	@echo "  make seed-skid       - Insert 1 skid to IDCS"
	@echo "  make seed-bulk       - Insert 200 manifests to IDCS (bulk simulation)"
	@echo "  make seed-out-of-order   - Simulate out-of-order CDC ingestion scenario"
	@echo "  make seed-race-condition - Simulate race condition scenario"
	@echo "  make seed-transaction    - Seed transactional manifest data"
	@echo "  make seed-trigger-reject - Simulate manifest rejection scenario"
	@echo "  make seed-reset-idcs     - Reset IDCS manifest/transaction data"
	@echo ""
	@echo "Master Entity Specific Seeders:"
	@echo "  make seed-logistic-partner       / make seed-reset-logistic-partner"
	@echo "  make seed-route                  / make seed-reset-route"
	@echo "  make seed-driver                 / make seed-reset-driver"
	@echo "  make seed-truck                  / make seed-reset-truck"
	@echo "  make seed-supplier               / make seed-reset-supplier"
	@echo "================================================================="

# =============================================================================
# Frontend Commands
# =============================================================================
fe-install:
	cd fe/web && pnpm install

fe-start:
	cd fe/web && pnpm start

fe-test:
	cd fe/web && pnpm test

fe-e2e:
	cd fe/web && pnpm run e2e

fe-e2e-open:
	cd fe/web && pnpm run e2e:open

# =============================================================================
# Backend Commands (Delegating to be/Makefile & .NET)
# =============================================================================
be-up:
	cd be && $(MAKE) up

be-down:
	cd be && $(MAKE) down

be-infra-up:
	cd be && $(MAKE) infra-up

be-infra-down:
	cd be && $(MAKE) infra-down

be-run-all:
	cd be && $(MAKE) run-all

be-stop-all:
	cd be && $(MAKE) stop-all

be-test:
	cd be && dotnet test

be-load-test:
	cd be/tests/k6 && k6 run driver_journey.js

be-metrics:
	@curl -s http://localhost:5140/metrics | head -40 || echo "API is not running. Start with 'make be-run-all'."

# =============================================================================
# Seeder & Simulation Commands
# =============================================================================
seed-master-one:
	dotnet run --project idcs-seeder -- one-master

reset-master-one:
	dotnet run --project idcs-seeder -- reset-master-one

seed-all:
	dotnet run --project idcs-seeder -- all

seed-reset-all:
	dotnet run --project idcs-seeder -- reset-all

seed-init:
	dotnet run --project idcs-seeder -- init

seed-transaction:
	dotnet run --project idcs-seeder -- transaction

seed-manifest:
	dotnet run --project idcs-seeder -- manifest

seed-part:
	dotnet run --project idcs-seeder -- part

seed-kanban:
	dotnet run --project idcs-seeder -- kanban

seed-skid:
	dotnet run --project idcs-seeder -- skid

seed-bulk:
	dotnet run --project idcs-seeder -- bulk

seed-out-of-order:
	dotnet run --project idcs-seeder -- out-of-order

seed-race-condition:
	dotnet run --project idcs-seeder -- race-condition

seed-trigger-reject:
	dotnet run --project idcs-seeder -- trigger-reject

seed-reset-idcs:
	dotnet run --project idcs-seeder -- reset

# --- Master Entity Seeders ---
seed-logistic-partner:
	dotnet run --project idcs-seeder -- logistic-partner

seed-reset-logistic-partner:
	dotnet run --project idcs-seeder -- reset-logistic-partner

seed-route:
	dotnet run --project idcs-seeder -- route

seed-reset-route:
	dotnet run --project idcs-seeder -- reset-route

seed-driver:
	dotnet run --project idcs-seeder -- driver

seed-reset-driver:
	dotnet run --project idcs-seeder -- reset-driver

seed-truck:
	dotnet run --project idcs-seeder -- truck

seed-reset-truck:
	dotnet run --project idcs-seeder -- reset-truck

seed-supplier:
	dotnet run --project idcs-seeder -- supplier

seed-reset-supplier:
	dotnet run --project idcs-seeder -- reset-supplier
