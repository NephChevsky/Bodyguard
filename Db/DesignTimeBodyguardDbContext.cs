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
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            string path = "secret.json";
            if (!File.Exists(path))
            {
                path = Path.Combine(@"D:\Dev\Bodyguard", path);
            }
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, false, false)
                .Build();

            var connectionString = configuration.GetConnectionString("DbKey");
            var builder = new DbContextOptionsBuilder<BodyguardDbContext>();
            builder.UseSqlServer(connectionString);
            return new BodyguardDbContext(builder.Options);
        }
    }
}
