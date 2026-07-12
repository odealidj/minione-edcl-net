.PHONY: up down infra-up infra-down logs run-EDCL.Api stop-EDCL.Api run-EDCL.Gateway stop-EDCL.Gateway run-workers stop-workers run-all stop-all

# Menjalankan seluruh sistem (Infrastructure + .NET API & Gateway & Workers)
up:
	docker-compose up -d --build

# Mematikan seluruh sistem
down:
	docker-compose down

# Hanya menjalankan infrastruktur utama (SQL Server, Redis, RabbitMQ, Debezium)
infra-up:
	docker-compose up -d sqlserver redis rabbitmq debezium-server

# Mematikan hanya infrastruktur utama
infra-down:
	docker-compose stop sqlserver redis rabbitmq debezium-server

# Melihat log dari semua service
logs:
	docker-compose logs -f

# ─── LOCAL DEVELOPMENT (NATIVE .NET) ──────────────────────────────────────────

# Run EDCL.Api secara lokal (background)
run-EDCL.Api:
	@echo "Starting EDCL.Api..."
	@cd src/Host/EDCL.Api && dotnet run --no-build > ../../../edcl_api.log 2>&1 & echo $$! > ../../../edcl_api.pid
	@echo "EDCL.Api is running in background. Logs: edcl_api.log"

# Stop EDCL.Api yang berjalan di lokal
stop-EDCL.Api:
	@echo "Stopping EDCL.Api..."
	@if [ -f edcl_api.pid ]; then kill `cat edcl_api.pid` 2>/dev/null || true; rm edcl_api.pid; fi
	@fuser -k 5140/tcp 2>/dev/null || true
	@pkill -f "EDCL.Api.dll" 2>/dev/null || true
	@echo "EDCL.Api stopped."

# Run EDCL.Gateway secara lokal (background)
run-EDCL.Gateway:
	@echo "Starting EDCL.Gateway..."
	@cd src/Gateway/EDCL.Gateway && dotnet run --no-build > ../../../edcl_gateway.log 2>&1 & echo $$! > ../../../edcl_gateway.pid
	@echo "EDCL.Gateway is running in background. Logs: edcl_gateway.log"

# Stop EDCL.Gateway yang berjalan di lokal
stop-EDCL.Gateway:
	@echo "Stopping EDCL.Gateway..."
	@if [ -f edcl_gateway.pid ]; then kill `cat edcl_gateway.pid` 2>/dev/null || true; rm edcl_gateway.pid; fi
	@fuser -k 5293/tcp 2>/dev/null || true
	@pkill -f "EDCL.Gateway.dll" 2>/dev/null || true
	@echo "EDCL.Gateway stopped."

# Run semua Worker secara lokal (background)
run-workers:
	@echo "Starting EDCL.Worker.Ingestion..."
	@cd workers/EDCL.Worker.Ingestion && dotnet run --no-build > ../../edcl_worker_ingestion.log 2>&1 & echo $$! > ../../edcl_worker_ingestion.pid
	@echo "Starting EDCL.Worker.Outbox..."
	@cd workers/EDCL.Worker.Outbox && dotnet run --no-build > ../../edcl_worker_outbox.log 2>&1 & echo $$! > ../../edcl_worker_outbox.pid
	@echo "Starting EDCL.Worker.Reporter..."
	@cd workers/EDCL.Worker.Reporter && dotnet run --no-build > ../../edcl_worker_reporter.log 2>&1 & echo $$! > ../../edcl_worker_reporter.pid
	@echo "All Workers are running in background. Logs: edcl_worker_*.log"

# Stop semua Worker yang berjalan di lokal
stop-workers:
	@echo "Stopping Workers..."
	@if [ -f edcl_worker_ingestion.pid ]; then kill `cat edcl_worker_ingestion.pid` 2>/dev/null || true; rm edcl_worker_ingestion.pid; fi
	@if [ -f edcl_worker_outbox.pid ]; then kill `cat edcl_worker_outbox.pid` 2>/dev/null || true; rm edcl_worker_outbox.pid; fi
	@if [ -f edcl_worker_reporter.pid ]; then kill `cat edcl_worker_reporter.pid` 2>/dev/null || true; rm edcl_worker_reporter.pid; fi
	@pkill -f "EDCL.Worker." 2>/dev/null || true
	@echo "All Workers stopped."

# Build seluruh solution terlebih dahulu agar tidak terjadi race condition file sharing (.pdb / .dll)
build-all:
	@echo "Building entire backend solution..."
	@dotnet build EDCL.sln
	@echo "Build complete."

# Run API, Gateway, dan Workers secara lokal (background)
run-all: build-all run-EDCL.Api run-EDCL.Gateway run-workers

# Stop API, Gateway, dan Workers lokal
stop-all: stop-EDCL.Api stop-EDCL.Gateway stop-workers
