using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataBase;
using System.IO;
using Microsoft.EntityFrameworkCore;

namespace Database.DAL
{
    public class DataBaseContext : Microsoft.EntityFrameworkCore.DbContext
    {

        public DataBaseContext()
        {
            Database.EnsureCreated();
        }

        public DbSet<QuestionAnswer> QA { get; set; }

        public string DbPath { get; }

        protected override void OnConfiguring(DbContextOptionsBuilder o)
           => o.UseSqlite($"Data Source=\"C:\\Users\\gameh\\source\\repos\\Solution1\\Database\\qa.db\"");
    }
}
