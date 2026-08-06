.PHONY: fe-install fe-start fe-test fe-e2e fe-e2e-open be-up be-down be-run be-stop be-test help

help:
	@echo "EDCL Mini - Unified Makefile"
	@echo ""
	@echo "Frontend Commands:"
	@echo "  make fe-install    - Install dependencies for fe/web"
	@echo "  make fe-start      - Start Angular dev server (http://localhost:4200)"
	@echo "  make fe-test       - Run frontend unit tests (Vitest)"
	@echo "  make fe-e2e        - Run Cypress E2E tests headless"
	@echo "  make fe-e2e-open   - Open Cypress UI for E2E tests"
	@echo ""
	@echo "Backend Commands:"
	@echo "  make be-up         - Start backend infrastructure (Docker Compose) with full API"
	@echo "  make be-down       - Stop backend infrastructure"
	@echo "  make be-infra-up   - Start only infrastructure (SQL Server, Redis, RabbitMQ, Debezium)"
	@echo "  make be-infra-down - Stop only infrastructure"
	@echo "  make be-run-all    - Run API, Gateway, and Workers locally (.NET run)"
	@echo "  make be-stop-all   - Stop all local backend services"
	@echo "  make be-test       - Run backend unit and integration tests"
	@echo ""
	@echo "Seeder Commands:"
	@echo "  make seed-logistic-partner       - Insert logistic partner data"
	@echo "  make seed-reset-logistic-partner - Reset/delete all logistic partner data"
	@echo "  make seed-route                  - Insert route data"
	@echo "  make seed-reset-route            - Reset/delete all route data"
	@echo "  make seed-driver                 - Insert driver data"
	@echo "  make seed-reset-driver           - Reset/delete all driver data"
	@echo "  make seed-init                   - Initialize IDCS DB & CDC"
	@echo "  make seed-manifest               - Insert 1 manifest to IDCS"
	@echo "  make seed-part                   - Insert 1 part to IDCS"
	@echo "  make seed-bulk                   - Insert 200 manifests to IDCS"
	@echo "  make seed-out-of-order           - Simulate out-of-order scenario"
	@echo "  make seed-race-condition         - Simulate race condition scenario"
	@echo "  make seed-edcl-master            - Seed EDCL core master data"

# --- Seeder Commands ---
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

seed-all:
	dotnet run --project idcs-seeder -- all

seed-reset-all:
	dotnet run --project idcs-seeder -- reset-all

seed-init:
	dotnet run --project idcs-seeder -- init

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

seed-transaction:
	dotnet run --project idcs-seeder -- transaction

seed-race-condition:
	dotnet run --project idcs-seeder -- race-condition

seed-edcl-master:
	dotnet run --project idcs-seeder -- edcl-master

seed-reset-idcs:
	dotnet run --project idcs-seeder -- reset


# --- Frontend Commands ---
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

# --- Backend Commands (Delegating to be/Makefile) ---
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
