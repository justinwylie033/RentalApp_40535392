# GitHub submission hand-off

Use this checklist on the exact folder that will be submitted. It keeps the
remaining GitHub work honest and produces the evidence required by the brief.

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

## 2. Create and connect the public repository

Create an empty public GitHub repository named `RentalApp_40535392`. Do not add
a generated `.gitignore` or licence when the local repository already has an
initial commit.

Copy the repository URL, then run:

```powershell
git remote add origin https://github.com/YOUR-USER/RentalApp_40535392.git
git branch -M main
git push -u origin main
```

If `origin` already exists, inspect it before changing anything:

```powershell
git remote get-url origin
```

Only replace it when it points to the wrong repository:

```powershell
git remote set-url origin https://github.com/YOUR-USER/RentalApp_40535392.git
```

Open the repository URL in a private or signed-out browser window. The source,
README, and commit history must be visible without signing in.

## 3. Verify GitHub Actions

The workflow starts on pushes and pull requests to `main` or `master`, and it can also be
started manually from **Actions → Build, test and package → Run workflow**.

The run is only complete when both jobs are green:

- `backend-tests`: restores and builds the backend, runs the 65 xUnit tests
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

## 4. Capture final report evidence

Capture screenshots that clearly show:

1. The public repository home page and its URL.
2. The repository commit history and genuine commit count.
3. The successful workflow run with both jobs green.
4. The coverage summary showing at least 80%.
5. The Android artifact listed on the workflow run.

Also capture the seven outstanding emulator screens listed in
`REPORT_EVIDENCE_CHECKLIST.md`.

Add the public URL and screenshots to the report, export the final PDF, and
confirm it remains below twenty pages.

## 5. Suggested commits for remaining real work

These are appropriate only when the described change actually exists:

```text
ci: harden automated tests and Android packaging
docs: add GitHub submission verification guide
docs: add final workflow and emulator evidence
docs: finalise submission report
```

Stage each logical change separately and inspect it before committing:

```powershell
git status --short
git diff
git add .github/workflows/build.yml
git diff --cached
git commit -m "ci: harden automated tests and Android packaging"
```

Repeat with the files that belong to each later change. Never use `git add .`
without first checking that generated archives, APKs, keystores, `.env`, and
test-result folders are ignored.
