# Production Readiness TODO

This checklist tracks implementation progress from the production-readiness assessment.

## Phase 1 — Baseline & truth
- [x] Align docs with actual runtime/framework/testing state
- [ ] Define production SLOs, non-functional requirements, and acceptance gates
- [ ] Create a release checklist (security, test, migration, rollback)

## Phase 2 — Architecture hardening
- [ ] Introduce persistent storage and migration strategy per module
- [ ] Define transactional boundaries and consistency model across plugins
- [ ] Replace demo-only in-memory flows with durable application services

## Phase 3 — Security
- [ ] Add authentication and authorization end-to-end
- [x] Enforce plugin trust policy baseline (allowlist + version compatibility)
- [ ] Add secret management and environment-based config hardening
- [ ] Add dependency and container vulnerability scanning policy

## Phase 4 — Reliability
- [x] Add health checks, startup/readiness probes, and graceful shutdown validation baseline
- [ ] Add retry/timeouts/circuit-breaker patterns for external operations
- [ ] Define backup, restore, and disaster recovery runbooks

## Phase 5 — Testing
- [x] Expand unit tests for host behavior (event routing)
- [x] Add integration tests for host readiness endpoints and request correlation
- [ ] Add regression tests for critical business workflows
- [ ] Add coverage thresholds and fail CI when below threshold

## Phase 6 — Observability
- [x] Add correlation IDs for request tracing baseline
- [ ] Add metrics/traces and dashboards for host + plugins
- [ ] Add alerting for load failures, event handling errors, and latency

## Phase 7 — CI/CD & Release
- [ ] Add lint/static analysis quality gates and enforce branch protections
- [ ] Add environment promotion flow (dev → staging → prod)
- [ ] Add automated versioning/changelog/release notes
- [ ] Validate deployment artifact strategy (container or package-based)

## Phase 8 — Operability
- [ ] Create operational docs (runbook, on-call, incident process)
- [ ] Add configuration matrix for all environments
- [ ] Perform staging load and failover tests before first production cutover
