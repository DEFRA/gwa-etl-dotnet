using Microsoft.Extensions.Hosting;

namespace Defra.Gwa.Etl
{
    public class Program
    {
        public static void Main()
        {
            IHost host = new HostBuilder().ConfigureFunctionsWorkerDefaults().Build();

            host.Run();
        }
    }
}
