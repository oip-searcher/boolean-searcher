using System.Text.RegularExpressions;

namespace BooleanSearcher.Services;

public sealed class BooleanQueryParser
{
    private readonly List<string> _tokens;
    private readonly Dictionary<string, HashSet<int>> _index;
    private readonly HashSet<int> _allDocs;
    private readonly LemmaGrouper _lemmaGrouper;
    private int _position;

    public BooleanQueryParser(
        string query,
        Dictionary<string, HashSet<int>> index,
        HashSet<int> allDocs)
    {
        _index = index;
        _allDocs = allDocs;
        _lemmaGrouper = new LemmaGrouper();
        _tokens = Tokenize(query);
    }

    public HashSet<int> Parse()
    {
        _position = 0;

        var result = ParseExpression();

        if (_position < _tokens.Count)
            throw new InvalidOperationException($"Лишний токен: {_tokens[_position]}");

        return result;
    }

    private HashSet<int> ParseExpression() => ParseOr();

    private HashSet<int> ParseOr()
    {
        var left = ParseAnd();

        while (Match("OR"))
        {
            var right = ParseAnd();
            left.UnionWith(right);
        }

        return left;
    }

    private HashSet<int> ParseAnd()
    {
        var left = ParseUnary();

        while (Match("AND"))
        {
            var right = ParseUnary();
            left.IntersectWith(right);
        }

        return left;
    }

    private HashSet<int> ParseUnary()
    {
        if (Match("NOT"))
        {
            var operand = ParseUnary();
            var result = new HashSet<int>(_allDocs);
            result.ExceptWith(operand);
            return result;
        }

        return ParsePrimary();
    }

    private HashSet<int> ParsePrimary()
    {
        if (Match("("))
        {
            var expr = ParseExpression();

            if (!Match(")"))
                throw new InvalidOperationException("Ожидалась закрывающая скобка ')'");

            return expr;
        }

        var token = ConsumeTerm();
        var lemma = NormalizeTerm(token);

        return _index.TryGetValue(lemma, out var docs)
            ? new HashSet<int>(docs)
            : new HashSet<int>();
    }

    private string NormalizeTerm(string term)
    {
        var lemma = _lemmaGrouper.GetLemma(term);
        return string.IsNullOrWhiteSpace(lemma)
            ? term.ToLowerInvariant()
            : lemma.ToLowerInvariant();
    }

    private string ConsumeTerm()
    {
        if (_position >= _tokens.Count)
            throw new InvalidOperationException("Ожидался термин");

        var current = _tokens[_position];

        if (current is "AND" or "OR" or "NOT" or "(" or ")")
            throw new InvalidOperationException($"Ожидался термин, но найдено: {current}");

        _position++;
        return current;
    }

    private bool Match(string expected)
    {
        if (_position < _tokens.Count &&
            string.Equals(_tokens[_position], expected, StringComparison.OrdinalIgnoreCase))
        {
            _position++;
            return true;
        }

        return false;
    }

    private static List<string> Tokenize(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new InvalidOperationException("Пустой запрос");

        var normalized = Regex.Replace(
            query,
            @"\bAND\b|\bOR\b|\bNOT\b",
            m => m.Value.ToUpperInvariant(),
            RegexOptions.IgnoreCase);

        var matches = Regex.Matches(
            normalized,
            @"\(|\)|\bAND\b|\bOR\b|\bNOT\b|[а-яА-ЯёЁa-zA-Z0-9_]+",
            RegexOptions.IgnoreCase);

        return matches
            .Select(m => m.Value)
            .Select(x => x is "AND" or "OR" or "NOT" or "(" or ")" ? x : x.ToLowerInvariant())
            .ToList();
    }
}