using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Newtonsoft.Json;
using SQLEFTableNotification.Entity.Context;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SQLEFTableNotification.Entity.Context
{
    public static class DbContextExtension
    {

        public static bool AllMigrationsApplied(this DbContext context)
        {
            var applied = context.GetService<IHistoryRepository>()
                .GetAppliedMigrations()
                .Select(m => m.MigrationId);

            var total = context.GetService<IMigrationsAssembly>()
                .Migrations
                .Select(m => m.Key);

            return !total.Except(applied).Any();
            //return false;
        }

        public static void EnsureSeeded(this SQLEFTableNotificationContext context)
        {

            if (!context.Accounts.Any())
            {
                var accounts = JsonConvert.DeserializeObject<List<Account>>(File.ReadAllText("seed" + Path.DirectorySeparatorChar + "accounts.json"));
                context.AddRange(accounts);
                context.SaveChanges();
            }

            //Ensure we have some status
            if (!context.Users.Any())
            {
                var users = JsonConvert.DeserializeObject<List<User>>(File.ReadAllText(@"seed" + Path.DirectorySeparatorChar + "users.json"));
                context.AddRange(users);
                context.SaveChanges();
            }
        }

    }
}
