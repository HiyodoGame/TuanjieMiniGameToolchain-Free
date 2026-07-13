# MiniGame Toolchain for Tuanjie Engine — Free Edition

<p align="center">
  <strong>A one-stop development toolchain for WeChat MiniGame on Tuanjie Engine</strong>
</p>

<p align="center">
  <img alt="License: MIT" src="https://img.shields.io/badge/License-Free%20Edition-blue.svg">
  <img alt="Engine: Tuanjie 2022.3" src="https://img.shields.io/badge/Engine-Tuanjie%202022.3-blueviolet.svg">
  <img alt="Platform: WeChat MiniGame" src="https://img.shields.io/badge/Platform-WeChat%20MiniGame-green.svg">
</p>

<p align="center">
  <a href="README_Gitee.md">中文版</a> |
  <a href="#overview">Overview</a> |
  <a href="#free-packages">Packages</a> |
  <a href="#installation">Installation</a> |
  <a href="#quick-start">Quick Start</a>
</p>

---

## Overview

**MiniGame Toolchain** is a suite of Unity/Tuanjie Editor plugins for WeChat MiniGame development. This is the **free distribution repository**, containing the open-source core framework and four free UPM packages.

The full commercial source code, including paid features such as `Builder Pro`, is maintained in a private repository and distributed through a licensing system.

## Free Packages

| Package | Description | UPM Path |
|---|---|---|
| `com.minigame.core` | Core framework: diagnostic engine, rule engine, WeChat bridge, editor UI | `Packages/com.minigame.core` |
| `com.minigame.build-optimizer` | Build-size diagnostics and optimization tools | `Packages/com.minigame.build-optimizer` |
| `com.minigame.performance-suite` | Lightweight runtime performance monitoring | `Packages/com.minigame.performance-suite` |
| `com.minigame.starter-kit` | Project starter templates and core systems | `Packages/com.minigame.starter-kit` |

> **Note:** `com.minigame.builder-pro` is a paid product and is **not** distributed in this repository.

## Installation

### Via Git URL (UPM)

Open **Window → Package Manager**, click **+ → Add package from git URL...**, and enter:

```
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.core#upm
```

Repeat for each package you need. Make sure to install `com.minigame.core` first, since the other packages depend on it.

Available Git URLs:

```
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.core#upm
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.build-optimizer#upm
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.performance-suite#upm
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.starter-kit#upm
```

### Via Local Disk

Download `free-upm-bundle.zip` from the [Releases](https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free/releases) page, extract it, and add each package folder via **Add package from disk...**.

### Via Gitee (when synced)

```
https://gitee.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.core#upm
```

## Quick Start

After installation, open the tool from the menu, for example:

```
MiniGame → Build Optimizer
```

Run a full scan to analyze your project and view the diagnostic report.

## Version

Current free release: **0.1.2**

The `upm` branch always contains the latest published packages. The default `main` branch only holds repository documentation.

## License

This free edition is released under a custom free license. See the `LICENSE.md` file inside each package for details.

Commercial use or deployment in a team requires purchasing a commercial license.

---

<p align="center">Made with ❤️ for WeChat MiniGame developers</p>
