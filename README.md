# MiniGame Toolchain for Tuanjie Engine — Free Edition

<p align="center">
  <strong>A one-stop development toolchain for WeChat MiniGame on Tuanjie Engine</strong>
</p>

<p align="center">
  <img alt="License: Free Edition" src="https://img.shields.io/badge/License-Free%20Edition-blue.svg">
  <img alt="Engine: Tuanjie 2022.3" src="https://img.shields.io/badge/Engine-Tuanjie%202022.3-blueviolet.svg">
  <img alt="Platform: WeChat MiniGame" src="https://img.shields.io/badge/Platform-WeChat%20MiniGame-green.svg">
</p>

---

## Overview / 简介

**MiniGame Toolchain** is a suite of Unity/Tuanjie Editor plugins for WeChat MiniGame development. This branch (`upm`) hosts the **free UPM packages** of the toolchain.

**MiniGame Toolchain** 是一套面向团结引擎（Tuanjie）微信小游戏开发的编辑器插件。本 `upm` 分支用于发布免费版 UPM 包。

The full commercial source code, including paid features such as `Builder Pro`, is maintained in a private repository and distributed through a licensing system.
完整商业源码（含付费产品 `Builder Pro`）保留在私有主仓库，通过授权系统分发。

## Included Free Packages / 免费包清单

- **Version / 版本**: 0.1.4
- **Published / 发布日期**: 2026-07-18

- `com.hiyodo.minigametoolchain.build-optimizer`
- `com.hiyodo.minigametoolchain.core`
- `com.hiyodo.minigametoolchain.performance-suite`
- `com.hiyodo.minigametoolchain.starter-kit`

## Installation / 安装

### Via Git URL (UPM)

Open **Window → Package Manager**, click **+ → Add package from git URL...**, and enter:

```
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.hiyodo.minigametoolchain.core#upm
```

Repeat for each package you need. Make sure to install `com.hiyodo.minigametoolchain.core` first, since the other packages depend on it.

打开 **Window → Package Manager**，点击 **+ → Add package from git URL...**，输入上述地址。按需安装其他包，注意必须先安装 `com.hiyodo.minigametoolchain.core`，其他包依赖它。

Available package paths / 可用包路径：

```
Packages/com.hiyodo.minigametoolchain.core
Packages/com.hiyodo.minigametoolchain.build-optimizer
Packages/com.hiyodo.minigametoolchain.performance-suite
Packages/com.hiyodo.minigametoolchain.starter-kit
```

### Via Gitee (when synced / 同步完成后)

```
https://gitee.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.hiyodo.minigametoolchain.core#upm
```

### Via Local Disk / 本地安装

Download `free-upm-bundle-0.1.4.zip` from Releases, extract it, and add each package folder via **Add package from disk...**.

从 Releases 下载 `free-upm-bundle-0.1.4.zip`，解压后通过 **Add package from disk...** 逐个添加。

## License / 许可

This free edition is released under a custom free license. See the `LICENSE.md` file inside each package for details.

本免费版采用自定义免费许可协议，详见每个包目录下的 `LICENSE.md`。商业用途或团队部署需购买商业许可证。
