using System;
using System.Collections.Generic;
using System.IO;

namespace AdlpLib.Configuration
{
    /// <summary>
    /// Примитивный парсер конфигурационных файлов формата "ключ: значение" или "ключ значение".
    /// Комментарии начинаются с '#', пустые строки игнорируются.
    /// </summary>
    internal static class Yaml
    {
        public static Dictionary<string, string> Parse(string Path)
        {
            var Result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var RawLine in File.ReadAllLines(Path))
            {
                var Line = RawLine;
                var Comment = Line.IndexOf('#');
                if (Comment >= 0)
                    Line = Line.Substring(0, Comment);

                Line = Line.Trim();
                if (Line.Length == 0)
                    continue;

                var Colon = Line.IndexOf(':');
                int Sep = Colon > 0 ? Colon : Line.IndexOf(' ');
                if (Sep <= 0)
                    continue;

                var Key = Line.Substring(0, Sep).Trim();
                var Value = Line.Substring(Sep + 1).Trim();

                Result[Key] = Value;
            }

            return Result;
        }
    }
}