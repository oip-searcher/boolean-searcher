using BooleanSearcher.Options;
using BooleanSearcher.Services;
using Microsoft.Extensions.Options;

namespace BooleanSearcher.Workers;

public sealed class BooleanSearchWorker : BackgroundService
{
    private readonly IOptions<BooleanSearchOptions> _options;
    private readonly ILogger<BooleanSearchWorker> _logger;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IHostEnvironment _env;

    public BooleanSearchWorker(
        IOptions<BooleanSearchOptions> options,
        ILogger<BooleanSearchWorker> logger,
        IHostApplicationLifetime lifetime,
        IHostEnvironment env)
    {
        _options = options;
        _logger = logger;
        _lifetime = lifetime;
        _env = env;
    }

    private string ResolveProjectPath(string path)
        => Path.IsPathRooted(path)
            ? path
            : Path.GetFullPath(Path.Combine(_env.ContentRootPath, path));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var opt = _options.Value;

            var tokensPerDocDir = ResolveProjectPath(opt.TokensPerDocDir);
            var indexPath = ResolveProjectPath(opt.IndexPath);

            if (!Directory.Exists(tokensPerDocDir))
                throw new DirectoryNotFoundException($"TokensPerDocDir not found: {tokensPerDocDir}");

            var indexService = new InvertedIndexService();

            _logger.LogInformation("Building inverted index from {Dir}", tokensPerDocDir);
            indexService.Build(tokensPerDocDir);

            _logger.LogInformation("Unique terms: {Count}", indexService.InvertedIndex.Count);

            indexService.Save(indexPath);
            _logger.LogInformation("Index saved to {Path}", indexPath);

            RunSearchLoop(indexService, stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BooleanSearch failed");
        }
        finally
        {
            _lifetime.StopApplication();
        }

        await Task.CompletedTask;
    }

    private void RunSearchLoop(InvertedIndexService indexService, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Введите запрос с AND, OR, NOT и скобками.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Запрос > ");
            var query = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(query))
                continue;

            if (query.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            try
            {
                var parser = new BooleanQueryParser(
                    query,
                    indexService.InvertedIndex,
                    indexService.GetAllDocuments());

                var results = parser.Parse();
                PrintResults(results, indexService.DocIdToFileName);
            }
            catch (Exception ex)
            {
                _logger.LogInformation("Ошибка: {m}", ex.Message);
            }
        }
    }

    private static void PrintResults(HashSet<int> docIds, Dictionary<int, string> docIdToFileName)
    {
        if (docIds.Count == 0)
        {
            Console.WriteLine("Ничего не найдено.");
            return;
        }

        Console.WriteLine($"Найдено документов: {docIds.Count}");

        foreach (var id in docIds.OrderBy(x => x))
        {
            if (docIdToFileName.TryGetValue(id, out var fileName))
                Console.WriteLine($"  [{id}] {fileName}");
            else
                Console.WriteLine($"  [{id}]");
        }

        Console.WriteLine();
    }
}