# DevKit Loader

**DevKit Loader** is an Editor extension for Unity (2021.3 LTS and newer) that provides a personal global catalog of tools (plugins, assets, UPM packages) with one-click installation from various sources.

## Features

- **Quick Setup window** – select and install multiple tools at once.
- **Manage List window** – add, edit, or remove tools from your personal catalog.
- Supports installation from:
  - **GitHub Releases** (public repos, .unitypackage or .zip)
  - **GitLab Releases** (public projects)
  - **Direct URLs** (direct links to .unitypackage or .zip)
  - **UPM packages** (Git URLs or scoped registries)
  - **Asset Store** (opens the store page in browser)
- Asynchronous downloads with progress reporting and cancellation.
- Automatic cache of GitHub/GitLab API responses (ETag, 24h TTL) to avoid rate limits.
- First-run wizard: opens Quick Setup automatically when imported into a new project.

## Installation

1. Download `DevKitLoader.unitypackage` from the release page.
2. In Unity, go to **Assets → Import Package → Custom Package** and select the downloaded file.
3. Wait for the import to complete.
4. The **Quick Setup** window will automatically appear. If not, open it via **Tools → DevKit Loader → Quick Setup**.

## Quick Start

### 1. Add your first tool

- Open **Tools → DevKit Loader → Manage List**.
- Fill in the form:
  - **Name** – display name for the tool.
  - **Source Type** – choose the appropriate source (GitHub Release, GitLab Release, Direct URL, Git UPM, Asset Store).
  - **URL** – depending on the type:
    - GitHub: `https://github.com/user/repo` or direct file URL.
    - GitLab: `https://gitlab.com/group/project`.
    - Direct URL: link to `.unitypackage` or `.zip`.
    - Git UPM: Git URL (e.g., `https://github.com/user/repo.git?path=/Assets/...`).
    - Asset Store: store asset URL.
  - **Description** (optional), **License** (optional), **Tags** (optional).
- Click **Add**. The tool appears in the list.

### 2. Install tools

- Open **Tools → DevKit Loader → Quick Setup**.
- Check the boxes next to the tools you want to install.
- Click **Install Selected**.
- Watch the progress bar and final report.

All installed assets will be placed in `Assets/DevKitInstalled/{ToolName}/`.

## Managing the Tool List
- Open **Manage List** to edit, delete, or add new entries.
- Changes are saved automatically in Unity EditorPrefs (global per machine) – the list is shared across all projects.

## Advanced Notes
- For GitHub/GitLab, the plugin fetches the **latest release** and downloads the first `.unitypackage` or `.zip` asset found.
- Direct URL must point to a downloadable file with extension `.unitypackage` or `.zip`.
- UPM packages are added via `PackageManager.Client.Add`. The operation waits for completion (timeout ~unlimited, but cancellable).
- Asset Store only opens the URL in the browser; you need to purchase/download manually.
- Temporary downloaded files are stored in system temp folder and cleaned after successful installation.

## Troubleshooting

- GitHub API returns 404 | Make sure the repository URL is correct and public.
- "No .unitypackage or .zip found" | The latest release does not contain any file with `.unitypackage` or `.zip` extension.
- UPM package fails | Check the Git URL format. For Git dependencies, use `https://...` with `?path=...`.
- Asset Database doesn't refresh | Manually press **Assets → Refresh** (Ctrl+R).

## Third-Party Licenses

This package includes [SharpCompress](https://github.com/adamhathcock/sharpcompress) (MIT License). See `LICENSE-3rd.txt` for details.

## Requirements

- Unity 2021.3 LTS or newer.
- .NET Standard 2.0 compatible.

## Support

For bug reports or feature requests, please open an issue on the GitHub repository (link to your repo).

## License

This plugin is provided as-is under the MIT License (or your chosen license). See `LICENSE.md` for details.
