using System;
using System.Linq;
using EFCoreSQLiteFTS.DataLayer;
using EFCoreSQLiteFTS.Entities;
using Microsoft.EntityFrameworkCore;

namespace EFCoreSQLiteFTS
{
    class Program
    {
        static void Main(string[] args)
        {
            InitDb.Start();

            EFServiceProvider.RunInContext(context =>
            {
                basicQueries(context);
            });

            // testUpdateTriggers();
        }

        private static void testUpdateTriggers()
        {
            EFServiceProvider.RunInContext(context =>
            {
                var item = context.Chapters.First();
                item.Title += ", Modified";
                context.SaveChanges();
            });

            EFServiceProvider.RunInContext(context =>
            {
                var item = context.Chapters.First();
                context.Remove(item);
                context.SaveChanges();
            });
        }

        private static void basicQueries(ApplicationDbContext context)
        {
            // Full table scan queries ---> !!BAD!!
            var cats = context.Chapters.Where(item => item.Text.Contains("cat")).ToList();
            cats = context.Chapters.Where(item => item.Text.StartsWith("cat")).ToList();
            cats = context.Chapters.Where(item => item.Text.EndsWith("cat")).ToList();

            // FTS queries
            // Query for all rows that contain at least once instance of the term
            // "fts5" (in any column). The following 2 queries are equivalent.
            const string ftsSql = "SELECT rowid, title, text, rank FROM Chapters_FTS WHERE Chapters_FTS MATCH {0}";
            searchChaptersFor(context, ftsSql, "fts5");

            searchChaptersFor(context, "SELECT rowid, title, text, rank FROM Chapters_FTS WHERE Chapters_FTS = {0}", "fts5");

            // To sort the search results from the most to least relevant, you use the ORDER BY clause as follows:
            searchChaptersFor(context, "SELECT rowid, title, text, rank FROM Chapters_FTS WHERE Chapters_FTS MATCH {0} ORDER BY rank", "text");

            // The following query returns all documents that match the search term Learn SQLite:
            searchChaptersFor(context, ftsSql, "learn SQLite");

            // `search*` matches with search, searching, searches, etc. See the following example:
            searchChaptersFor(context, ftsSql, "search*");

            // to get the documents that match the learn phrase but doesn’t match the FTS5 phrase, you use the NOT operator as follows:
            searchChaptersFor(context, ftsSql, "learn NOT text");

            // That's the same as : The NOT operator (or, if using the standard syntax, a unary "-" operator)
            // In a column filter, a ```dash``` means to NOT look at the following columns.
            searchChaptersFor(context, ftsSql, "learn -title:text");

            searchChaptersFor(context, ftsSql, "\"2018-2019\"");

            // finds records that containt the two words 2018 and 2019
            searchChaptersFor(context, ftsSql, "2018 2019");

            // search for the phrase "2018 2019" instead
            searchChaptersFor(context, ftsSql, "\"2018 2019\"");

            // To search for prefixes
            searchChaptersFor(context, ftsSql, "some*");

            // To search for documents that match either phrase learn or text, you use the OR operator as the following example:
            searchChaptersFor(context, ftsSql, "learn OR text");

            // To find the documents that match both SQLite and searching, you use the AND operator as shown below:
            searchChaptersFor(context, ftsSql, "sqlite AND searching");

            // To change the operator precedence, you use parenthesis to group expressions. For example:
            searchChaptersFor(context, ftsSql, "search AND sqlite OR help");

            // To find the documents that match search and either sqlite or help, you use parenthesis as follows:
            searchChaptersFor(context, ftsSql, "search AND (sqlite OR help)");

            // search on 1 field only
            // All columns of an FTS table are full-text-indexed.
            searchChaptersFor(context, ftsSql, "text:some AND title:sqlite"); // AND is case sensitive!

            searchChaptersFor(context, ftsSql, "text:some OR title:sqlite"); // OR is case sensitive!

            // Built-in auxiliary functions
            searchChaptersFor(context, "SELECT rowid, highlight(Chapters_FTS, title, '<b>', '</b>') as title, snippet(Chapters_FTS, text, '<b>', '</b>', '...', 64) as text, rank FROM Chapters_FTS WHERE Chapters_FTS MATCH {0} ORDER BY bm25(Chapters_FTS)", "SQLite");

            // handle markup
            searchChaptersFor(context, "SELECT rowid, highlight(Chapters_FTS, title, '<b>', '</b>') as title, snippet(Chapters_FTS, text, '<b>', '</b>', '...', 64) as text, rank FROM Chapters_FTS WHERE Chapters_FTS MATCH {0} ORDER BY bm25(Chapters_FTS)", "funny");

            // handle markup, search for removed tags
            searchChaptersFor(context, "SELECT rowid, highlight(Chapters_FTS, title, '<b>', '</b>') as title, snippet(Chapters_FTS, text, '<b>', '</b>', '...', 64) as text, rank FROM Chapters_FTS WHERE Chapters_FTS MATCH {0} ORDER BY bm25(Chapters_FTS)", "font");

            // unicode
            searchChaptersFor(context, "SELECT rowid, highlight(Chapters_FTS, title, '<b>', '</b>') as title, snippet(Chapters_FTS, text, '<b>', '</b>', '...', 64) as text, rank FROM Chapters_FTS WHERE Chapters_FTS MATCH {0} ORDER BY bm25(Chapters_FTS)", "آزمايش");

            // --- exclude
            // If a column filter specification is preceded by a "-" character, then it is interpreted as a list of column not to match against.
            searchChaptersFor(context,
                @"SELECT rowid, highlight(Chapters_FTS, title, '<b>', '</b>') as title,
                snippet(Chapters_FTS, text, '<b>', '</b>', '...', 64) as text,
                rank
                FROM Chapters_FTS WHERE Chapters_FTS MATCH {0} ORDER BY bm25(Chapters_FTS)", "-title:آزمايش");

            // spell check
            searchSpellfixChaptersFor(context,
                @"SELECT word, rank, distance, score, matchlen FROM Chapters_FTS_SpellFix
                WHERE word MATCH {0} and top=6", "فارشي");
        }

        private static void searchSpellfixChaptersFor(ApplicationDbContext context, string ftsSql, string parameter)
        {
            Console.WriteLine(ftsSql);
            foreach (var item in context.Set<SpellCheck>().FromSqlRaw(ftsSql, parameter))
            {
                Console.WriteLine($"Word: {item.Word}");
                Console.WriteLine($"Distance: {item.Distance}");
            }
            Console.WriteLine(Environment.NewLine);
        }

        private static void searchChaptersFor(ApplicationDbContext context, string ftsSql, string parameter)
        {
            Console.WriteLine(ftsSql);
            foreach (var chapter in context.Set<ChapterFTS>().FromSqlRaw(ftsSql, parameter))
            {
                Console.WriteLine($"Title: {chapter.Title}");
                Console.WriteLine($"Text: {chapter.Text}");
            }
            Console.WriteLine(Environment.NewLine);
        }
    }
}