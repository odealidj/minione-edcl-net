.PHONY: idcs-up idcs-down seed-init seed-manifest seed-part seed-kanban seed-skid seed-out-of-order

idcs-up:
	docker-compose -f idcs-seeder/docker-compose-idcs.yml up -d

idcs-down:
	docker-compose -f idcs-seeder/docker-compose-idcs.yml down

seed-init:
	dotnet run --project idcs-seeder init

seed-manifest:
	dotnet run --project idcs-seeder manifest

seed-part:
	dotnet run --project idcs-seeder part

seed-kanban:
	dotnet run --project idcs-seeder kanban

seed-skid:
	dotnet run --project idcs-seeder skid

seed-out-of-order:
	dotnet run --project idcs-seeder out-of-order
