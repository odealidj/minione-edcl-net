.PHONY: up down infra-up infra-down logs run-EDCL.Api stop-EDCL.Api run-EDCL.Gateway stop-EDCL.Gateway run-workers stop-workers run-all stop-all

# Menjalankan seluruh sistem (Infrastructure + .NET API & Gateway & Workers)
up:
	docker-compose up -d --build

# Mematikan seluruh sistem
down:
	docker-compose down

# Hanya menjalankan infrastruktur utama (SQL Server, Redis, RabbitMQ, Debezium, Jaeger, Prometheus)
infra-up:
	docker-compose up -d sqlserver redis rabbitmq debezium-server jaeger prometheus
	@echo "Waiting for infrastructure to become healthy..."
	@while docker-compose ps | grep -qi "starting"; do \
		sleep 2; \
	done
	@if docker-compose ps | grep -qi "exited"; then \
		echo "ERROR: One or more containers failed to start or crashed."; \
		exit 1; \
	fi
	@echo "Infrastructure is running and healthy."

# Mematikan hanya infrastruktur utama
infra-down:
	docker-compose stop sqlserver redis rabbitmq debezium-server jaeger prometheus

# Melihat log dari semua service
logs:
	docker-compose logs -f

# ─── LOCAL DEVELOPMENT (NATIVE .NET) ──────────────────────────────────────────

# Run EDCL.Api secara lokal (background)
run-EDCL.Api:
	@echo "Starting EDCL.Api..."
	@cd src/Host/EDCL.Api && nohup dotnet run --no-build > ../../../edcl_api.log 2>&1 & echo $$! > edcl_api.pid
	@sleep 3
	@if kill -0 `cat edcl_api.pid` 2>/dev/null; then \
		echo "EDCL.Api is running in background. Logs: edcl_api.log"; \
	else \
		echo "ERROR: EDCL.Api failed to start. Check edcl_api.log for details."; \
		exit 1; \
	fi

# Stop EDCL.Api yang berjalan di lokal
stop-EDCL.Api:
	@echo "Stopping EDCL.Api..."
	@if [ -f edcl_api.pid ]; then kill `cat edcl_api.pid` 2>/dev/null || true; rm edcl_api.pid; fi
	@fuser -k 5140/tcp 2>/dev/null || true
	@pkill -f "[e]dcl.api" 2>/dev/null || true
	@echo "EDCL.Api stopped."

# Run EDCL.Gateway secara lokal (background)
run-EDCL.Gateway:
	@echo "Starting EDCL.Gateway..."
	@cd src/Gateway/EDCL.Gateway && nohup dotnet run --no-build > ../../../edcl_gateway.log 2>&1 & echo $$! > edcl_gateway.pid
	@sleep 3
	@if kill -0 `cat edcl_gateway.pid` 2>/dev/null; then \
		echo "EDCL.Gateway is running in background. Logs: edcl_gateway.log"; \
	else \
		echo "ERROR: EDCL.Gateway failed to start. Check edcl_gateway.log for details."; \
		exit 1; \
	fi

# Stop EDCL.Gateway yang berjalan di lokal
stop-EDCL.Gateway:
	@echo "Stopping EDCL.Gateway..."
	@if [ -f edcl_gateway.pid ]; then kill `cat edcl_gateway.pid` 2>/dev/null || true; rm edcl_gateway.pid; fi
	@fuser -k 5293/tcp 2>/dev/null || true
	@pkill -f "[e]dcl.gateway" 2>/dev/null || true
	@echo "EDCL.Gateway stopped."

# Run EDCL.Worker.GpsTracker secara lokal
run-EDCL.Worker.GpsTracker:
	@echo "Starting EDCL.Worker.GpsTracker..."
	@cd src/Workers/EDCL.Worker.GpsTracker && nohup dotnet run --no-build > ../../../edcl_worker_gpstracker.log 2>&1 & echo $$! > edcl_worker_gpstracker.pid
	@sleep 3
	@if kill -0 `cat edcl_worker_gpstracker.pid` 2>/dev/null; then \
		echo "EDCL.Worker.GpsTracker is running in background. Logs: edcl_worker_gpstracker.log"; \
	else \
		echo "ERROR: EDCL.Worker.GpsTracker failed to start. Check edcl_worker_gpstracker.log for details."; \
		exit 1; \
	fi

