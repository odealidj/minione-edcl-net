.PHONY: idcs-up idcs-down seed-init seed-master seed-transaction seed-out-of-order seed-race-condition seed-bulk reset-transaction reset-master seed-one-master reset-pickup trigger-reject

idcs-up:
	docker-compose -f docker-compose-idcs.yml up -d

idcs-down:
	docker-compose -f docker-compose-idcs.yml down

seed-init:
	dotnet run init

seed-master:
	dotnet run all

seed-transaction:
	dotnet run transaction

seed-out-of-order:
	dotnet run out-of-order

seed-race-condition:
	dotnet run race-condition

seed-bulk:
	dotnet run bulk

reset-transaction:
	dotnet run reset

reset-master:
	dotnet run reset-all

reset-pickup:
	dotnet run reset-pickup

trigger-reject:
	dotnet run trigger-reject

seed-k6-clean:
	cd ../be/tests/k6/EDCL.K6Seeder && dotnet run clean

seed-one-master:
	dotnet run one-master
