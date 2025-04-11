# winget-pkgs-index

Open-source package index of [Windows Package Manager repository](https://github.com/microsoft/winget-pkgs)

Documentation: [wintuner.app/docs/related/winget-package-index/](https://wintuner.app/docs/related/winget-package-index/)

## Why?

[WingetIntune](https://github.com/svrooij/wingetintune) uses winget to search the correct installer to publish to Intune. It had a dependency on winget (thus making it platform dependent) and it was slow. This project is a simple index of all packages in the winget repository. It is updated every 4 hours through a [github action](https://github.com/svrooij/winget-pkgs-index/actions/workflows/refresh.yml).

## Usage

| Kind | Online link | Download URL |
| ---- | ----------- | ------------ |
| CSV v2 | [index.v2.csv](https://github.com/svrooij/winget-pkgs-index/blob/main/index.v2.csv) | `https://github.com/svrooij/winget-pkgs-index/raw/main/index.v2.csv` |
| JSON v2 | [index.json](https://github.com/svrooij/winget-pkgs-index/blob/main/index.v2.json) | `https://github.com/svrooij/winget-pkgs-index/raw/main/index.v2.json` |
| CSV | [index.csv](https://github.com/svrooij/winget-pkgs-index/blob/main/index.csv) | `https://github.com/svrooij/winget-pkgs-index/raw/main/index.csv` |
| JSON | [index.json](https://github.com/svrooij/winget-pkgs-index/blob/main/index.json) | `https://github.com/svrooij/winget-pkgs-index/raw/main/index.json` |

## Version wrong?

The 9th of April 2025, Microsoft removed the old format source file from their CDN. The new source file (which is better!) has an [issue](https://github.com/microsoft/winget-cli/issues/4928) with the versions like `123.0` or `123.10` or `12.0.0.0` the zeros are removed.

Want to install any of the affected apps, and using this index? Start complaining in [this issue](https://github.com/microsoft/winget-cli/issues/4928). And sorry for the faulty versions, there is nothing I can do!
