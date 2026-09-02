.PHONY: help idcs-up idcs-down seed-init seed-master-one reset-master-one

help:
	@echo "================================================================="
	@echo "🏭 IDCS Simulation & Seeder CLI"
	@echo "================================================================="
	@echo "  make idcs-up          - Start isolated IDCS SQL Server container (port 1466)"
	@echo "  make idcs-down        - Stop and remove IDCS SQL Server container"
	@echo "  make seed-init        - Initialize IDCS DB schema & CDC snapshot"
	@echo "  make seed-master-one  - Seed full master set + 1 manifest + PO for E2E demo"
	@echo "  make reset-master-one - Reset simulation and master data state"
	@echo "================================================================="

DOCKER_COMPOSE ?= $(shell if command -v docker-compose >/dev/null 2>&1; then echo "docker-compose"; elif command -v docker >/dev/null 2>&1; then echo "docker compose"; elif command -v podman-compose >/dev/null 2>&1; then echo "podman-compose"; else echo "podman compose"; fi)

# =============================================================================
# IDCS Simulation Container (Port 1466)
# =============================================================================
idcs-up:
	$(DOCKER_COMPOSE) -f docker-compose-idcs.yml up -d

idcs-down:
	$(DOCKER_COMPOSE) -f docker-compose-idcs.yml down

# =============================================================================
# Simulation & Seeder Commands
# =============================================================================
seed-init:
	dotnet run -- init

seed-master-one:
	dotnet run -- one-master

reset-master-one:
	dotnet run -- reset-master-one
