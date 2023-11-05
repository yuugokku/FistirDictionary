using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Sqlite;
using Microsoft.EntityFrameworkCore.Storage;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;

namespace FistirDictionary
{
    public enum SearchTarget
    {
        Rhyme,
        HeadwordTranslation,
        Headword,
        Translation,
        Example,
    }

    public enum SearchMethod
    {
        RegexMatch,
        Includes,
        StartsWith,
        EndsWith,
        Is,
    }

    internal class SearchStatement
    {
        public string? Keyword { get; set; }
        public SearchTarget? Target { get; set; }
        public SearchMethod? Method { get; set; }
    }

    /// <summary>
    ///   FDictionary: SQLite-FDIC辞書ファイルの読み書き
    /// </summary>
    internal class FDictionary
    {
        public class DictionaryNotFoundExcepction : Exception
        {
            public string? Message { get; set; }
            public DictionaryNotFoundExcepction(string message) : base(message)
            {
                Message = message;
            }
        }

        public class DictionaryException : Exception
        {
            public string? Message { get; set; }
            public DictionaryException(string message) : base(message)
            {
                Message = message;
            }
        }

        public class DictionaryBrokenException : Exception
        {
            public string? Message { get; set; }
            public DictionaryBrokenException(string message) : base(message)
            {
                Message = message;
            }
        }

        private class WordRhyme
        {
            public int WordID { get; set; }
            public string? Headword { get; set; }
            public string? Translation { get; set; }
            public string? Example { get; set; }
            public DateTime? UpdatedAt { get; set; }

            public string? ScansionScript { get; set; }
            public string? Scanned { get; set; }
        }

        public static string GetSqliteConnectionString(string filepath)
        {
            return $"Data Source={filepath};";
        }

        public static ICollection<Word> GetDictionaryMetadata(string dictionaryPath)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            return (from word in db.Words
                where word.Headword != null && word.Headword.StartsWith("__")
                select word)
                .ToArray();
        }

        /// <summary>
        ///   空の辞書を作成します。
        /// </summary>
        /// <param name="dictionaryPath"></param>
        /// <param name="dicname"></param>
        /// <param name="description"></param>
        /// <param name="scansionScript"></param>
        /// <param name="derivationScript"></param>
        /// <param name="enableWordHistory"></param>
        /// <param name="authorInfo"></param>
        public static void CreateEmptyDictionary(
            string dictionaryPath,
            string dicname,
            string description,
            string scansionScript,
            string derivationScript,
            bool enableWordHistory,
            string authorInfo)
        {
            if (System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryException($"{dictionaryPath} がすでに存在します。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            RelationalDatabaseCreator rdc = (RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>();
            if (!rdc.HasTables())
            {
                rdc.CreateTables();
            }
            var metadata = new Dictionary<string, string>()
                {
                    { "__Name", dicname },
                    { "__Description", description },
                    { "__ScansionScript", scansionScript },
                    { "__DerivationScript", derivationScript },
                    { "__EnableHistory", enableWordHistory ? "true" : "false" },
                    { "__Author", authorInfo }
                };
            var counter = -1;
            foreach (var mt in metadata)
            {
                var row = new Word
                {
                    WordID = counter,
                    Headword = mt.Key,
                    Translation = mt.Value,
                    UpdatedAt = DateTime.Now
                };
                db.Add(row);
                counter--;
            }
            db.SaveChanges();
        }

        public static Word AddWord(
            string dictionaryPath,
            string headword,
            string translation,
            string example)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            var w = new Word
            {
                Headword = headword,
                Translation = translation,
                Example = example,
                UpdatedAt = DateTime.Now
            };
            db.Words.Add(w);
            db.SaveChanges();
            return w;
        }

        public static int AddWord(string dictionaryPath, Word[] wordsToAdd)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            db.Words.AddRange(wordsToAdd);
            return db.SaveChanges(true);
        }

