using ClosedXML.Excel;
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FistirDictionary.ModelExcelTable;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FistirDictionary
{
    internal class FDictionaryExcel
    {
        /// <summary>
        ///   空のExcel辞書を作成します。
        /// </summary>
        /// <param name="dicname">辞書の名前</param>
        /// <param name="description">辞書の説明</param>
        /// <param name="scansionScript">韻律解析スクリプトのパス</param>
        /// <param name="derivationScript">派生語生成スクリプトのパス</param>
        /// <returns></returns>
        public static DataSet CreateEmptyDictionary(
            string dicname,
            string description,
            string scansionScript,
            string derivationScript,
            bool enableWordHistory,
            string authorInfo)
        {
            var wt = CreateEmptyWordTable(dicname, description, scansionScript, derivationScript, enableWordHistory, authorInfo);
            var wht = CreateEmptyWordHistoryTable();
            var ds = new DataSet();
            ds.Tables.Add(wt);
            ds.Tables.Add(wht);
            return ds;
        }

        /// <summary>
        ///   与えられたFDictionary DataSetを指定したPathへExcel形式で保存します。
        /// </summary>
        /// <param name="ds">FDictinoary DataSet</param>
        /// <param name="path">Excelファイルの保存先パス</param>
        public static void SaveToExcel(DataSet ds, string path)
        {
            var workbook = new XLWorkbook();
            workbook.Worksheets.Add(ds.Tables["Word"]);
            workbook.Worksheets.Add(ds.Tables["WordHistory"]);
            workbook.SaveAs(path);
        }

        public static DataSet ReadExcel(string path)
        {
            var workbook = new XLWorkbook(path);
            var wht = workbook.Worksheets.First(sh => sh.Name == "WordHistory").Tables.First().AsNativeDataTable();
            wht.TableName = "WordHistory";
            var wt = workbook.Worksheets.First(sh => sh.Name == "Word").Tables.First().AsNativeDataTable();
            wt.TableName = "Word";
            var ds = new DataSet();
            ds.Tables.Add(wt);
            ds.Tables.Add(wht);
            return ds;
        }

        private static DataTable CreateEmptyWordTable(
            string dicname,
            string description,
            string scansionScript,
            string derivationScript,
            bool enableWordHistory,
            string authorInfo)
        {
            var wordTable = new DataTable();
            wordTable.TableName = "Word";
            foreach (var column in Definition.WordColumnNames
                .Zip(Definition.WordColumnNameTypes, (n, t) => new { Name = n, Type_ = t}))
            {
                wordTable.Columns.Add(column.Name, column.Type_);
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
                var row = wordTable.NewRow();
                row.SetField("WordID", counter);
                row.SetField("HeadWord", mt.Key);
                row.SetField("Translation", mt.Value);
                row.SetField<DateTime>("UpdatedAt", DateTime.Now);
                wordTable.Rows.Add(row);
                counter--;
            }
            return wordTable;
        }

        private static DataTable CreateEmptyWordHistoryTable()
        {
            var whTable = new DataTable();
            whTable.TableName = "WordHistory";
            foreach (var column in Definition
                .WordHistoryColumnNames
                .Zip(Definition.WordHistoryColumnNameTypes, (n, t) => new { Name = n, Type_ = t}))
            {
                whTable.Columns.Add(column.Name, column.Type_);
            }
            return whTable;
        }

    }

    namespace ModelExcelTable
    {
        internal class Definition
        {
            public static List<string> SheetNames 
            { 
                get
                {
                    return new List<string>() { "Word", "WordHistory" };
                } 
            }

            internal class Sheet
            {
                public const string Word = "Word";
                public const string WordHistory = "WordHistory";
            }

            public static List<string> WordColumnNames
            {
                get
                {
                    return new List<string>() { "WordID", "Headword", "Translation", "Example", "UpdatedAt" };
                }
            }

            public static List<Type> WordColumnNameTypes
            {
                get
                {
                    return new List<Type>() { typeof(int), typeof(string), typeof(string), typeof(string), typeof(DateTime) };
                }
            }

            public static List<string> WordHistoryColumnNames
            {
                get
                {
                    return new List<string>() { "WordHistoryID", "WordID", "Headword", "Translation", "Example", "UpdatedAt" };
                }
            }
            public static List<Type> WordHistoryColumnNameTypes
            {
                get
                {
                    return new List<Type>() { typeof(int), typeof(int), typeof(string), typeof(string), typeof(string), typeof(DateTime) };
                }
            }
        }

        public class Word
        {
            public const string WordID = "WordID";
            public const string Headword = "Headword";
            public const string Translation = "Translation";
            public const string Example = "Example";
            public const string UpdatedAt = "UpdatedAt";
            public static Type WordIDType { get { return typeof(int); } }
            public static Type HeadwordType { get { return typeof(string); } }
            public static Type TranslationType {  get { return typeof(string); } }
            public static Type ExampleType { get { return typeof(string); } }
            public static Type UpdatedAtType { get { return typeof(DateTime); } }
        }

        public class WordHistory
        {
            public const string WordHistoryID = "WordHistoryID";
            public const string WordID = "WordID";
            public const string Headword = "Headword";
            public const string Translation = "Translation";
            public const string Example = "Example";
            public const string UpdatedAt = "UpdatedAt";
            public static Type WordHistoryIDType {  get { return typeof(int); } }
            public static Type WordIDType { get { return typeof(int); } }
            public static Type HeadwordType { get { return typeof(string); } }
            public static Type TranslationType {  get { return typeof(string); } }
            public static Type ExampleType { get { return typeof(string); } }
            public static Type UpdatedAtType { get { return typeof(DateTime); } }
        }
    }
}
