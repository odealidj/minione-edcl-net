.PHONY: help be-up be-down infra-up infra-down idcs-up idcs-down seed-init seed-master-one reset-master-one fe-start gps-run gps-build

GPS_PROJECT ?= ../EDCLGPSAPI/src/Bootstrapper/Api/Api.csproj

help:
	@echo "================================================================="
	@echo "🚚 EDCL Mini - Developer CLI"
	@echo "================================================================="
	@echo "  make be-up            - Start all backend containers (Database, Redis, RabbitMQ, Gateway, API, Workers)"
	@echo "  make be-down          - Stop and remove all backend containers"
	@echo "  make infra-up         - Start shared infra only (SQL Server, Redis, RabbitMQ, Postgres, Seq)"
	@echo "  make infra-down       - Stop shared infra containers"
	@echo "  make gps-run          - Run EDCLGPSAPI service locally via dotnet run (port 5090)"
	@echo "  make gps-build        - Build EDCLGPSAPI service project"
	@echo "  make idcs-up          - Start isolated IDCS SQL Server container (port 1466)"
	@echo "  make idcs-down        - Stop and remove IDCS SQL Server container"
	@echo "  make seed-init        - Initialize IDCS DB schema & CDC snapshot"
	@echo "  make seed-master-one  - Seed full master set + 1 manifest + PO for E2E demo"
	@echo "  make reset-master-one - Reset simulation and master data state"
	@echo "  make fe-start         - Start Angular development server (http://localhost:4200)"
	@echo "================================================================="

# =============================================================================
# Backend Containers (.NET 10 & Docker)
# =============================================================================
be-up:
	cd be && $(MAKE) up

be-down:
	cd be && $(MAKE) down

infra-up:
	cd be && $(MAKE) infra-up

infra-down:
	cd be && $(MAKE) infra-down


# =============================================================================
# IDCS Simulation Container (Port 1466)
# =============================================================================
idcs-up:
	cd idcs-seeder && $(MAKE) idcs-up

idcs-down:
	cd idcs-seeder && $(MAKE) idcs-down

# =============================================================================
# Simulation & Seeder Commands
# =============================================================================
seed-init:
	dotnet run --project idcs-seeder -- init

seed-master-one:
	dotnet run --project idcs-seeder -- one-master

reset-master-one:
	dotnet run --project idcs-seeder -- reset-master-one

# =============================================================================
# Frontend (Angular 19)
# =============================================================================
fe-start:
	cd fe/web && pnpm start

# =============================================================================
# EDCLGPSAPI Service (.NET 10 / FastEndpoints / Marten)
# =============================================================================
gps-build:
	dotnet build $(GPS_PROJECT)

gps-run:
	dotnet run --project $(GPS_PROJECT)

