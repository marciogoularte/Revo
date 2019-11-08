﻿using System.Collections.Generic;

namespace Revo.Infrastructure.DataAccess.Migrations
{
    public interface IDatabaseMigrationRegistry
    {
        IReadOnlyCollection<IDatabaseMigration> Migrations { get; }

        IEnumerable<string> GetAvailableModules();
        void AddMigration(IDatabaseMigration migration);
        void ValidateMigrations();
    }
}