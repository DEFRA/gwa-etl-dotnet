using System;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Gwa.Etl.Helpers
{
    public class Authorization
    {
        public static string GetAuthHeader(string path, string certificatePath)
        {
            X509Certificate2 certificate = new(certificatePath);
            CmsSigner signer = new(certificate);
            _ = signer.SignedAttributes.Add(new Pkcs9SigningTime());
            byte[] signingData = Encoding.UTF8.GetBytes(path);
            SignedCms signedCms = new(new ContentInfo(signingData), detached: true);
            signedCms.ComputeSignature(signer);
            byte[] signature = signedCms.Encode();
            return $"CMSURL`1 {Convert.ToBase64String(signature)}";
        }
    }
}