# Stop EDCL.Worker.GpsTracker
stop-EDCL.Worker.GpsTracker:
	@echo "Stopping EDCL.Worker.GpsTracker..."
	@if [ -f edcl_worker_gpstracker.pid ]; then kill `cat edcl_worker_gpstracker.pid` 2>/dev/null || true; rm edcl_worker_gpstracker.pid; fi
	@pkill -f "[e]dcl.worker.gpstracker" 2>/dev/null || true
	@echo "EDCL.Worker.GpsTracker stopped."

# Run semua Worker secara lokal (background)
run-workers: run-EDCL.Worker.GpsTracker
	@echo "Starting EDCL.Worker.Ingestion..."
	@cd workers/EDCL.Worker.Ingestion && nohup dotnet run --no-build > ../../edcl_worker_ingestion.log 2>&1 & echo $$! > edcl_worker_ingestion.pid
	@echo "Starting EDCL.Worker.Outbox..."
	@cd workers/EDCL.Worker.Outbox && nohup dotnet run --no-build > ../../edcl_worker_outbox.log 2>&1 & echo $$! > edcl_worker_outbox.pid
	@echo "Starting EDCL.Worker.Reporter..."
	@cd workers/EDCL.Worker.Reporter && nohup dotnet run --no-build > ../../edcl_worker_reporter.log 2>&1 & echo $$! > edcl_worker_reporter.pid
	@sleep 3
	@failed=0; \
	if ! kill -0 `cat edcl_worker_ingestion.pid` 2>/dev/null; then echo "ERROR: EDCL.Worker.Ingestion failed to start. Check edcl_worker_ingestion.log"; failed=1; fi; \
	if ! kill -0 `cat edcl_worker_outbox.pid` 2>/dev/null; then echo "ERROR: EDCL.Worker.Outbox failed to start. Check edcl_worker_outbox.log"; failed=1; fi; \
	if ! kill -0 `cat edcl_worker_reporter.pid` 2>/dev/null; then echo "ERROR: EDCL.Worker.Reporter failed to start. Check edcl_worker_reporter.log"; failed=1; fi; \
	if [ $$failed -eq 1 ]; then exit 1; fi
	@echo "All Workers are running in background. Logs: edcl_worker_*.log"

# Stop semua Worker yang berjalan di lokal
stop-workers: stop-EDCL.Worker.GpsTracker
	@echo "Stopping all Workers..."
	@if [ -f edcl_worker_ingestion.pid ]; then kill `cat edcl_worker_ingestion.pid` 2>/dev/null || true; rm edcl_worker_ingestion.pid; fi
	@pkill -f "[e]dcl.worker.ingestion" 2>/dev/null || true
	@if [ -f edcl_worker_outbox.pid ]; then kill `cat edcl_worker_outbox.pid` 2>/dev/null || true; rm edcl_worker_outbox.pid; fi
	@pkill -f "[e]dcl.worker.outbox" 2>/dev/null || true
	@if [ -f edcl_worker_reporter.pid ]; then kill `cat edcl_worker_reporter.pid` 2>/dev/null || true; rm edcl_worker_reporter.pid; fi
	@pkill -f "[e]dcl.worker.reporter" 2>/dev/null || true
	@echo "All Workers stopped."

# Build seluruh solution terlebih dahulu agar tidak terjadi race condition file sharing (.pdb / .dll)
build-all:
	@echo "Building entire backend solution..."
	@dotnet build EDCL.slnx
	@echo "Build complete."

# Run API, Gateway, dan Workers secara lokal (background)
run-all: build-all run-EDCL.Api run-EDCL.Gateway run-workers

# Stop API, Gateway, dan Workers lokal
stop-all: stop-EDCL.Api stop-EDCL.Gateway stop-workers
