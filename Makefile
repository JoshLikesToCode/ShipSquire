.PHONY: help up down restart logs clean reset-db build test

help:
	@echo "ShipSquire Development Commands"
	@echo "  make up          - Start all services"
	@echo "  make down        - Stop all services"
	@echo "  make restart     - Restart all services"
	@echo "  make logs        - View logs"
	@echo "  make clean       - Remove containers and volumes"
	@echo "  make reset-db    - Reset database"
	@echo "  make build       - Build all projects"
	@echo "  make test        - Run all tests"

up:
	docker compose up -d

down:
	docker compose down

restart:
	docker compose restart

logs:
	docker compose logs -f

clean:
	docker compose down -v
	rm -rf api/ShipSquire.*/bin api/ShipSquire.*/obj
	rm -rf web/node_modules web/dist

reset-db:
	docker compose down postgres
	docker volume rm shipSquire_postgres_data || true
	docker compose up -d postgres

build:
	cd api && dotnet build
	cd web && npm install && npm run build

test:
	cd api && dotnet test
	cd web && npm test
