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
                  "address": "0.0.0.0",
                  "port": 443
              }
          },
          "pollTimeMs": 60000,
          "warningTimeoutMs": 120000,
          "errorTimeoutMs": 180000,
          "apiKey": "74deac09af03dab85468b91ab7c12273cfc37dfc",
          "apiKeyUsername": "crestron",
          "presets": {
              "comment": {
              "name": "Touch panel name given for each preset",
              "path": "REST API applyScene path",
              "pathExample": "/ems/api/org/switch/v1/op/applyScene/{switch_id}/{scene_id}?time=60"
              },
              "preset1": {
                  "name": "Bright",
                  "path": "/ems/api/org/switch/v1/op/applyScene/11/1?time=0"
              },
              "preset2": {
                  "name": "Medium",
                  "path": "/ems/api/org/switch/v1/op/applyScene/11/2?time=0"
              },
              "preset3": {
                  "name": "Mid-Low",
                  "path": "/ems/api/org/switch/v1/op/applyScene/11/3?time=0"
              },
              "preset4": {
                  "name": "Low",
                  "path": "/ems/api/org/switch/v1/op/applyScene/11/4?time=0"
              },
              "preset5": {
                  "name": "Off",
                  "path": "/ems/api/org/switch/v1/op/applyScene/11/5?time=0"
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
