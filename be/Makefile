.PHONY: help up down be-up be-down

help:
	@echo "================================================================="
	@echo "⚙️ EDCL Backend Container CLI"
	@echo "================================================================="
	@echo "  make up (or be-up)    - Start all backend containers (Database, Redis, RabbitMQ, Gateway, API, Workers)"
	@echo "  make down (or be-down)- Stop and remove all backend containers"
	@echo "================================================================="

DOCKER_COMPOSE ?= $(shell if command -v docker-compose >/dev/null 2>&1; then echo "docker-compose"; elif command -v docker >/dev/null 2>&1; then echo "docker compose"; elif command -v podman-compose >/dev/null 2>&1; then echo "podman-compose"; else echo "podman compose"; fi)

# =============================================================================
# Backend Containers (.NET 10 & Docker)
# =============================================================================
up:
	$(DOCKER_COMPOSE) up -d --build

be-up: up

down:
	$(DOCKER_COMPOSE) down

be-down: down
