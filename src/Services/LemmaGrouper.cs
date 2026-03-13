using Nestor;
using Nestor.Models;

namespace BooleanSearcher.Services;

public sealed class LemmaGrouper
{
    private readonly NestorMorph _morph = new();

    public string GetLemma(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return string.Empty;

        token = token.Replace('ё', 'е').ToLowerInvariant();

        Word[] infos;
        try { infos = _morph.WordInfo(token); }
        catch { return token; }

        if (infos == null || infos.Length == 0)
            return token;
        
        var best = infos
            .Select(w => w.Lemma?.Word)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s!.Replace('ё', 'е').ToLowerInvariant())
            .OrderBy(s => s.Length)
            .FirstOrDefault();

        return best ?? token;
    }

    public bool IsStopPos(string token)
    {
        token = token.Replace('ё', 'е').ToLowerInvariant();

        Word[] infos;
        try
        {
            infos = _morph.WordInfo(token);
        }
        catch
        {
            return false;
        }

        if (infos == null || infos.Length == 0)
            return false;

        var pos = infos[0].Tag.Pos;
        return pos is Pos.Preposition or Pos.Conjunction or Pos.Particle;
    }
}