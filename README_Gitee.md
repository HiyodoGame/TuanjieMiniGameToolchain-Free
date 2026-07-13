# MiniGame Toolchain for Tuanjie Engine（免费版）

<p align="center">
  <strong>专为团结引擎打造的微信小游戏一站式开发工具链</strong>
</p>

<p align="center">
  <img alt="License: 免费版" src="https://img.shields.io/badge/License-免费版-blue.svg">
  <img alt="Engine: Tuanjie 2022.3" src="https://img.shields.io/badge/Engine-Tuanjie%202022.3-blueviolet.svg">
  <img alt="Platform: 微信小游戏" src="https://img.shields.io/badge/Platform-微信小游戏-green.svg">
</p>

<p align="center">
  <a href="README.md">English</a> |
  <a href="#简介">简介</a> |
  <a href="#免费版包含的包">包含的包</a> |
  <a href="#安装">安装</a> |
  <a href="#快速开始">快速开始</a>
</p>

---

## 简介

**MiniGame Toolchain** 是一套面向团结引擎（Tuanjie）微信小游戏开发的编辑器插件。本仓库为 **免费版发布仓库**，包含开源核心框架与 4 个可免费使用的 UPM 包；完整商业源码（含付费产品 `Builder Pro`）保留在私有主仓库，通过授权系统分发。

## 免费版包含的包

| 包名 | 说明 | UPM 路径 |
|---|---|---|
| `com.minigame.core` | 核心框架：诊断引擎、规则引擎、微信桥接、编辑器 UI | `Packages/com.minigame.core` |
| `com.minigame.build-optimizer` | 构建体积诊断与优化工具 | `Packages/com.minigame.build-optimizer` |
| `com.minigame.performance-suite` | 轻量级运行时性能监控 | `Packages/com.minigame.performance-suite` |
| `com.minigame.starter-kit` | 项目启动模板与核心系统 | `Packages/com.minigame.starter-kit` |

> **注意：** `com.minigame.builder-pro` 为付费产品，不在本仓库分发。

## 安装

### 通过 Git URL 安装（推荐）

打开 **Window → Package Manager**，点击 **+ → Add package from git URL...**，输入：

```
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.core#upm
```

按需安装其他包，注意 `com.minigame.core` 必须先安装，其他包依赖它。

其他包的 Git URL：

```
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.build-optimizer#upm
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.performance-suite#upm
https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.starter-kit#upm
```

### 本地安装

从 [Releases](https://github.com/HiyodoGame/TuanjieMiniGameToolchain-Free/releases) 下载 `free-upm-bundle.zip`，解压后通过 **Add package from disk...** 逐个添加。

### Gitee 安装（同步完成后）

```
https://gitee.com/HiyodoGame/TuanjieMiniGameToolchain-Free.git?path=Packages/com.minigame.core#upm
```

## 快速开始

安装完成后，通过菜单打开工具，例如：

```
MiniGame → Build Optimizer
```

点击“完整扫描”即可分析项目并查看诊断报告。

## 版本

当前免费版版本：**0.1.4**

`upm` 分支始终存放最新发布的包；默认 `main` 分支仅用于仓库说明文档。

## 许可证

本免费版采用自定义免费许可协议，详见每个包目录下的 `LICENSE.md`。

商业用途或团队部署需购买商业许可证。

---

<p align="center">Made with ❤️ for 微信小游戏开发者</p>
