using System.Text;

namespace BooleanSearcher.Services;

public sealed class InvertedIndexService
{
    public Dictionary<string, HashSet<int>> InvertedIndex { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<int, string> DocIdToFileName { get; } = new();

    public void BuildFromLemmas(string lemmasPerDocDir)
    {
        var files = Directory.GetFiles(lemmasPerDocDir, "*_lemmas.txt")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var docId = 0;

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file, Encoding.UTF8)
                .Where(x => !string.IsNullOrWhiteSpace(x));

            foreach (var line in lines)
            {
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 0) continue;

                var lemma = parts[0].Trim();
                if (string.IsNullOrWhiteSpace(lemma)) continue;

                if (!InvertedIndex.TryGetValue(lemma, out var docs))
                {
                    docs = new HashSet<int>();
                    InvertedIndex[lemma] = docs;
                }

                docs.Add(docId);
            }

            var name = Path.GetFileNameWithoutExtension(file); // 1_lemmas
            const string suffix = "_lemmas";

            var originalFileName = name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
                ? name[..^suffix.Length] + ".txt"
                : name + ".txt";

            DocIdToFileName[docId] = originalFileName;
            docId++;
        }
    }

    public void Save(string indexPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(indexPath) ?? ".");

        var lines = InvertedIndex
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.Key}: {string.Join(", ", x.Value.OrderBy(v => v))}");

        File.WriteAllLines(indexPath, lines, Encoding.UTF8);
    }

    public HashSet<int> GetAllDocuments()
        => DocIdToFileName.Keys.ToHashSet();
}