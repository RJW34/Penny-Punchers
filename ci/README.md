# GitHub Actions workflow template

`verify.yml` contains the pinned Linux simulation checks and optional native Windows job. It is retained here because the current GitHub OAuth login rejected writes to `.github/workflows/` without the workflow scope. The game source and local verification do not depend on GitHub Actions.

To activate it using a GitHub login authorized to manage workflows, place this exact file at `.github/workflows/verify.yml`. The optional native job additionally needs the documented matching Godot/.NET toolchain and a self-hosted runner with the `penny-punchers-native` label. No such runner is claimed to be installed by this template.
