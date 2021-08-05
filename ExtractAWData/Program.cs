using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Defra.Gwa.Etl
{
    public static class Program
    {
        public static void Main()
        {
            IHost host = new HostBuilder()
              .ConfigureFunctionsWorkerDefaults()
              .ConfigureServices(s => { _ = s.AddHttpClient(); })
              .ConfigureHostConfiguration(config =>
              {
                  _ = config.AddJsonFile("local.settings.json", true, true);
                  _ = config.AddEnvironmentVariables();
              })
              .Build();

            host.Run();
        }
    }
}
