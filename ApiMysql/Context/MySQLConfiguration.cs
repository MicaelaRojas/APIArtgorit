using ApiMysql.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApiMysql.Context
{
    public class MySQLConfiguration : DbContext
    {
        public string ConnectionString => _connectionString;

        private readonly string _connectionString;

        public MySQLConfiguration(DbContextOptions<MySQLConfiguration> options) : base(options)
        {
        }

        public DbSet<Users> users { get; set; }

        public DbSet<Obras> Obras { get; set; }
    }
}
