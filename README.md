# PepperDash Essentials Enlighted Lighting Plugin (c) 2021

## License

Provided under MIT license

## Overview

This repo contains a plugin for use with [PepperDash Essentials](https://github.com/PepperDash/Essentials). This plugin enables Essentials to communicate with and control Enlighting Lighting Energy Manager (EM) lighting scenes via HTTPS.

## Example Config Object

```json
{
        "key": "lighting-1",
        "uid": 1,
        "name": "Enlighted Energy Manager (EM)",
        "type": "enlightedlighting",
        "group": "plugin",
        "parentDeviceKey": "processor",
        "properties": {
          "control": {
            "method": "https",
            "tcpSshProperties": {
              "address": "lighting.server.com",
              "port": 443
            }
          },
          "pollTimeMs": 60000,
          "warningTimeoutMs": 120000,
          "errorTimeoutMs": 180000,
          "apiKey": "97987asdfasdf923098248702938423423lkjwe",
          "apiKeyUsername": "crestron",
          "headerUsesApiKey": true,
          "virtualSwitchIdentifier": "10",
          "scenes": {
            "scene1": {
              "sceneId": 31
            },
            "scene2": {
              "sceneId": 32
            },
            "scene3": {
              "sceneId": 33
            },
            "scene4": {
              "sceneId": 34
            },
            "scene5": {
              "sceneId": 35
            },
            "scene6": {
              "sceneId": 26
            }
          }
        }
      }
```
For more configuration information, see the [PepperDash Essentials wiki](https://github.com/PepperDash/Essentials/wiki).

## Github Actions

This repo contains two Github Action workflows that will build this project automatically. Modify the SOLUTION_PATH and SOLUTION_FILE environment variables as needed. Any branches named `feature/*`, `release/*`, `hotfix/*` or `development` will automatically be built with the action and create a release in the repository with a version number based on the latest release on the master branch. If there are no releases yet, the version number will be 0.0.1. The version number will be modified based on what branch triggered the build:

- `feature` branch builds will be tagged with an `alpha` descriptor, with the Action run appended: `0.0.1-alpha-1`
- `development` branch builds will be tagged with a `beta` descriptor, with the Action run appended: `0.0.1-beta-2`
- `release` branches will be tagged with an `rc` descriptor, with the Action run appended: `0.0.1-rc-3`
- `hotfix` branch builds will be tagged with a `hotfix` descriptor, with the Action run appended: `0.0.1-hotfix-4`

Builds on the `Main` branch will ONLY be triggered by manually creating a release using the web interface in the repository. They will be versioned with the tag that is created when the release is created. The tags MUST take the form `major.minor.revision` to be compatible with the build process. A tag like `v0.1.0-alpha` is NOT compatabile and may result in the build process failing.

If you have any questions about the action, contact Andrew Welker or Neil Dorin.
