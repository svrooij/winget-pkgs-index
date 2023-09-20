# winget-pkgs-index

Open-source package index of [Windows Package Manager repository](https://github.com/microsoft/winget-pkgs)

## Why?

[WingetIntune](https://github.com/svrooij/wingetintune) uses winget to search the correct installer to publish to Intune. It had a dependency on winget (thus making it platform dependent) and it was slow. This project is a simple index of all packages in the winget repository. It is updated every 6 hours (to be determined) and will be updated at regular intervals through a github action.

## Usage

Use this uri as index for [WingetIntune](https://github.com/svrooij/wingetintune)

```Shell
https://github.com/svrooij/winget-pkgs-index/raw/main/index.json
```
