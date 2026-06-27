.PHONY: idcs-up idcs-down seed-init seed-manifest seed-supplier seed-part seed-kanban seed-skid seed-out-of-order seed-race-condition seed-bulk

idcs-up:
	docker-compose -f docker-compose-idcs.yml up -d

idcs-down:
	docker-compose -f docker-compose-idcs.yml down

seed-init:
	dotnet run init

seed-supplier:
	dotnet run supplier

seed-manifest:
	dotnet run manifest

seed-part:
	dotnet run part

seed-kanban:
	dotnet run kanban

seed-skid:
	dotnet run skid

seed-out-of-order:
	dotnet run out-of-order

seed-race-condition:
	dotnet run race-condition

seed-bulk:
	dotnet run bulk

seed-edcl-master:
	dotnet run edcl-master
