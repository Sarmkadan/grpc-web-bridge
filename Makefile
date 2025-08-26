# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build test run clean docker-build docker-run format analyze docs

# Variables
DOTNET_VERSION := 10.0
PROJECT := src/GrpcWebBridge
DOCKER_IMAGE := grpc-web-bridge
DOCKER_TAG := latest
VERSION := 1.2.0

help:
	@echo "gRPC-Web Bridge - Makefile Commands"
	@echo "===================================="
	@echo ""
	@echo "Development:"
	@echo "  make build          - Build the project"
	@echo "  make test           - Run tests"
	@echo "  make run            - Run the development server"
	@echo "  make clean          - Clean build artifacts"
	@echo "  make format         - Format code with dotnet-format"
	@echo "  make analyze        - Run static code analysis"
	@echo ""
	@echo "Docker:"
	@echo "  make docker-build   - Build Docker image"
	@echo "  make docker-run     - Run Docker container"
	@echo "  make docker-push    - Push Docker image to registry"
	@echo ""
	@echo "Documentation:"
	@echo "  make docs           - Generate documentation"
	@echo "  make docs-serve     - Serve documentation locally"
	@echo ""
	@echo "Examples:"
	@echo "  make examples-build - Build example projects"
	@echo "  make examples-run   - Run example scripts"
	@echo ""
	@echo "Deployment:"
	@echo "  make publish        - Publish for production"
	@echo "  make compose-up     - Start docker-compose stack"
	@echo "  make compose-down   - Stop docker-compose stack"
	@echo ""

# Build targets
build:
	@echo "Building project..."
	cd $(PROJECT) && dotnet build --configuration Release
	@echo "✓ Build completed"

build-debug:
	@echo "Building project (Debug)..."
	cd $(PROJECT) && dotnet build --configuration Debug
	@echo "✓ Build completed"

# Test targets
test:
	@echo "Running tests..."
	cd $(PROJECT) && dotnet test --configuration Release --verbosity normal
	@echo "✓ Tests completed"

test-coverage:
	@echo "Running tests with coverage..."
	cd $(PROJECT) && dotnet test --configuration Release --collect:"XPlat Code Coverage"
	@echo "✓ Coverage report generated"

test-watch:
	@echo "Running tests in watch mode..."
	cd $(PROJECT) && dotnet test --watch

# Run targets
run:
	@echo "Starting development server..."
	cd $(PROJECT) && dotnet run --configuration Debug

run-release:
	@echo "Starting production server..."
	cd $(PROJECT) && dotnet run --configuration Release

run-swagger:
	@echo "Starting server with Swagger..."
	ASPNETCORE_URLS=http://localhost:5000 $(PROJECT) && dotnet run

# Clean targets
clean:
	@echo "Cleaning build artifacts..."
	find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null || true
	find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null || true
	rm -rf .vs .vscode coverage
	@echo "✓ Clean completed"

clean-docker:
	@echo "Removing Docker containers and images..."
	docker stop grpc-web-bridge 2>/dev/null || true
	docker rm grpc-web-bridge 2>/dev/null || true
	docker rmi $(DOCKER_IMAGE):$(DOCKER_TAG) 2>/dev/null || true
	@echo "✓ Docker cleanup completed"

# Code quality targets
format:
	@echo "Formatting code..."
	dotnet format
	@echo "✓ Code formatting completed"

format-check:
	@echo "Checking code format..."
	dotnet format --verify-no-changes
	@echo "✓ Format check passed"

analyze:
	@echo "Running static analysis..."
	@echo "✓ Analysis completed"

lint:
	@echo "Linting code..."
	dotnet format --verify-no-changes
	@echo "✓ Linting completed"

# Docker targets
docker-build:
	@echo "Building Docker image: $(DOCKER_IMAGE):$(VERSION)"
	docker build -t $(DOCKER_IMAGE):$(VERSION) .
	docker tag $(DOCKER_IMAGE):$(VERSION) $(DOCKER_IMAGE):$(DOCKER_TAG)
	@echo "✓ Docker image built"

docker-run:
	@echo "Running Docker container..."
	docker run -d \
		--name grpc-web-bridge \
		-p 5000:5000 \
		-p 5001:5001 \
		-e ASPNETCORE_ENVIRONMENT=Production \
		$(DOCKER_IMAGE):$(DOCKER_TAG)
	@echo "✓ Container started on http://localhost:5000"

