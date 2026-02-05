# Copilot Instructions

## General Guidelines
- First general instruction
- Second general instruction

## Code Style
- Use specific formatting rules
- Follow naming conventions

## Project-Specific Rules
- Create the file 'railway.json' in the root directory with the exact content: {
  "build": {
    "builder": "DOCKER"
    },
    "deploy": {
      "port": 80
    }
  } when the user confirms. Notify when 'railway.json' has been created to facilitate the push. This configuration forces Railway to use Dockerfile, which is essential for the deployment process. This is the preferred configuration of the user, as it sets `build.builder` to `DOCKER` and `deploy.port` to `80`.