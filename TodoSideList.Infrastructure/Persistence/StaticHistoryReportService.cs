using System.Globalization;
using System.Text;
using TodoSideList.Core.Models;
using TodoSideList.Core.Services;

namespace TodoSideList.Infrastructure.Persistence;

public sealed class StaticHistoryReportService : IHistoryReportService
{
    private readonly IAppPaths _appPaths;

    public StaticHistoryReportService(IAppPaths appPaths)
    {
        _appPaths = appPaths;
    }

    public string Generate(IReadOnlyCollection<TodoItem> completedItems)
    {
        Directory.CreateDirectory(_appPaths.HistoryDirectory);
        var reportPath = Path.Combine(_appPaths.HistoryDirectory, "index.html");
        var payload = BuildPayload(completedItems);
        File.WriteAllText(reportPath, payload);
        return reportPath;
    }

    private static string BuildPayload(IReadOnlyCollection<TodoItem> completedItems)
    {
        var grouped = completedItems
            .Where(item => item.CompletedAtUtc is not null)
            .GroupBy(item => item.CompletedAtUtc!.Value.UtcDateTime.Date)
            .OrderBy(group => group.Key)
            .ToArray();

        var points = grouped
            .Select(group => $$"""{"date":"{{group.Key:yyyy-MM-dd}}","count":{{group.Count()}},"titles":[{{string.Join(",", group.Select(item => "\"" + Escape(item.Title) + "\""))}}]}""")
            .ToArray();

        var html = new StringBuilder();
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"en\">");
        html.AppendLine("<head>");
        html.AppendLine("  <meta charset=\"utf-8\" />");
        html.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        html.AppendLine("  <title>TodoSideList History</title>");
        html.AppendLine("  <style>");
        html.AppendLine("    body { font-family: 'Segoe UI', sans-serif; margin: 0; background: #101418; color: #f5f7fa; }");
        html.AppendLine("    main { max-width: 920px; margin: 0 auto; padding: 32px 20px 60px; }");
        html.AppendLine("    .card { background: #1a2128; border: 1px solid #2d3742; border-radius: 16px; padding: 20px; margin-top: 16px; }");
        html.AppendLine("    .bar { display: grid; grid-template-columns: 120px 1fr 48px; gap: 12px; align-items: center; margin: 10px 0; }");
        html.AppendLine("    .track { height: 14px; background: #24303c; border-radius: 999px; overflow: hidden; }");
        html.AppendLine("    .fill { height: 100%; background: linear-gradient(90deg, #5eead4, #38bdf8); }");
        html.AppendLine("    ul { margin: 8px 0 0; padding-left: 18px; }");
        html.AppendLine("    h1, h2, p { margin-top: 0; }");
        html.AppendLine("    .muted { color: #9aacbf; }");
        html.AppendLine("  </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("  <main>");
        html.AppendLine("    <h1>TodoSideList History</h1>");
        html.AppendLine($"    <p class=\"muted\">Generated {DateTimeOffset.UtcNow.ToString("u", CultureInfo.InvariantCulture)}</p>");
        html.AppendLine("    <section class=\"card\">");
        html.AppendLine($"      <h2>Completed tasks: {completedItems.Count(item => item.IsCompleted)}</h2>");
        html.AppendLine("      <div id=\"chart\"></div>");
        html.AppendLine("    </section>");
        html.AppendLine("    <section class=\"card\">");
        html.AppendLine("      <h2>Completed task titles</h2>");
        html.AppendLine("      <div id=\"details\"></div>");
        html.AppendLine("    </section>");
        html.AppendLine("  </main>");
        html.AppendLine("  <script>");
        html.AppendLine($"    const data = [{string.Join(",", points)}];");
        html.AppendLine("    const max = data.reduce((current, item) => Math.max(current, item.count), 1);");
        html.AppendLine("    document.getElementById('chart').innerHTML = data.length === 0 ? '<p>No completed tasks yet.</p>' : data.map(item => `");
        html.AppendLine("      <div class=\"bar\">");
        html.AppendLine("        <span>${item.date}</span>");
        html.AppendLine("        <div class=\"track\"><div class=\"fill\" style=\"width:${(item.count / max) * 100}%\"></div></div>");
        html.AppendLine("        <strong>${item.count}</strong>");
        html.AppendLine("      </div>`).join('');");
        html.AppendLine("    document.getElementById('details').innerHTML = data.length === 0 ? '<p>No history available.</p>' : data.map(item => `");
        html.AppendLine("      <article>");
        html.AppendLine("        <h3>${item.date}</h3>");
        html.AppendLine("        <ul>${item.titles.map(title => `<li>${title}</li>`).join('')}</ul>");
        html.AppendLine("      </article>`).join('');");
        html.AppendLine("  </script>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");
        return html.ToString();
    }

    private static string Escape(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
