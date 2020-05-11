using System.Linq;
using EFCoreSQLiteFTS.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCoreSQLiteFTS.DataLayer
{
    public static class InitDb
    {
        public static void Start()
        {
            EFServiceProvider.RunInContext(context =>
            {
                context.Database.Migrate();

                createFtsTables(context);

                seedDb(context);
            });
        }

        private static void seedDb(ApplicationDbContext context)
        {
            if (!context.Chapters.Any())
            {
                var user1 = context.Users.Add(new User { Name = "Test User" });
                context.Chapters.Add(new Chapter
                {
                    Title = "Learn SQlite FTS5",
                    Text = "This tutorial teaches you how to perform full-text search in SQLite using FTS5",
                    User = user1.Entity
                });
                context.Chapters.Add(new Chapter
                {
                    Title = "Advanced SQlite Full-text Search",
                    Text = "Show you some advanced techniques in SQLite full-text searching",
                    User = user1.Entity
                });
                context.Chapters.Add(new Chapter
                {
                    Title = "SQLite Tutorial",
                    Text = "Help you learn SQLite quickly and effectively",
                    User = user1.Entity
                });
                context.Chapters.Add(new Chapter
                {
                    Title = "Handle markup in text",
                    Text = "<p>Isn't this <font face=\"Comic Sans\">funny</font>?",
                    User = user1.Entity
                });

                // The unicode61 tokenizer is available in SQLite version 3.7.13. This tokenizer supports "full unicode case folding" and "recognizes unicode space and punctuation characters."
                // Unicode characters are handled like 'normal' letters, so you can use them in FTS data and search terms. (Prefix searches should work, too.)
                context.Chapters.Add(new Chapter
                {
                    Title = "آزمايش متن فارسي",
                    Text = "براي نمونه تهيه شده‌است",
                    User = user1.Entity
                });

                context.Chapters.Add(new Chapter
                {
                    Title = "Exclude test 1",
                    Text = "in the years 2018-2019 something happened.",
                    User = user1.Entity
                });
                context.Chapters.Add(new Chapter
                {
                    Title = "Exclude test 2",
                    Text = "It was 2018 and then it was 2019",
                    User = user1.Entity
                });

                context.SaveChanges();
            }
        }

        private static void createFtsTables(ApplicationDbContext context)
        {
            // For SQLite FTS
            // Note: This can be added to the `protected override void Up(MigrationBuilder migrationBuilder)` method too.
            context.Database.ExecuteSqlRaw(@"CREATE VIRTUAL TABLE IF NOT EXISTS ""Chapters_FTS""
                                    USING fts5(""Text"", ""Title"", content=""Chapters"", content_rowid=""Id"");");

            // 'SQLite Error 1: 'no such module: spellfix1'.' --> must be loaded ...
            // EditCost for unicode support
            context.Database.ExecuteSqlRaw("CREATE VIRTUAL TABLE IF NOT EXISTS Chapters_FTS_Vocab USING fts5vocab('Chapters_FTS', 'row');");
            context.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS Chapters_FTS_SpellFix_EditCost(iLang INT, cFrom TEXT, cTo TEXT, iCost INT);");
            context.Database.ExecuteSqlRaw("CREATE VIRTUAL TABLE IF NOT EXISTS Chapters_FTS_SpellFix USING spellfix1(edit_cost_table=Chapters_FTS_SpellFix_EditCost);");
        }
    }
}