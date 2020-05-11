using System;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFCoreSQLiteFTS.DataLayer;
using Microsoft.Data.Sqlite;

namespace EFCoreSQLiteFTS
{
    public static class EFServiceProvider
    {
        private static readonly Lazy<IServiceProvider> _serviceProviderBuilder =
                new Lazy<IServiceProvider>(getServiceProvider, LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// A lazy loaded thread-safe singleton
        /// </summary>
        public static IServiceProvider Instance { get; } = _serviceProviderBuilder.Value;

        public static T GetRequiredService<T>()
        {
            return Instance.GetRequiredService<T>();
        }

        public static void RunInContext(Action<ApplicationDbContext> action)
        {
            using var serviceScope = GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            action(context);
        }

        public static async Task RunInContextAsync(Func<ApplicationDbContext, Task> action)
        {
            using var serviceScope = GetRequiredService<IServiceScopeFactory>().CreateScope();
            using var context = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await action(context);
        }

        private static IServiceProvider getServiceProvider()
        {
            var services = new ServiceCollection();
            services.AddOptions();

            services.AddLogging(cfg => cfg.AddConsole().AddDebug());

            var basePath = Directory.GetCurrentDirectory();
            var contentRootPath = basePath.Split(new[] { "\\bin\\" }, StringSplitOptions.RemoveEmptyEntries)[0];
            Console.WriteLine($"Using `{contentRootPath}` as the ContentRootPath");
            var configuration = new ConfigurationBuilder()
                                .SetBasePath(contentRootPath)
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .Build();
            services.AddSingleton(_ => configuration);

            LoadSqliteExtensions.AddToSystemPath(Path.Combine(contentRootPath, "SqliteExtensions"));
            var connectionString = GetConnectionString(contentRootPath, configuration);

            services.AddDbContext<ApplicationDbContext>((serviceProvider, optionsBuilder) =>
                {
                    var connection = new SqliteConnection(connectionString);
                    connection.Open();

                    connection.LoadExtension("spellfix1");

                    //connection.CreateCollation("PersianCollationNoCase", (x, y) => string.Compare(x, y, ignoreCase: true));

                    // Passing in an already open connection will keep the connection open between requests.
                    optionsBuilder.UseSqlite(connection);

                    /*optionsBuilder
                        .UseSqlite(connectionString);*/
                });

            return services.BuildServiceProvider();
        }

        public static string GetConnectionString(string contentRootPath, IConfigurationRoot configuration)
        {
            var connectionString = configuration["ConnectionStrings:ApplicationDbContextConnection"];
            if (connectionString.Contains("%CONTENTROOTPATH%"))
            {
                connectionString = connectionString.Replace("%CONTENTROOTPATH%", contentRootPath);
            }
            Console.WriteLine($"Using {connectionString}");
            return connectionString;
        }
    }
}