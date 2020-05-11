using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using EFCoreSQLiteFTS.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCoreSQLiteFTS.DataLayer
{
    public static class FtsNormalizer
    {
        private static readonly Regex _htmlRegex = new Regex("<[^>]*>", RegexOptions.Compiled);

        public static string NormalizeText(this string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            // Remove html tags
            text = _htmlRegex.Replace(text, string.Empty);

            // TODO: add other normalizers here, such as `remove diacritics`, `fix Persian Ye-Ke` and so on ...

            return text;
        }
    }

    public static class UpdateFtsTriggers
    {
        public static void UpdateChapterFTS(
            this DbContext context,
            List<(EntityState State, Chapter NewEntity, Chapter OldEntity)> changedChapters)
        {
            var database = context.Database;

            try
            {
                database.BeginTransaction(IsolationLevel.ReadCommitted);
                var isUpdated = false;

                foreach (var (State, NewEntity, OldEntity) in changedChapters)
                {
                    var chapterNew = NewEntity;
                    var chapterOld = OldEntity;

                    var normalizedNewText = chapterNew.Text.NormalizeText();
                    var normalizedOldText = chapterOld.Text.NormalizeText();
                    var normalizedNewTitle = chapterNew.Title.NormalizeText();
                    var normalizedOldTitle = chapterOld.Title.NormalizeText();
                    switch (State)
                    {
                        case EntityState.Added:
                            if (shouldSkipAddedChapter(chapterNew))
                            {
                                continue;
                            }
                            database.ExecuteSqlRaw("INSERT INTO Chapters_FTS(rowid, Text, Title) values({0}, {1}, {2});",
                                    chapterNew.Id, normalizedNewText, normalizedNewTitle);
                            isUpdated = true;
                            break;
                        case EntityState.Modified:
                            if (shouldSkipModifiedChapter(chapterNew, chapterOld))
                            {
                                continue;
                            }
                            // This format is important! Otherwise we will get `SQLite Error 11: 'database disk image is malformed'.` error!
                            database.ExecuteSqlRaw(@"INSERT INTO Chapters_FTS(Chapters_FTS, rowid, Text, Title)
                                                        VALUES('delete', {0}, {1}, {2}); ",
                                                        chapterOld.Id, normalizedOldText, normalizedOldTitle);
                            database.ExecuteSqlRaw("INSERT INTO Chapters_FTS(rowid, Text, Title) values({0}, {1}, {2});",
                                    chapterNew.Id, normalizedNewText, normalizedNewTitle);
                            isUpdated = true;
                            break;
                        case EntityState.Deleted:
                            // This format is important! Otherwise we will get `SQLite Error 11: 'database disk image is malformed'.` error!
                            database.ExecuteSqlRaw(@"INSERT INTO Chapters_FTS(Chapters_FTS, rowid, Text, Title)
                                                        VALUES('delete', {0}, {1}, {2}); ",
                                    chapterOld.Id, normalizedOldText, normalizedOldTitle);
                            isUpdated = true;
                            break;
                    }
                }

                if (isUpdated)
                {
                    // for spellfix1
                    database.ExecuteSqlRaw(@"INSERT INTO Chapters_FTS_SpellFix(word, rank)
                                            SELECT term, cnt FROM Chapters_FTS_Vocab
                                            WHERE term not in (SELECT word from Chapters_FTS_SpellFix_vocab)");
                    database.ExecuteSqlRaw("INSERT INTO Chapters_FTS_SpellFix(command) VALUES(\"reset\");");
                }
            }
            finally
            {
                database.CommitTransaction();
            }
        }

        private static bool shouldSkipAddedChapter(Chapter chapterNew)
        {
            // TODO: add your logic to avoid indexing this item
            return false;
        }

        private static bool shouldSkipModifiedChapter(Chapter chapterNew, Chapter chapterOld)
        {
            // TODO: add your logic to avoid indexing this item
            return chapterNew.Text == chapterOld.Text && chapterNew.Title == chapterOld.Title;
        }
    }
}