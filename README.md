# Physics Sandbox Unity Package

This repository can be consumed directly by Unity's Package Manager using a Git URL.

## Prerequisites

- Unity 2019.4 or newer (recommended: Unity 2021 LTS+).
- A valid `package.json` at the repository root that includes at minimum:
  - `name` (for example: `com.yourorg.physics-sandbox`)
  - `version` (for example: `1.0.0`)
  - `displayName`
  - `unity` (minimum supported Unity version)

If `package.json` is missing, create it at the root before trying to install from Git.

## Install from a GitHub URL in Unity

1. Open your Unity project.
2. Go to **Window > Package Manager**.
3. Click the **+** button in the top-left of the Package Manager window.
4. Select **Add package from git URL...**.
5. Enter this repository URL:

   ```text
   https://github.com/<owner>/physics-sandbox.git
   ```

6. Click **Add**.

Unity will fetch the package and add it to your project.

## Install a specific branch, tag, or commit

You can pin installation to a ref by appending it to the URL:

- Branch:

  ```text
  https://github.com/<owner>/physics-sandbox.git#main
  ```

- Tag:

  ```text
  https://github.com/<owner>/physics-sandbox.git#v1.0.0
  ```

- Commit:

  ```text
  https://github.com/<owner>/physics-sandbox.git#<commit-sha>
  ```

## Install through `Packages/manifest.json`

You can also edit your Unity project's `Packages/manifest.json` directly:

```json
{
  "dependencies": {
    "com.yourorg.physics-sandbox": "https://github.com/<owner>/physics-sandbox.git#main"
  }
}
```

## Recommended repository structure for Unity packages

At minimum:

```text
physics-sandbox/
├─ package.json
├─ Runtime/
├─ Editor/                (optional)
├─ Samples~/              (optional)
├─ Documentation~/        (optional)
└─ LICENSE
```

This keeps the package compatible with Unity Package Manager expectations.
