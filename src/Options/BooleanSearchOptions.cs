namespace BooleanSearcher.Options;

public class BooleanSearchOptions
{
    public string TokensPerDocDir { get; set; } = "tokens_per_doc";
    public string IndexPath { get; set; } = "inverted_index.txt";
    public string DocumentsIndexPath { get; set; } = "index.txt";
    public string? Query { get; set; }
}