# Scripts

> A collection of useful scripts.

*Note:* Scripts are targeted to \*nix environments and use
[sh](https://en.wikipedia.org/wiki/Bourne_shell) to run.

## [Run function locally](./run-function-locally)

Run the function locally by sending an HTTP POST request. The function
app needs to be running.

## [Run tests and generate coverage](./run-tests-with-coverage)

Run the tests and generate code coverage.

## [Run GitHub Super Linter](./run-github-super-linter)

Run `./run-github-super-linter` to run GitHub Super Linter on the repo. This is
run during the CI build to ensure all files adhere to the linting rules.

## [Update app settings](./update-app-settings)

Run `./update-app-settings` from the repo's root directory to update the app
settings for the function app deployed in Azure. You must be logged in for this
to work - `az login`.

There must be a file in the repo's root directory containing the app settings,
use [.app-settings.json](../.app-settings.json) as a basis. As with
`.local.settings.json` any sensitive values have been removed.
