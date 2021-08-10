using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Gwa.Etl.Services
{
    public class AuthorizationHeader
    {
        private readonly CmsSigner signer;

        public AuthorizationHeader(string certificatePath)
        {
            X509Certificate2 certificate = new(certificatePath);
            signer = new(certificate);
        }

        public string GetAuthHeader(string path, DateTime now)
        {
            _ = signer.SignedAttributes.Add(new Pkcs9SigningTime(now));
            byte[] signingData = Encoding.UTF8.GetBytes(path);
            SignedCms signedCms = new(new ContentInfo(signingData), detached: true);
            signedCms.ComputeSignature(signer);
            byte[] signature = signedCms.Encode();
            return $"CMSURL`1 {Convert.ToBase64String(signature)}";
        }
    }
}
