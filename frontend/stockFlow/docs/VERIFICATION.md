# Verification — 2026-10-28

## Completed

- `npm test`: **40 passed, 0 failed** in 7 test files using Angular TestBed + Vitest 4.1.12.
- `npm run build`: passed with Angular strict TypeScript and strict template checking, production optimization and lazy feature chunks. Initial JS/CSS: about 295.70 kB uncompressed, excluding deferred fonts/assets.
- `npm audit`: **0 known vulnerabilities**, including development dependencies, at verification time. This is a point-in-time check, not a permanent security guarantee.
- Retrieved deployed OpenAPI from `https://stock-flow.runasp.net/openapi/v1.json`; validated **79 HTTP operations** used by the UI against the snapshot.
- Live `GET /health/ready`: returned `{"status":"Healthy"}`.
- Live CORS preflight for `Origin: http://localhost:4200`, GET products with Authorization: 204, allow-origin matched. The final deployed frontend origin still has to be configured by the owner.
- Browser QA: inspected real compiled login layout. Using a **temporary local synthetic API harness**, inspected dashboard and product data, created a local test product, opened a submitted PO, created a receipt, confirmed posting and observed Posted/read-only state. No real production data was created or changed.
- Inspected dashboard at desktop width and inside a 390px iframe to exercise responsive mobile layout.
- Arabic font files are self-hosted in production assets; OFL license supplied.

## Tests cover

Role/action matrix, immutable final document actions, cancelled receipt recovery, decimal precision and safe numeric range, quoted If-Match, pagination/filter contracts and lookup pagination beyond 100 records, editable draft payload shape, no transport-error retry of writes, rotating refresh single-flight, refresh cancellation/session clearing, restoring and checking user identity, avoiding bearer leakage to unrelated hosts, 401 refresh/replay, no refresh on failed login, no replay on server failure, ProblemDetails handling, duplicate document lines, same-warehouse transfers, stale-version handling, Viewer controls, and actual component rendering for master resources/users/dashboard/documents.

## Evidence

- `evidence/unit-tests.txt`
- `evidence/production-build.txt`
- `evidence/npm-audit.json`
- `evidence/api-contract-check.txt`
- `backend-openapi.snapshot.json`

## Limits

No production account was used; authenticated end-to-end workflows against the user's hosted SQL database were **not** run. Temporary QA harness was excluded from the source and build delivery. This does not replace the backend SQL/concurrency tests or the manual acceptance checklist. Production CORS, redirects, IIS modules, hostname/TLS, and actual credentials must be checked on the selected frontend host. No deployment or git commit was performed.

Suggested commit: `feat(frontend): add Arabic Angular inventory workspace and API integration`
