---
sidebar_position: 1
---

# CI/CD Integration

SBOM Quality Gate is designed to integrate into your existing CI/CD pipelines. This guide covers common integration patterns.

## Overview

A typical integration:

1. **Build** your application
2. **Generate** an SBOM from the build artifacts
3. **Submit** the SBOM to Quality Gate
4. **Poll** for validation results
5. **Gate** the pipeline based on pass/fail

## GitHub Actions

### Basic Integration

```yaml
name: Build and Validate SBOM

on:
  push:
    branches: [main]
  pull_request:

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'

      - name: Build
        run: dotnet build --configuration Release

      - name: Generate SBOM
        run: |
          dotnet tool install --global CycloneDX
          dotnet CycloneDX ./src/MyApp/MyApp.csproj -o sbom.json -j

      - name: Submit to SBOM Quality Gate
        id: submit
        run: |
          RESPONSE=$(curl -s -X POST ${{ vars.SBOM_GATE_URL }}/api/sboms/upload \
            -F "file=@sbom.json" \
            -F "team=${{ github.repository_owner }}" \
            -F "project=${{ github.event.repository.name }}" \
            -F "version=${{ github.sha }}")
          
          SBOM_ID=$(echo $RESPONSE | jq -r '.id')
          echo "sbom_id=$SBOM_ID" >> $GITHUB_OUTPUT

      - name: Wait for Validation
        run: |
          # Simple polling - production should use proper retry logic
          for i in {1..30}; do
            echo "Checking validation status (attempt $i)..."
            sleep 5
            # TODO: Poll validation result endpoint when available
          done

      - name: Upload SBOM as Artifact
        uses: actions/upload-artifact@v4
        with:
          name: sbom
          path: sbom.json
```

### With Reusable Workflow

Create a reusable workflow for consistency across repositories:

```yaml title=".github/workflows/sbom-validate.yml"
name: SBOM Validation

on:
  workflow_call:
    inputs:
      project-path:
        required: true
        type: string
      team:
        required: true
        type: string
    secrets:
      SBOM_GATE_URL:
        required: true

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - name: Generate SBOM
        run: |
          dotnet tool install --global CycloneDX
          dotnet CycloneDX ${{ inputs.project-path }} -o sbom.json -j

      - name: Submit SBOM
        run: |
          curl -X POST ${{ secrets.SBOM_GATE_URL }}/api/sboms/upload \
            -F "file=@sbom.json" \
            -F "team=${{ inputs.team }}" \
            -F "project=${{ github.event.repository.name }}" \
            -F "version=${{ github.sha }}"
```

Then call it from your project:

```yaml
jobs:
  sbom:
    uses: your-org/.github/.github/workflows/sbom-validate.yml@main
    with:
      project-path: ./src/MyApp/MyApp.csproj
      team: platform
    secrets:
      SBOM_GATE_URL: ${{ secrets.SBOM_GATE_URL }}
```

## GitLab CI

```yaml title=".gitlab-ci.yml"
stages:
  - build
  - sbom
  - deploy

variables:
  SBOM_GATE_URL: https://sbom-gate.example.com

build:
  stage: build
  script:
    - dotnet build --configuration Release
  artifacts:
    paths:
      - bin/

generate-sbom:
  stage: sbom
  script:
    - dotnet tool install --global CycloneDX
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - dotnet CycloneDX ./MyApp.csproj -o sbom.json -j
    - |
      curl -X POST ${SBOM_GATE_URL}/api/sboms/upload \
        -F "file=@sbom.json" \
        -F "team=${CI_PROJECT_NAMESPACE}" \
        -F "project=${CI_PROJECT_NAME}" \
        -F "version=${CI_COMMIT_SHA}"
  artifacts:
    paths:
      - sbom.json
  dependencies:
    - build
```

## Azure DevOps