        public static Word? UpdateWord(
            string dictionaryPath,
            int wordID,
            string headword,
            string translation,
            string example)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            var w = (from word in db.Words
                     where word.WordID == wordID
                     select word).First();
            var wh = new WordHistory
            {
                WordID = w.WordID,
                Headword = w.Headword,
                Translation = w.Translation,
                Example = w.Example,
                UpdatedAt = w.UpdatedAt
            };
            var metadata = GetDictionaryMetadata(dictionaryPath);
            if (metadata != null &&
                metadata.Where(row => row.Headword == "__EnableHistory")
                        .First()
                        .Translation?
                        .ToLower() == "true")
            {
                db.WordHistories.Add(wh);
            }
            var changed = false;
            if (w.Headword != headword)
            {
                w.Headword = headword;
                changed = true;
            }
            if (w.Translation != translation)
            {
                w.Translation = translation;
                changed = true;
            }
            if (w.Example != example)
            {
                w.Example = example;
                changed = true;
            }
            if (changed)
            {
                w.UpdatedAt = DateTime.Now;
                db.SaveChanges();
                return w;
            }
            else
            {
                return null;
            }
        }

        public static Word GetWord(string dictionaryPath, int wordID)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            return (from word in db.Words
                    where word.WordID == wordID
                    select word)
                    .First();
        }

        public static void DeleteWord(string dictionaryPath, int wordID)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            var w = db.Words.Single(word => word.WordID == wordID);
            db.Words.Remove(w);
            db.SaveChanges();
        }

        public static Word[] SearchWord(string dictionaryPath, SearchStatement[] statements, bool ignoreCase)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            var scansionScriptPath = db.Words.First(word => word.Headword == "__ScansionScript").Translation;
            var dictionaryName = db.Words.First(word => word.Headword == "__Name").Translation;
            string scriptHash = "";
            if (scansionScriptPath != null && scansionScriptPath.Length > 0)
            {
                scriptHash = FLua.GetScriptHash(scansionScriptPath);
            }
            IQueryable<WordRhyme> dbQuery =
                from word in db.Words
                join rhyme in db.Rhymes
                on new { word.Headword, Script = scriptHash } equals new { rhyme.Headword, Script = rhyme.ScansionScript } into wordRhyme
                from wr in wordRhyme.DefaultIfEmpty()
                select new WordRhyme
                {
                    WordID = word.WordID,
                    Headword = word.Headword,
                    Translation = word.Translation,
                    Example = word.Example,
                    UpdatedAt = word.UpdatedAt,
                    Scanned = wr.Scanned,
                    ScansionScript = wr.ScansionScript,
                };
            foreach (var statement in statements)
            {
                if (statement == null) continue;
                if (statement.Keyword == null || statement.Keyword.Length == 0) continue;
                switch (statement.Method)
                {
                    case SearchMethod.Is:
                        switch (statement.Target)
                        {
                            case SearchTarget.HeadwordTranslation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(
                                        word => word.Headword.ToLower() == statement.Keyword.ToLower()
                                        || word.Translation.ToLower() == statement.Keyword.ToLower());
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(
                                        word => word.Headword == statement.Keyword
                                        || word.Translation == statement.Keyword);
                                }
                                break;
                            case SearchTarget.Headword:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word => word.Headword.ToLower() == statement.Keyword.ToLower());
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word => word.Headword == statement.Keyword);
                                }
                                break;
                            case SearchTarget.Translation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word => word.Translation.ToLower() == statement.Keyword.ToLower());
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word => word.Translation == statement.Keyword);
                                }
                                break;
                            case SearchTarget.Example:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word => word.Example.ToLower() == statement.Keyword.ToLower());
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word => word.Example == statement.Keyword);
                                }
                                break;
                        }
                        break;
                    case SearchMethod.Includes:
                        switch (statement.Target)
                        {
                            case SearchTarget.HeadwordTranslation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        (word.Headword != null && word.Headword.ToLower().Contains(statement.Keyword.ToLower()))
                                        || (word.Translation != null && word.Translation.ToLower().Contains(statement.Keyword.ToLower())));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        (word.Headword != null && word.Headword.Contains(statement.Keyword))
                                        || (word.Translation != null && word.Translation.Contains(statement.Keyword)));
                                }
                                break;
                            case SearchTarget.Headword:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Headword != null && word.Headword.ToLower().Contains(statement.Keyword.ToLower()));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Headword != null && word.Headword.Contains(statement.Keyword));
                                }
                                break;
                            case SearchTarget.Translation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Translation != null && word.Translation.ToLower().Contains(statement.Keyword.ToLower()));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Translation != null && word.Translation.Contains(statement.Keyword));
                                }
                                break;
                            case SearchTarget.Example:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Example != null && word.Example.ToLower().Contains(statement.Keyword.ToLower()));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Example != null && word.Example.Contains(statement.Keyword));
                                }
                                break;
                        }
                        break;
                    case SearchMethod.StartsWith:
                        switch (statement.Target)
                        {
                            case SearchTarget.HeadwordTranslation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        (word.Headword != null && EF.Functions.Like(word.Headword.ToLower(), $"{statement.Keyword.ToLower()}%"))
                                        || (word.Translation != null && EF.Functions.Like(word.Translation.ToLower(), $"{statement.Keyword.ToLower()}%")));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        (word.Headword != null && EF.Functions.Like(word.Headword, $"{statement.Keyword}%"))
                                        || (word.Translation != null && EF.Functions.Like(word.Translation, $"{statement.Keyword}%")));
                                }
                                break;
                            case SearchTarget.Headword:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Headword != null && EF.Functions.Like(word.Headword.ToLower(), $"{statement.Keyword.ToLower()}%"));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Headword != null && EF.Functions.Like(word.Headword, $"{statement.Keyword}%"));
                                }
                                break;
                            case SearchTarget.Translation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Translation != null && EF.Functions.Like(word.Translation.ToLower(), $"{statement.Keyword.ToLower()}%"));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Translation != null && EF.Functions.Like(word.Translation, $"{statement.Keyword}%"));
                                }
                                break;
                            case SearchTarget.Example:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Example != null && EF.Functions.Like(word.Example.ToLower(), $"{statement.Keyword.ToLower()}%"));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Example != null && EF.Functions.Like(word.Example, $"{statement.Keyword}%"));
                                }
                                break;
                        }
                        break;
                    case SearchMethod.EndsWith:
                        switch (statement.Target)
                        {
                            case SearchTarget.HeadwordTranslation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        (word.Headword != null && EF.Functions.Like(word.Headword.ToLower(), $"%{statement.Keyword.ToLower()}"))
                                        || (word.Translation != null && EF.Functions.Like(word.Translation.ToLower(), $"%{statement.Keyword.ToLower()}")));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        (word.Headword != null && EF.Functions.Like(word.Headword, $"%{statement.Keyword}"))
                                        || (word.Translation != null && EF.Functions.Like(word.Translation, $"%{statement.Keyword}")));
                                }
                                break;
                            case SearchTarget.Headword:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Headword != null && EF.Functions.Like(word.Headword.ToLower(), $"%{statement.Keyword.ToLower()}"));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Headword != null && EF.Functions.Like(word.Headword, $"%{statement.Keyword}"));
                                }
                                break;
                            case SearchTarget.Translation:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Translation != null && EF.Functions.Like(word.Translation.ToLower(), $"%{statement.Keyword.ToLower()}"));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Translation != null && EF.Functions.Like(word.Translation, $"%{statement.Keyword}"));
                                }
                                break;
                            case SearchTarget.Example:
                                if (ignoreCase)
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Example != null && EF.Functions.Like(word.Example.ToLower(), $"%{statement.Keyword.ToLower()}"));
                                }
                                else
                                {
                                    dbQuery = dbQuery.Where(word =>
                                        word.Example != null && EF.Functions.Like(word.Example, $"%{statement.Keyword}"));
                                }
                                break;
                        }
                        break;
                    case default(SearchMethod):
                        break;
                }
            }
            dbQuery = dbQuery.Where(word => word.Headword != null && !word.Headword.StartsWith("__"));
            var query = dbQuery.ToList().AsQueryable();
            FLua flua = null;
            foreach (var statement in statements)
            {
                if (statement == null) continue;
                if (statement.Keyword == null || statement.Keyword.Length == 0) continue;
                if (statement.Method == SearchMethod.RegexMatch)
                {
                    switch (statement.Target)
                    {
                        case SearchTarget.HeadwordTranslation:
                            query = query.Where(word =>
                                (word.Headword != null && Regex.IsMatch(word.Headword, statement.Keyword))
                                || (word.Translation != null && Regex.IsMatch(word.Translation, statement.Keyword)));
                            break;
                        case SearchTarget.Headword:
                            query = query.Where(word =>
                                word.Headword != null && Regex.IsMatch(word.Headword, statement.Keyword));
                            break;
                        case SearchTarget.Translation:
                            query = query.Where(word =>
                                word.Translation != null && Regex.IsMatch(word.Translation, statement.Keyword));
                            break;
                        case SearchTarget.Example:
                            query = query.Where(word =>
                                word.Example != null && Regex.IsMatch(word.Example, statement.Keyword));
                            break;
                        default:
                            break;
                    }
                }
                else if (statement.Target == SearchTarget.Rhyme && scansionScriptPath != null && scansionScriptPath.Length > 0)
                {
                    flua ??= FLua.LoadScansionScript(scansionScriptPath);
                    switch (statement.Method)
                    {
                        case SearchMethod.Is:
                            query = query.Where(word =>
                                (
                                    word.Scanned == null ? 
                                    flua.Scan(word.Headword, word.Translation, word.Example, dictionaryName) :
                                    word.Scanned
                                ) == statement.Keyword);
                            break;
                        case SearchMethod.Includes:
                            query = query.Where(word =>
                                (
                                    word.Scanned == null ?
                                    flua.Scan(word.Headword,word.Translation, word.Example, dictionaryName) :
                                    word.Scanned
                                ).Contains(statement.Keyword));
                            break;
                        case SearchMethod.StartsWith:
                            query = query.Where(word =>
                                (
                                    word.Scanned == null ?
                                    flua.Scan(word.Headword, word.Translation, word.Example, dictionaryName) :
                                    word.Scanned
                                ).StartsWith(statement.Keyword));
                            break;
                        case SearchMethod.EndsWith:
                            query = query.Where(word =>
                                (
                                    word.Scanned == null ?
                                    flua.Scan(word.Headword, word.Translation, word.Example, dictionaryName) :
                                    word.Scanned
                                ).EndsWith(statement.Keyword));
                            break;
                        case SearchMethod.RegexMatch:
                            query = query.Where(word => Regex.IsMatch(
                                word.Scanned == null ?
                                flua.Scan(word.Headword, word.Translation, word.Example, dictionaryName) :
                                word.Scanned, statement.Keyword));
                            break;
                    }
                }
            }
            var result = query.Select(wordRhyme => new Word {
                WordID = wordRhyme.WordID,
                Headword = wordRhyme.Headword,
                Translation = wordRhyme.Translation,
                Example = wordRhyme.Example,
                UpdatedAt = wordRhyme.UpdatedAt,
            }).ToArray();

            if (scansionScriptPath != null &&
                scansionScriptPath.Length > 0 &&
                flua != null &&
                flua.HasScanned())
            {
                Task.Run(() =>
                {
                    using var _db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
                    var f = FLua.LoadScansionScript(scansionScriptPath);
                    foreach (var wr in from word in _db.Words
                        join rhyme in _db.Rhymes
                        on new { word.Headword, Script = scriptHash } equals new { rhyme.Headword, Script = rhyme.ScansionScript } into wordRhyme
                        from wr in wordRhyme.DefaultIfEmpty()
                        select new WordRhyme
                        { 
                            WordID = word.WordID,
                            Headword = word.Headword,
                            Translation = word.Translation,
                            Example = word.Example,
                            UpdatedAt = word.UpdatedAt,
                            Scanned = wr.Scanned,
                            ScansionScript = wr.ScansionScript,
                        })
                    {
                        if (wr.Scanned == null)
                        {
                            _db.Rhymes.Add(new Rhyme
                            {
                                Headword = wr.Headword,
                                Translation = wr.Translation,
                                Example = wr.Example,
                                Scanned = f.Scan(wr.Headword, wr.Translation, wr.Example, dictionaryName),
                                DictionaryName = dictionaryName,
                                ScansionScript = scriptHash,
                            });
                        }
                    }
                    _db.SaveChanges();
                });
            }

            return result;
        }

        public static List<WordHistory> GetWordHistory(string dictionaryPath, int wordID)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            var result = db.WordHistories.Where(wh => wh.WordID == wordID).ToList();
            return result;
        }

        public static int GetWordCount(string dictionaryPath)
        {
            if (!System.IO.File.Exists(dictionaryPath))
            {
                throw new DictionaryNotFoundExcepction($"{dictionaryPath} が見つかりません。");
            }
            using var db = new DictionaryContext(GetSqliteConnectionString(dictionaryPath));
            var result = db.Words.Where(word => word.Headword != null && !word.Headword.StartsWith("__")).Count();
            return result;
        }
    }

    internal class DictionaryContext : DbContext
    {
        private readonly string _connectionString;

        public DbSet<Word> Words { get; set; }
        public DbSet<WordHistory> WordHistories { get; set; }
        public DbSet<Rhyme> Rhymes { get; set; }
        public DbSet<Derivation> Derivation { get; set; }

        public DictionaryContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            optionsBuilder.UseSqlite(_connectionString).LogTo(Console.WriteLine);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<Word>()
                .HasKey(w => w.WordID)
                .HasName("PrimaryKey_WordID");
            modelBuilder.Entity<Word>()
                .HasMany(w => w.WordHistories)
                .WithOne(wh => wh.WordLatest)
                .HasForeignKey(e => e.WordID)
                .IsRequired(true);
            modelBuilder.Entity<Word>()
                .Property(e => e.WordID)
                .ValueGeneratedOnAdd();
        }
    }

    internal class Word
    {
        [Key]
        public int WordID { get; set; }
        public string? Headword { get; set; }
        public string? Translation { get; set; }
        public string? Example { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public ICollection<WordHistory>? WordHistories { get; set; }
    }

    internal class WordHistory
    {
        [Key]
        public int WordHistoryID { get; set; }
        public int? WordID { get; set; }
        public string? Headword { get; set; }
        public string? Translation { get; set; }
        public string? Example { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Word? WordLatest { get; set; }
    }

    internal class Rhyme
    {
        [Key]
        public int RhymeID { get; set; }
        public string? DictionaryName { get; set; }
        public string? ScansionScript { get; set; }
        public string? Headword { get; set; }
        public string? Translation { get; set; }
        public string? Example { get; set; }
        public string? Scanned { get; set; }
    }

    internal class Derivation
    {
        [Key]
        public string DerivationID { get; set; }
        public string? DictionaryName { get; set; }
        public string? DerivationScript { get; set; }
        public string? DerivationForm { get; set; }
        public string? Headword { get; set; }
    }
}
