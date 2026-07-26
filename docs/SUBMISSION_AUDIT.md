# RentalApp submission audit

Audit date: 26 July 2026  
Student: Justin Wylie (40535392)

This is an evidence-based gate, not a prediction of the final grade. A status is
only **PASS** when the available source or supplied execution output proves it.

| Submission check | Status | Evidence and action |
| --- | --- | --- |
| Public GitHub repository is accessible | **BLOCKED** | No public repository URL or connected GitHub repository was supplied. Add the URL to the report, then open it in a signed-out browser to prove public access. |
| All required features implemented | **PASS (source/test evidence)** | Tier 1 and Tier 2 are mapped in `COMPLETION_MATRIX.md`. The source includes authentication, item CRUD, PostGIS nearby search, full rental workflow, verified reviews, MVVM, repositories, and service layers. State Pattern and overdue detection provide advanced evidence. Complete one final live demo before submission. |
| Tests pass and coverage exceeds the threshold | **PASS** | Latest supplied Docker/Linux result: 65 total, 65 passed, 0 failed, 0 skipped in 16 seconds; Cobertura line coverage 86.4%. This exceeds the brief's 80% distinction testing threshold. |
| GitHub Actions pipeline passes | **BLOCKED** | `.github/workflows/build.yml` now has a manual trigger, current Node 24-compatible actions, a PostGIS test service, an enforced 80% coverage gate, explicit Android API 36 setup, and signed APK/test artifacts. Its structure was validated locally, but no repository or live GitHub run was available to inspect. Push to `main`, confirm both jobs are green, and capture the run. |
| README has complete setup instructions | **PASS** | `README.md` covers prerequisites, Docker/API startup, Android build/install, test/coverage commands, production-style configuration, API routes, architecture, and shutdown/reset. |
| Report contains all Section 10 sections | **PARTIAL** | The generated report contains all eight named sections, four diagrams, feature checklist, test groups, three test excerpts, CI workflow excerpt, four design-pattern examples, AI record, and references. It still needs the public GitHub URL, seven remaining app screenshots, the coverage screenshot/export, and the green workflow screenshot. |
| Report is PDF and under 20 pages | **PASS** | The revised Word report was rendered to PDF and every page was visually inspected. The result is 16 pages, below the 20-page maximum, with no clipped or overlapping content. |
| No credentials or sensitive data | **PASS with local-demo note** | No `.env`, keystore, APK, private key, or production secret is included. `.gitignore` excludes those artifacts, and `docker-compose.production.yml` requires environment-supplied secrets. The public source intentionally documents disposable local/demo credentials; they are not production secrets. |
| Code is well-commented | **PASS** | The production projects contain 147 comment lines, including 47 XML-documentation lines and 29 searchable `Presentation point:` explanations around architectural decisions. |
| At least 15 commits over the project period | **BLOCKED** | The submitted ZIP does not contain `.git` history and no GitHub repository was available. Check the public repository's commit history. Do not manufacture back-dated or meaningless commits. |
| AI usage is documented | **PASS** | `AI_USAGE.md` records the tools, nine significant interactions, evaluation/modification decisions, validation, rejected scope, and a critical reflection. The report includes representative interactions and the reflection. |

## Required evidence still to collect

1. Public GitHub URL, checked while signed out.
2. One green GitHub Actions run showing both `backend-tests` and
   `android-build`.
3. GitHub history showing at least 15 genuine commits over the project period.
4. Seven remaining emulator screenshots: login/registration, browse, item
   detail/request, listing edit, create item/address lookup, rentals, and
   verified review/profile.
5. Coverage artifact screenshot showing 65 passing tests and 86.4% line
   coverage.
6. Final Android publish/install and one complete live workflow on the exact
   submitted source.

## Final submission rule

Do not tick the three GitHub checks or the report evidence check until the
external evidence above exists. The implementation and local quality evidence
are strong, but the quantitative report awards marks for visible evidence, not
for unverified statements.
