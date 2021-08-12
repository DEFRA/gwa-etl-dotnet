using Gwa.Etl.Services;
using System;
using System.Globalization;
using Xunit;

namespace Gwa.Etl.Tests.Services
{
    public class AuthorizationHeaderTests
    {
        [Fact]
        public void AuthorizationHeaderIsCorrect()
        {
            string certificatePath = "./test-certificate.p12";
            AuthorizationHeader authorizationHeader = new(certificatePath);

            string path = "/api/mdm";
            DateTime now = DateTime.Parse("2020-02-02T12:34:56-0:00", new CultureInfo("en-GB"));

            string authHeader = authorizationHeader.GetAuthHeader(path, now);

            Assert.Equal($"CMSURL`1 MIIFnQYJKoZIhvcNAQcCoIIFjjCCBYoCAQExDTALBglghkgBZQMEAgEwCwYJKoZIhvcNAQcBoIIDajCCA2YwggJOoAMCAQICAQEwDQYJKoZIhvcNAQELBQAwYjEXMBUGA1UEAwwOZ3dhLWV0bC1kb3RuZXQxDjAMBgNVBAoMBWRlZnJhMQswCQYDVQQGEwJHQjEqMCgGCSqGSIb3DQEJARYbZ3dhLWV0bC1kb3RuZXRAZGVmcmEuZ292LnVrMB4XDTIxMDgwOTA4MzAxNFoXDTMxMDgwNzA4MzAxNFowYjEXMBUGA1UEAwwOZ3dhLWV0bC1kb3RuZXQxDjAMBgNVBAoMBWRlZnJhMQswCQYDVQQGEwJHQjEqMCgGCSqGSIb3DQEJARYbZ3dhLWV0bC1kb3RuZXRAZGVmcmEuZ292LnVrMIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA6F3a3Xt2Kj0ZsfVJD/TQ34zLNa3I7XuhcRnMVyQcTuQqYYfQVpCmmxAINP1chUjXHkVyFWn7Yla1hv7mIOl/vqh2X0DVLnvpq2jkOyc0FRDR74ZHka9AFxJ+SOouPTOVBmn4SzJ8fHIrqEVNcwzgvzBB/Q1kQPw3+sGcRcT3AeId7wlkuIDeZ1I2GaAuEm7DwLaRQLrX4/qlgm+Tw3DAXUk0h1bWyR5/KPcMIATa4OIzTUQ8xSgqRbMCIMjB7Ikd6Rz9Rr1pJwtsq8O3YDG3KEl1lK2Qt8eKzPssNHfe6wqxphW6PYo4o2MCdMgFQkxVTz2PB3vjdvXwdcufuKo4NQIDAQABoycwJTALBgNVHQ8EBAMCApQwFgYDVR0lAQH/BAwwCgYIKwYBBQUHAwMwDQYJKoZIhvcNAQELBQADggEBAKkTFfAQMqNrPvpbGAu2kI81rxefUg9Z5MJgmnOhGdTqY79JH70KorbMQl7ISzcqNlRaWuaGcVThTrT7pQ/acmg95U+Njub7a1NLqaaGGCAo0qRFO6pJzZICS4HABi1Bk3//Nt4xj9o2HNAWGAYs6Z03ZREKKzCs/MPu8nNAFFF89/Y8Rp5zYGbdX0r/qtGpBC6wjvimJTwYcPU8g2UXoi/dtfGq/qZRRI/VhCTCAHfKRZCn0mb+eJr4TcquDOPm6LY30NB9MtfzcHrvPIr+KInbmNnhbugljYTdAaKL06zMqFv/7ZGG8BYI2DMm3cG4eslPaG66Nq+T/oI538awChMxggH5MIIB9QIBATBnMGIxFzAVBgNVBAMMDmd3YS1ldGwtZG90bmV0MQ4wDAYDVQQKDAVkZWZyYTELMAkGA1UEBhMCR0IxKjAoBgkqhkiG9w0BCQEWG2d3YS1ldGwtZG90bmV0QGRlZnJhLmdvdi51awIBATALBglghkgBZQMEAgGgaTAYBgkqhkiG9w0BCQMxCwYJKoZIhvcNAQcBMBwGCSqGSIb3DQEJBTEPFw0yMDAyMDIxMjM0NTZaMC8GCSqGSIb3DQEJBDEiBCDCmQ9FcDKvMkdCUTK/asJ97Mw3tYMPURneIglNudtbLjALBgkqhkiG9w0BAQEEggEAOH0ibbkPFYaoYwhBI2xvoFFHBnyn+qaSxh9ApWqxisl5l0/HQxIKf7afLqP4pIoig4DhlVTElUs8EG8AHb7G6ajCyHW3NjuMc+wioVFrtXXIN5X7ZG8XWbvfQcO8AEGf/Dq1JAb2cUF/SDdey4Kp6kNYdTcogpM6wwfh3Zfik4y5Yt/zTUd9k2/bVesxwzQSuHX45BW3sAxgfzn28PPkCZS36dkevgGX6YF8ZOXqij0cm58hZMG4a7hyVv+MAvEMyemJdnycYiNEp94vaBF3fSAe7W43sFT3plDt5u83SjSksS5Hkkb6c7sZXpNKangjF7KmFSAtwLWjcuWwHhi6ig==", authHeader);
        }
    }
}
