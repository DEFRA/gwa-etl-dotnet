using Gwa.Etl.Services;
using System;
using Xunit;

namespace Gwa.Etl.Tests.Services
{
    public class AuthorizationHeaderTests
    {
        [Fact]
        public void UnsuccessfulStatusCodeThrows()
        {
            string certificatePath = "";
            AuthorizationHeader authorizationHeader = new(certificatePath);

            string path = "/api/mdm";
            DateTime now = DateTime.Now;

            string authHeader = authorizationHeader.GetAuthHeader(path, now);

            Assert.Equal($"CMSURL`1 {1}", authHeader);
        }
    }
}
