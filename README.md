# Mane Text

Text rendering component for your Unity games. Creates a mesh for your label and renders it with a simple shader. Built on top of the Unity dynamic font atlas.

It was initially developed well before `TextMesh Pro` existed, and before the `UI Canvas` system. It has now been reworked for `Unity 6` and updated with a custom `UI Toolkit` editor to match modern requirements.

Treat this as a legacy tool: useful for learning how to render text from fonts, *or as a base if you need a custom renderer with full control*.

The package is supported starting from **Unity 6.0.81** (6000.0.81f1).

## Features

- Dynamic text rendering from any font supported by Unity.
- Shared materials for labels with the same style, to reduce draw calls (font, color, and styles must match).
- Basic `outline` and `shadow` implemented with extra geometry, not with shaders.
- Full control over character alignment and spacing.
- NUnit coverage for wrapping and public property setters.

## Look

![](https://raw.githubusercontent.com/wiki/ManeFunction/ManeTools-Text/main.png)

## Installation

I recommend installing this package with the `OpenUPM` CLI. It keeps dependencies and updates easy to manage. If you cannot use `OpenUPM`, download the package and place it anywhere in your Unity project.

Setting up `OpenUPM` for the first time takes a few minutes, but it is worth it. `OpenUPM` is the usual registry for open-source Unity packages and works with Unity’s Package Manager (dependency resolution and updates included).

On Windows, I recommend `Git Bash` (`MINGW`) for CLI work: it is a Unix-like shell, and it is often already installed.

1. Install `OpenUPM` (skip this if you already have it):
   - If you do not have `npm`, install [Node.js](https://nodejs.org) (or on macOS: `brew install node`).
   - In a terminal, run: `npm install -g openupm-cli`.
   - You can then install packages from the `OpenUPM` registry with no extra Unity setup.
1. Install this package:
   - Open a terminal in your Unity project folder: `cd /path/to/your/project`.
   - Run: `openupm add com.manefunction.unity-text`.
   - Switch back to Unity and wait for the package to finish importing.

## Why Preview?

Despite the fact that the code itself is not new, splitting one package into a few - plus a pile of refactoring and migration to the `UI Toolkit` - is a great way to invent fresh bugs.

Overall it should still be safe for commercial work (the original ManeTools already ships in a few dozen live projects), but let's give this split a little time to surface whatever I missed. Use at your own risk, I suppose.

## Repository info

This repo follows the [Conventional Commits](https://www.conventionalcommits.org/) specification.

[![GitHub Sponsors](https://img.shields.io/github/sponsors/ManeFunction?label=Sponsor&logo=GitHubSponsors&style=flat)](https://github.com/sponsors/ManeFunction)
[![openupm](https://img.shields.io/npm/v/com.manefunction.unity-text?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.manefunction.unity-text/)
[![openupm](https://img.shields.io/badge/dynamic/json?color=brightgreen&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.manefunction.unity-text)](https://openupm.com/packages/com.manefunction.unity-text/)
