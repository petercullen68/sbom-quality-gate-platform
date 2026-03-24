# SBOM Quality Gate Platform

## 🎯 Overview

The SBOM Quality Gate Platform is a backend system designed to:

* Ingest Software Bill of Materials (SBOM) documents
* Validate SBOM quality using external tooling (sbomqs)
* Enforce policy-based compliance (e.g., NIS2)
* Provide auditability and visibility into SBOM quality across teams and products

The system is designed with a strong emphasis on:

* Clean architecture
* Scalability
* Testability
* Separation of concerns

---

## 🧱 Architecture

The system follows a layered architecture:

```
API → Application → Domain
             ↓
     Infrastructure (DB, external tools)
             ↓
           Worker
```

### 🔹 API

* Handles HTTP requests (upload, submit, feature discovery)
* Performs input validation and request shaping
* Does **not** perform heavy processing

### 🔹 Application

* Contains use cases (handlers)
* Coordinates workflows (submit SBOM, process job, discover features)
* Depends only on interfaces (no infrastructure coupling)

### 🔹 Domain

* Core entities:

    * Sbom
    * ValidationJob
    * ValidationResult
    * SbomFeature
* Business enums and rules

### 🔹 Infrastructure

* EF Core persistence (Postgres)
* External tool integration (sbomqs)
* Process execution abstraction (IProcessRunner)
* UnitOfWork implementation

### 🔹 Worker

* Background service
* Listens for DB notifications (LISTEN/NOTIFY)
* Processes validation jobs asynchronously
* Handles retries, backoff, and resilience

---

## 🔄 Core Flow

1. **SBOM submitted via API**
2. SBOM stored in database
3. Validation job created (Pending)
4. Worker picks up job
5. sbomqs executed
6. Result stored
7. Job marked Completed or Failed

```
API → DB → Job → Worker → sbomqs → Result
```

---

## ⚙️ Key Design Decisions

### ✔ API / Worker separation

* API is fast and responsive
* Worker handles heavy processing
* Enables independent scaling

### ✔ UnitOfWork abstraction

* Centralized transaction handling
* Enables testability without EF dependency

### ✔ IValidationTool abstraction

* sbomqs is pluggable
* Future tools can be added easily

### ✔ IProcessRunner abstraction

* Removes direct OS/process dependency
* Enables mocking in unit tests

### ✔ Policy-driven validation (future)

* Policies define pass/fail rules
* Supports global, team, and product-level enforcement

---

## 🧪 Testing Strategy

### Unit Tests

* Located in `/tests/UnitTests`
* Use fakes (no DB, no external processes)
* Test application logic (handlers)

### Integration Tests

* Located in `/tests/IntegrationTests`
* Use real database and infrastructure
* Validate end-to-end behavior

---

## 📊 Feature Discovery

The system extracts supported SBOM features from sbomqs reports:

* Reads `comprehenssive` (and fallback `comprehensive`)
* Stores feature metadata:

    * category
    * feature name

This enables:

* Policy alignment with real tool capabilities
* Dynamic UI generation (future)

---

## 🛡️ Resilience

* Timeout protection on sbomqs execution
* Circuit breaker (temporary disable on repeated failures)
* Retry with exponential backoff in worker
* Fallback polling if notifications fail

---

## 🚀 Future Enhancements

* Policy engine (NIS2 and custom policies)
* UI for SBOM visibility and compliance
* SBOM generation (e.g., from package.json)
* AI summarization of SBOM reports
* Multi-tool validation support
* Hosted demo environment

---

## 🧠 Design Philosophy

This system prioritizes:

* Simplicity over cleverness
* Explicit boundaries over hidden coupling
* Testability as a first-class concern
* Incremental evolution over big rewrites

---

## 📌 Current Status

* SBOM ingestion ✔
* Job processing ✔
* sbomqs integration ✔
* Feature discovery ✔
* Unit testing foundation ✔
* Policy engine 🚧 (next phase)

---

## 👤 Author

Built as a clean-room, enterprise-grade SBOM validation platform with a focus on real-world applicability and extensibility.
