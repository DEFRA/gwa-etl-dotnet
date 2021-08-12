# Extract AirWatch Data - .NET

> Triggers on a timer, extracting user data from an
> [AirWatch](https://www.vmware.com/products/workspace-one.html) API.

## Detail

The function triggers on a [timer](./function.json) to make a request to the
[AirWatch REST API](https://resources.workspaceone.com/view/zv5cgwjrcv972rd6fmml/en),
specifically the `DevicesV2` (`/devices/search`) endpoint.

All devices will be retrieved (500 per page) and for those devices that are
_not_ an iPad and have a `UserEmailAddress`, both the email and `PhoneNumber`
will be saved in a file which will be uploaded to blob storage in the
[gwa-etl](https://github.com/DEFRA/gwa-etl/) function app.

There is no check on whether the phone number is populated. This is so that the
scenario of a person being registered in AW and without a phone number they
will still be in the system so they will be able to login to the web app and
add additional devices. If only users with email and phone numbers were added
this would not be possible.

There are instances where a user (as determined by `UserEmailAddress`) has
several phone numbers. In these case all phone numbers are included.

## Notes

### Request signed by certificate

There are several ways to authenticate with the AirWatch API:

* Basic Authentication
* Directory Authentication
* Certificate Authentication

Additional information is available in the
[API Guide](https://resources.workspaceone.com/view/zv5cgwjrcv972rd6fmml/en).

For a number of reasons certificate authentication is the preferred mechanism
and has been implemented in this function. Prior to this implementation, basic
authentication had been used and the function responsible was written in
Node.js in the `gwa-etl` app.
However, (for whatever reason) the certificate signing would not
work using [node-forge](https://www.npmjs.com/package/node-forge) on Node V14.
The process worked with other certificates but not the AirWatch issued
certificate. This is the reason this function exists.

[PR#30](https://github.com/DEFRA/gwa-etl/pull/30) removed the basic auth
function from `gwa-etl`.

### Excluding iPads

Devices are typically iPhones or iPads. iPads have been excluded from the
export this is because at the time of decision it made sense to exclude a known
entity that does not wish to have messages sent. The alternative would be to
only include iPhones, however, this has the potential for additional inclusions
to be added at a later time when devices other than iPhones would wish to have
messages sent.

The device codes are available in chapter 17 of the
[AirWatch REST API Guide](https://resources.workspaceone.com/view/zv5cgwjrcv972rd6fmml/en).

### Uploaded file `Content-Type`

The file is saved via the
[`UploadAsync`](https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Storage.Blobs/12.9.1/api/Azure.Storage.Blobs/Azure.Storage.Blobs.BlobClient.html#Azure_Storage_Blobs_BlobClient_UploadAsync_System_IO_Stream_System_Boolean_System_Threading_CancellationToken_)
method on `BlobClient` available in the
[Azure Storage Blobs](https://azuresdkdocs.blob.core.windows.net/$web/dotnet/Azure.Storage.Blobs/12.9.1/index.html)
.NET SDK.

Whilst it is possible to set the `Content-Type` of the file doing so does not
perform the upsert operation, therefore, at the expense of correct content type
the simple option has been chosen.
