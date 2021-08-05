# GWA ETL .NET

[![Build and Deploy](https://github.com/DEFRA/gwa-etl-dotnet/actions/workflows/build-and-deploy.yml/badge.svg)](https://github.com/DEFRA/gwa-etl-dotnet/actions/workflows/build-and-deploy.yml)
[![Coverage](https://sonarcloud.io/api/project_badges/measure?project=DEFRA_gwa-etl-dotnet&metric=coverage)](https://sonarcloud.io/dashboard?id=DEFRA_gwa-etl-dotnet)
[![Technical Debt](https://sonarcloud.io/api/project_badges/measure?project=DEFRA_gwa-etl-dotnet&metric=sqale_index)](https://sonarcloud.io/dashboard?id=DEFRA_gwa-etl-dotnet)
[![Maintainability Rating](https://sonarcloud.io/api/project_badges/measure?project=DEFRA_gwa-etl-dotnet&metric=sqale_rating)](https://sonarcloud.io/dashboard?id=DEFRA_gwa-etl-dotnet)\
[![Security Rating](https://sonarcloud.io/api/project_badges/measure?project=DEFRA_gwa-etl-dotnet&metric=security_rating)](https://sonarcloud.io/dashboard?id=DEFRA_gwa-etl-dotnet)
[![Vulnerabilities](https://sonarcloud.io/api/project_badges/measure?project=DEFRA_gwa-etl-dotnet&metric=vulnerabilities)](https://sonarcloud.io/dashboard?id=DEFRA_gwa-etl-dotnet)

> An [Azure Function app](https://azure.microsoft.com/en-gb/services/functions/)
> for retrieving data from AirWatch API.

The app extracts data from
[AirWatch](https://www.vmware.com/products/workspace-one.html) and uploads it
to blob storage used by [gwa-etl](https://github.com/DEFRA/gwa-etl) as part of
a data extract pipeline.

## Functions

The app is made up of a single function `ExtractAWData`. The function is
running on .NET 5.0 using the
[C# isolated process](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide)
function.

## Development

A good place to start to get an overview for function development is from the
page linked above where additional documentation can be found. This includes
how to get setup and started using
[Visual Studio Code](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-developer-howtos?pivots=development-environment-vscode)
and
[CLI tools](https://docs.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-developer-howtos?pivots=development-environment-cli).

The documentation within this repo assumes `CLI tools` are being used and
the setup has been completed, specifically for
[Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli).

## Running Locally

To start the function app run navigate into the directory and start the
function i.e.

```bash
cd ExtractAWData
func start
```

Once the function is running it can be triggered by running
[run-function-locally](./scripts/run-function-locally)
and passing the name of the function i.e.
`./scripts/run-function-locally ExtractAWData`.

`local.settings.json` will need to contain the relevant secrets for
the function app to work correctly. Along with the `.p12` certificate file
with no password must also be available in the repo's root directory.

### Pre-requisites

The app uses Azure Storage blobs. When working locally
[Azurite](https://github.com/Azure/Azurite) can be used to emulate storage.
Follow the
[instructions](https://docs.microsoft.com/en-us/azure/storage/common/storage-use-azurite)
for your preferred installation option.

The app uses `local.settings.json` for local development.
[.local.settings.json](.local.settings.json) can be used as the
basis as it contains all required env vars with the exception of secrets which
have been removed. The connection string for Azurite is included as this is not
a secret.

## License

THIS INFORMATION IS LICENSED UNDER THE CONDITIONS OF THE OPEN GOVERNMENT
LICENCE found at:

<http://www.nationalarchives.gov.uk/doc/open-government-licence/version/3>

The following attribution statement MUST be cited in your products and
applications when using this information.

> Contains public sector information licensed under the Open Government license
> v3

### About the license

The Open Government Licence (OGL) was developed by the Controller of Her
Majesty's Stationery Office (HMSO) to enable information providers in the
public sector to license the use and re-use of their information under a common
open licence.

It is designed to encourage use and re-use of information freely and flexibly,
with only a few conditions.
