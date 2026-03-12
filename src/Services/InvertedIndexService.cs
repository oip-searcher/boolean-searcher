using System.Text;

namespace BooleanSearcher.Services;

public class InvertedIndexService
{
    public Dictionary<string, HashSet<int>> InvertedIndex { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<int, string> DocIdToFileName { get; } = new();

    public void Build(string tokensPerDocDir)
    {
        var files = Directory.GetFiles(tokensPerDocDir, "*_tokens.txt")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var docId = 0;

        foreach (var file in files)
        {
            var tokens = File.ReadAllLines(file, Encoding.UTF8)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim());

            foreach (var token in tokens)
            {
                if (!InvertedIndex.TryGetValue(token, out var docs))
                {
                    docs = new HashSet<int>();
                    InvertedIndex[token] = docs;
                }

                docs.Add(docId);
            }

            var name = Path.GetFileNameWithoutExtension(file);
            const string suffix = "_tokens";

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