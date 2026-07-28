# GitHub submission hand-off

Use this checklist on the exact folder that will be submitted. The public
repository and CI evidence are now verified; the remaining task is to publish
the final documentation changes without losing the genuine history.

## 1. Preserve any genuine history

Run these commands from the repository root in PowerShell:

```powershell
git rev-parse --is-inside-work-tree
git remote -v
git rev-list --count HEAD
git log --oneline --decorate --reverse
```

If the first command returns `true`, keep that `.git` directory. Do not extract
a ZIP over a different folder and then initialise a replacement repository,
because that discards the real development history.

If the folder is not yet a Git repository, create one once:

```powershell
git init -b main
git add .
git commit -m "feat: complete RentalApp coursework implementation"
```

That creates one truthful snapshot commit. It does not prove fifteen commits
over the project period. Do not split unchanged files, backdate commits, or
create meaningless commits to manufacture the rubric evidence.

## 2. Verified public repository

Verified on 28 July 2026:

- Public repository:
  `https://github.com/justinwylie033/RentalApp_40535392`
- Default branch: `master`
- Reachable commits on `master`: 20
- Pull requests #1 and #2: merged

Before the final push, confirm the local remote and branch:

```powershell
git remote get-url origin
git branch --show-current
git fetch origin master
git status --short
```

## 3. Verify GitHub Actions

The workflow starts on pushes and pull requests to `main` or `master`, and it can also be
started manually from **Actions → Build, test and package → Run workflow**.

The run is only complete when both jobs are green:

- `backend-tests`: restores and builds the backend, runs the 89 xUnit tests
  against a PostGIS service, enforces at least 80% line coverage, generates a
  readable coverage report, and uploads the evidence.
- `android-build`: installs .NET 10, MAUI Android, Android API 36, and Java 17;
  creates an ephemeral CI signing key; publishes a signed APK; and uploads it.

Download these artifacts from the successful run:

- `test-results-and-coverage`
- `rentalapp-android`

If a job fails, open that job and copy the first failing step and its complete
error message. Fix the cause in source, commit the real fix, push it, and wait
for the replacement run.

Verified final run `30390459728` completed both jobs successfully. Its test artifact
contains 89 passing tests and 87.4% line coverage.

## 4. Final report evidence

The final report now includes:

- the public GitHub URL and 18-commit verification;
- API-verified evidence that both Actions jobs are green;
- the 89-test and 87.4%-coverage export;
- four architecture diagrams;
- eight genuine Android emulator screenshots;
- every report section listed in Section 10 of the brief.

The rendered PDF is 19 pages and has passed a complete page-by-page visual
inspection.

## 5. Publish the final documentation

Inspect and commit only the real final-report changes:

```powershell
git status --short
git diff
git add README.md docs tools/build_report.py output/report output/pdf
git diff --cached
git commit -m "docs: finalise verified coursework report"
git push origin master
```

Do not add generated archives, APKs, keystores, `.env`, or local test-result
folders. After pushing, confirm the new `master` workflow run is green before
submission.