```yaml title="azure-pipelines.yml"
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

variables:
  SBOM_GATE_URL: $(SbomGateUrl)

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: dotnet build --configuration Release
    displayName: 'Build'

  - script: |
      dotnet tool install --global CycloneDX
      dotnet CycloneDX ./src/MyApp/MyApp.csproj -o $(Build.ArtifactStagingDirectory)/sbom.json -j
    displayName: 'Generate SBOM'

  - script: |
      curl -X POST $(SBOM_GATE_URL)/api/sboms/upload \
        -F "file=@$(Build.ArtifactStagingDirectory)/sbom.json" \
        -F "team=$(System.TeamProject)" \
        -F "project=$(Build.Repository.Name)" \
        -F "version=$(Build.SourceVersion)"
    displayName: 'Submit SBOM'

  - task: PublishBuildArtifacts@1
    inputs:
      pathToPublish: '$(Build.ArtifactStagingDirectory)/sbom.json'
      artifactName: 'sbom'
```

## Jenkins

```groovy title="Jenkinsfile"
pipeline {
    agent any
    
    environment {
        SBOM_GATE_URL = credentials('sbom-gate-url')
    }
    
    stages {
        stage('Build') {
            steps {
                sh 'dotnet build --configuration Release'
            }
        }
        
        stage('Generate SBOM') {
            steps {
                sh '''
                    dotnet tool install --global CycloneDX
                    export PATH="$PATH:$HOME/.dotnet/tools"
                    dotnet CycloneDX ./MyApp.csproj -o sbom.json -j
                '''
            }
        }
        
        stage('Submit SBOM') {
            steps {
                sh '''
                    curl -X POST ${SBOM_GATE_URL}/api/sboms/upload \
                        -F "file=@sbom.json" \
                        -F "team=${JOB_NAME%%/*}" \
                        -F "project=${JOB_BASE_NAME}" \
                        -F "version=${GIT_COMMIT}"
                '''
            }
        }
    }
    
    post {
        always {
            archiveArtifacts artifacts: 'sbom.json', fingerprint: true
        }
    }
}
```

## Best Practices

### 1. Generate at Build Time

Generate SBOMs from actual build artifacts, not just manifest files:

```bash
# Good: After build, from compiled output
dotnet CycloneDX ./bin/Release/net10.0/MyApp.dll -o sbom.json

# Less good: From project file only (may miss transitive deps)
dotnet CycloneDX ./MyApp.csproj -o sbom.json
```

### 2. Version with Commit SHA

Use the git commit SHA for traceability:

```bash
-F "version=${GITHUB_SHA}"
```

### 3. Fail Fast vs. Warn

Decide whether SBOM validation should block deployment:

- **Development**: Warn only, don't block
- **Staging**: Warn, collect metrics
- **Production**: Block on failure

### 4. Cache SBOM Tools

Avoid reinstalling tools on every build:

```yaml
- name: Cache CycloneDX
  uses: actions/cache@v4
  with:
    path: ~/.dotnet/tools
    key: cyclonedx-${{ runner.os }}
```

### 5. Store SBOMs as Artifacts

Always archive generated SBOMs for audit trails:

```yaml
- uses: actions/upload-artifact@v4
  with:
    name: sbom-${{ github.sha }}
    path: sbom.json
    retention-days: 90
```

## Polling for Results

Until a webhook or streaming API is available, poll for results:

```bash
#!/bin/bash
SBOM_ID=$1
MAX_ATTEMPTS=30
INTERVAL=5

for ((i=1; i<=MAX_ATTEMPTS; i++)); do
    echo "Checking status (attempt $i/$MAX_ATTEMPTS)..."
    
    RESPONSE=$(curl -s "${SBOM_GATE_URL}/api/sboms/${SBOM_ID}")
    
    # TODO: Check validation result when endpoint is available
    # STATUS=$(echo $RESPONSE | jq -r '.validationStatus')
    # if [ "$STATUS" != "pending" ]; then
    #     echo "Validation complete: $STATUS"
    #     exit $([ "$STATUS" == "pass" ] && echo 0 || echo 1)
    # fi
    
    sleep $INTERVAL
done

echo "Timeout waiting for validation"
exit 1
```

## Next Steps

- [API Reference](../api) — Full API documentation
- [Dependency-Track Integration](./dependency-track) — Vulnerability scanning
