using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Db
{
	public class DesignTimeBodyguardDbContextFactory : IDesignTimeDbContextFactory<BodyguardDbContext>
	{
        public BodyguardDbContext CreateDbContext(string[] args)
        {
            string path = "secret.json";
            if (!File.Exists(path))
            {
                path = Path.Combine(@"D:\Dev\Twitch", path);
            }
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, false)
                .Build();

            var connectionString = configuration.GetConnectionString(configuration.GetConnectionString("DbKey"));
            var builder = new DbContextOptionsBuilder<BodyguardDbContext>();
            builder.UseSqlServer(connectionString);
            return new BodyguardDbContext(builder.Options);
        }
    }
}
