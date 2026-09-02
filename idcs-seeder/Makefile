.PHONY: idcs-up idcs-down seed-init seed-master seed-master-one seed-one-master reset-master reset-master-one \
        seed-transaction seed-out-of-order seed-race-condition seed-bulk reset-transaction reset-pickup trigger-reject \
        seed-manifest seed-part seed-kanban seed-skid seed-supplier reset-supplier \
        seed-logistic-partner reset-logistic-partner seed-route reset-route seed-driver reset-driver seed-truck reset-truck seed-k6-clean

idcs-up:
	@if command -v docker-compose >/dev/null 2>&1; then docker-compose -f docker-compose-idcs.yml up -d; \
	elif command -v docker >/dev/null 2>&1; then docker compose -f docker-compose-idcs.yml up -d; \
	elif command -v podman-compose >/dev/null 2>&1; then podman-compose -f docker-compose-idcs.yml up -d; \
	else podman compose -f docker-compose-idcs.yml up -d; fi

idcs-down:
	@if command -v docker-compose >/dev/null 2>&1; then docker-compose -f docker-compose-idcs.yml down; \
	elif command -v docker >/dev/null 2>&1; then docker compose -f docker-compose-idcs.yml down; \
	elif command -v podman-compose >/dev/null 2>&1; then podman-compose -f docker-compose-idcs.yml down; \
	else podman compose -f docker-compose-idcs.yml down; fi

seed-init:
	dotnet run init

# Master Data Seeding
seed-master:
	dotnet run all

seed-master-one:
	dotnet run one-master

seed-one-master:
	dotnet run one-master

reset-master:
	dotnet run reset-all

reset-master-one:
	dotnet run reset-master-one

# Granular Master Data Seeders
seed-supplier:
	dotnet run supplier

reset-supplier:
	dotnet run reset-supplier

seed-logistic-partner:
	dotnet run logistic-partner

reset-logistic-partner:
	dotnet run reset-logistic-partner

seed-route:
	dotnet run route

reset-route:
	dotnet run reset-route

seed-driver:
	dotnet run driver

reset-driver:
	dotnet run reset-driver

seed-truck:
	dotnet run truck

reset-truck:
	dotnet run reset-truck

# Transactional Data Seeding
seed-transaction:
	dotnet run transaction

seed-manifest:
	dotnet run manifest

seed-part:
	dotnet run part

seed-kanban:
	dotnet run kanban

seed-skid:
	dotnet run skid

seed-bulk:
	dotnet run bulk

reset-transaction:
	dotnet run reset

reset-pickup:
	dotnet run reset-pickup

# Extreme Cases & Testing
seed-out-of-order:
	dotnet run out-of-order

seed-race-condition:
	dotnet run race-condition

trigger-reject:
	dotnet run trigger-reject

seed-k6-clean:
	cd ../be/tests/k6/EDCL.K6Seeder && dotnet run clean
