using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SentimentAnalysis
{
    public class DesignTimeMLDbContextFactory : IDesignTimeDbContextFactory<MLDbContext>
    {
        public MLDbContext CreateDbContext(string[] args)
        {
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            string path = "secret.json";
            IConfigurationRoot configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(path, false)
                .Build();

            var connectionString = configuration.GetConnectionString("MLDbKey");
            var builder = new DbContextOptionsBuilder<MLDbContext>();
            builder.UseSqlServer(connectionString);
            return new MLDbContext(builder.Options);
        }
    }
}
