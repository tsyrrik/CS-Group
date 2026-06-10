DOTNET_IMAGE := mcr.microsoft.com/dotnet/sdk:8.0
DOTNET := docker run --rm -e DOTNET_NUGET_SIGNATURE_VERIFICATION=false -v "$(CURDIR):/workspace" -w /workspace $(DOTNET_IMAGE) dotnet
SOLUTION := FileDatabaseTask.sln
WINDOWS_TARGETING := /p:EnableWindowsTargeting=true
NUGET_CONFIG := --configfile NuGet.config

.PHONY: up down ps restore build format lint check clean db-shell

up:
	docker compose up -d postgres

down:
	docker compose down

ps:
	docker compose ps

restore:
	$(DOTNET) restore $(SOLUTION) $(WINDOWS_TARGETING) $(NUGET_CONFIG)

build:
	$(DOTNET) build $(SOLUTION) $(WINDOWS_TARGETING) $(NUGET_CONFIG)

format:
	$(DOTNET) format $(SOLUTION)

lint:
	$(DOTNET) format $(SOLUTION) --verify-no-changes

check: build lint

clean:
	$(DOTNET) clean $(SOLUTION) $(WINDOWS_TARGETING)

db-shell:
	docker compose exec postgres psql -U file_scans -d file_scans
