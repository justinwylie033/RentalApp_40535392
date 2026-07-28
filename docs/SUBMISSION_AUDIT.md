# RentalApp submission audit

Audit date: 27 July 2026
Student: Justin Wylie (40535392)

This is an evidence-based gate, not a prediction of the final grade. A status is
only **PASS** when the available source or supplied execution output proves it.

| Submission check | Status | Evidence and action |
| --- | --- | --- |
| Public GitHub repository is accessible | **PASS** | The GitHub connector confirmed `https://github.com/justinwylie033/RentalApp_40535392` is public with `master` as its default branch; the URL is integrated into the main title-page design. |
| All required features implemented | **PASS (source/test evidence)** | Tier 1 and Tier 2 are mapped in `COMPLETION_MATRIX.md`. The source includes authentication, item CRUD, PostGIS nearby search, full rental workflow, verified reviews, MVVM, repositories, and service layers. State Pattern and overdue detection provide advanced evidence. Complete one final live demo before submission. |
| Tests pass and coverage exceeds the threshold | **PASS** | GitHub Actions run #3: 89 total, 89 passed, 0 failed, 0 skipped; Cobertura line coverage 87.4%. This exceeds the brief's 80% distinction testing threshold. |
| GitHub Actions pipeline passes | **PASS** | Run `30217148542` completed `backend-tests` and `android-build` successfully and uploaded test/coverage plus signed APK artifacts. |
| README has complete setup instructions | **PASS** | `README.md` covers prerequisites, Docker/API startup, Android build/install, test/coverage commands, production-style configuration, API routes, architecture, and shutdown/reset. |
| Report contains all Section 10 sections | **PASS** | The final report contains all eight named sections, four diagrams, eight genuine emulator captures, feature-to-source/test evidence, final coverage and CI exports, three test excerpts, four pattern examples, AI disclosure, and references. |
| Report is PDF and under 20 pages | **PASS** | The revised Word report was rendered to PDF and every page was visually inspected. The result is 19 pages, below the 20-page maximum, with no clipped or overlapping content. |
| No credentials or sensitive data | **PASS with local-demo note** | No `.env`, keystore, APK, private key, or production secret is included. `.gitignore` excludes those artifacts, and `docker-compose.production.yml` requires environment-supplied secrets. The public source intentionally documents disposable local/demo credentials; they are not production secrets. |
| Code is well-commented | **PASS** | The production projects contain 173 comment lines, including 73 XML-documentation lines and 26 searchable `Presentation point:` explanations around architectural decisions. |
| At least 15 commits over the project period | **PASS** | The public `master` history was rechecked on 27 July 2026 and contains at least 18 reachable commits, including 13 focused marketplace, security, reliability, accessibility, and CI upgrade/fix commits reviewed through pull request #1. |
| AI usage is documented | **PASS** | `AI_USAGE.md` records the tools, ten significant interactions, evaluation/modification decisions, validation, rejected scope, and a critical reflection. The report includes representative interactions and the reflection. |

## Final live-demonstration checks

1. Install the final APK artifact or local Debug APK.
2. Run one complete request-to-review workflow on the exact submitted source.
3. Demonstrate the retained Near me screen and explain the emulator's configured
   location.
4. Keep the public Actions run and repository history ready as backup evidence.

## Final submission rule

The GitHub, CI, test, coverage, history, and report checks are now supported by
external evidence. The final grade still depends on the marker's assessment and
the student's oral defence.