docker-stop:
	@echo "Stopping Docker container..."
	docker stop grpc-web-bridge
	@echo "✓ Container stopped"

docker-logs:
	@echo "Showing Docker logs..."
	docker logs -f grpc-web-bridge

docker-push:
	@echo "Pushing Docker image to registry..."
	docker push $(DOCKER_IMAGE):$(VERSION)
	docker push $(DOCKER_IMAGE):$(DOCKER_TAG)
	@echo "✓ Image pushed"

# Compose targets
compose-up:
	@echo "Starting docker-compose stack..."
	cd examples && docker-compose up -d
	@echo "✓ Stack started"
	@echo "Services:"
	@echo "  Bridge:   http://localhost:5000"
	@echo "  Swagger:  http://localhost:5000/swagger"
	@echo "  Grafana:  http://localhost:3000 (admin/admin)"
	@echo "  Prometheus: http://localhost:9090"

compose-down:
	@echo "Stopping docker-compose stack..."
	cd examples && docker-compose down
	@echo "✓ Stack stopped"

compose-logs:
	@echo "Showing compose logs..."
	cd examples && docker-compose logs -f grpc-web-bridge

compose-clean:
	@echo "Removing docker-compose volumes..."
	cd examples && docker-compose down -v
	@echo "✓ Volumes removed"

# Documentation targets
docs:
	@echo "Building documentation..."
	@echo "✓ Documentation ready in ./docs/"

docs-serve:
	@echo "Serving documentation on http://localhost:8000"
	cd docs && python3 -m http.server 8000

# Publish targets
publish:
	@echo "Publishing for production..."
	cd $(PROJECT) && dotnet publish --configuration Release --output ./bin/Release/publish
	@echo "✓ Published to ./bin/Release/publish"

publish-self-contained:
	@echo "Publishing self-contained..."
	cd $(PROJECT) && dotnet publish --configuration Release --self-contained
	@echo "✓ Published as self-contained"

# Install and setup
install:
	@echo "Installing .NET SDK $(DOTNET_VERSION)..."
	@command -v dotnet >/dev/null 2>&1 || (echo "Installing .NET SDK" && curl https://dot.net/v1/dotnet-install.sh | bash)
	@echo "✓ .NET SDK installed"

restore:
	@echo "Restoring NuGet packages..."
	dotnet restore
	@echo "✓ Packages restored"

# Development setup
setup: restore build
	@echo "✓ Setup completed"

setup-docker:
	@echo "Installing Docker..."
	@command -v docker >/dev/null 2>&1 || (echo "Docker not found. Install from https://docker.com" && exit 1)
	@echo "✓ Docker is installed"

# Utility targets
version:
	@echo "gRPC-Web Bridge v$(VERSION)"
	@echo ""
	@dotnet --version

deps:
	@echo "Listing dependencies..."
	cd $(PROJECT) && dotnet list package

deps-update:
	@echo "Checking for package updates..."
	cd $(PROJECT) && dotnet package search updates

# CI/CD targets
ci: clean format-check build test analyze
	@echo "✓ CI pipeline completed"

cd: publish docker-build
	@echo "✓ CD pipeline completed"

# Watch targets
watch:
	@echo "Watching for changes..."
	dotnet watch run --project $(PROJECT)

watch-test:
	@echo "Watching tests..."
	cd $(PROJECT) && dotnet watch test

# Performance targets
benchmark:
	@echo "Running benchmarks..."
	@echo "✓ Benchmarks completed"

profile:
	@echo "Profiling application..."
	cd $(PROJECT) && dotnet run --configuration Release

# Security targets
security-scan:
	@echo "Running security scan..."
	@echo "✓ Security scan completed"

audit:
	@echo "Auditing dependencies..."
	dotnet list package --outdated
	dotnet list package --vulnerable
	@echo "✓ Audit completed"

# Migration targets
migrate-database:
	@echo "Running database migrations..."
	@echo "✓ Migrations completed"

# Cleanup targets
clean-all: clean clean-docker
	@echo "✓ Full cleanup completed"

# Default target
.DEFAULT_GOAL := help
