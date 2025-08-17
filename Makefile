PWSH := pwsh -NoProfile
DLL := libs/net8.0/MT.HexDump.dll
CSharpFiles = $(shell find src \( -name ".git" -o -name "obj" -o -name "bin" \) -prune -o -name "*.cs" -print)

.ONESHELL:

$(DLL): $(CSharpFiles)
	dotnet build --nologo -c Release src

.PHONY: build
build: $(DLL) ## Build C# Projects

.PHONY: test
test: build ## Build and Run tests
	@$(PWSH) -File test/test.ps1

.PHONY: help
help: ## Display this help
	@echo "Targets:"
	grep -E '^[a-zA-Z_-]+:.*?## .*$$' /dev/null $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":|## "}; {printf "  %-20s %s\n", $$(NF-2), $$NF}'

