using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace RaceDay.PlanningChecks;

// Sources:
// The IIE (2026), POE Part I General Feedback (HE), required operations.
// Microsoft (2026), Regex, LINQ to XML and ProcessStartInfo documentation:
// https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex
// https://learn.microsoft.com/en-us/dotnet/standard/linq/linq-xml-overview
// https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo
// Accessed: 17 October 2026.
internal static class Program
{
    private static readonly string[] TableNames =
    [
        "Roles", "Users", "Events", "EventCategories", "EventRoutes", "Enrolments", "Results"
    ];

    // These pairs follow the lecturer's 25 required operations.
    private static readonly string[] RequiredOperations =
    [
        "POST /api/auth/register",
        "POST /api/auth/login",
        "POST /api/auth/logout",
        "GET /api/profile",
        "PUT /api/profile",
        "GET /api/events",
        "GET /api/events/{eventId}",
        "GET /api/events/mine",
        "POST /api/events",
        "PUT /api/events/{eventId}",
        "DELETE /api/events/{eventId}",
        "GET /api/events/{eventId}/categories",
        "POST /api/events/{eventId}/categories",
        "PUT /api/events/{eventId}/categories/{categoryId}",
        "DELETE /api/events/{eventId}/categories/{categoryId}",
        "POST /api/enrolments",
        "GET /api/enrolments/mine",
        "GET /api/enrolments/{enrolmentId}",
        "GET /api/events/{eventId}/enrolments",
        "PUT /api/enrolments/{enrolmentId}/cancel",
        "DELETE /api/enrolments/{enrolmentId}",
        "POST /api/enrolments/{enrolmentId}/result",
        "GET /api/results/mine",
        "GET /api/events/{eventId}/results",
        "PUT /api/enrolments/{enrolmentId}/result"
    ];

    private static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args.Length == 0 ? Directory.GetCurrentDirectory() : args[0]);
            CheckFiles(root);
            CheckEndpoints(root);
            CheckSchemaDocuments(root);
            CheckSqlSections(root);
            CheckWritingAndHistory(root);
            Console.WriteLine("Planning checks passed. These checks do not execute SQL or test the API.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Planning check failed: {error.Message}");
            return 1;
        }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static string Read(string root, string file) =>
        File.ReadAllText(Path.Combine(root, file)).Replace("\r\n", "\n");

    private static void CheckFiles(string root)
    {
        string[] files =
        [
            "RaceDay.sln", "README.md", "README-Part1.md",
            "docs/RaceDay-ERD.drawio", "docs/RaceDay-ERD.png", "docs/RaceDay-ERD.svg",
            "docs/RaceDay-ERD-Notes.md", "docs/API-Endpoint-Plan.md",
            "docs/RaceDay-Data-Dictionary.md", "docs/RaceDay.sql",
            "docs/Part-1-Database-Corrections.md", "docs/Part-2-Requirements-Checklist.md",
            "docs/evidence/ssms-corrected-database-counts.png",
            "docs/evidence/ssms-corrected-database-success.png",
            "tools/RaceDay.PlanningChecks/RaceDay.PlanningChecks.csproj",
            "tools/RaceDay.PlanningChecks/Program.cs"
        ];
        foreach (string file in files)
        {
            var info = new FileInfo(Path.Combine(root, file));
            Check(info.Exists && info.Length > 0, $"Required file is missing or empty: {file}");
            if (info.Extension == ".png")
            {
                using var stream = info.OpenRead();
                using var reader = new BinaryReader(stream);
                Check(reader.ReadBytes(8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                    $"Invalid PNG file: {file}");
            }
        }
        Console.WriteLine("Passed: required files and PNG signatures.");
    }

    private static void CheckEndpoints(string root)
    {
        string plan = Read(root, "docs/API-Endpoint-Plan.md");
        const string header = "| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |";
        Check(plan.Contains(header), "The six-column endpoint header is missing.");
        var endpoints = new List<string>();
        foreach (string line in plan.Split('\n'))
        {
            if (!Regex.IsMatch(line, @"^\| (GET|POST|PUT|DELETE) \|"))
                continue;
            string[] cells = line.Trim('|').Split('|').Select(cell => cell.Trim()).ToArray();
            Check(cells.Length == 6 && cells.All(cell => cell.Length > 0), $"Incomplete endpoint row: {line}");
            Check(Regex.IsMatch(cells[1], @"^`/api/[^`\s]+`$"), $"Invalid API route: {cells[1]}");
            Check(Regex.IsMatch(cells[5], @"`2\d\d "), $"No success response: {cells[0]} {cells[1]}");
            endpoints.Add($"{cells[0]} {cells[1].Trim('`')}");
        }
        Check(endpoints.Count == endpoints.Distinct().Count(), "Duplicate method-and-route pairs found.");
        string[] missing = RequiredOperations.Except(endpoints).ToArray();
        Check(missing.Length == 0, $"Missing required operations: {string.Join(", ", missing)}");
        Check(endpoints.Count == 40, $"Expected the 40 planned endpoints; found {endpoints.Count}.");

        var mapping = Regex.Matches(plan, @"^\| (\d+) \| [^|]+ \| [^|]+ \| (GET|POST|PUT|DELETE) `([^`]+)` \|$", RegexOptions.Multiline);
        Check(mapping.Select(row => int.Parse(row.Groups[1].Value)).SequenceEqual(Enumerable.Range(1, 25)),
            "The required-operation mapping must list 1 to 25 once each.");
        Check(mapping.Select(row => $"{row.Groups[2].Value} {row.Groups[3].Value}").SequenceEqual(RequiredOperations),
            "The 25-operation mapping differs from the required methods and routes.");
        Console.WriteLine("Passed: 40 unique, complete endpoint rows and all 25 required operations.");
    }

    private static void CheckSchemaDocuments(string root)
    {
        string sql = Read(root, "docs/RaceDay.sql");
        string dictionary = Read(root, "docs/RaceDay-Data-Dictionary.md");
        var diagram = XDocument.Load(Path.Combine(root, "docs/RaceDay-ERD.drawio"));
        var svg = XDocument.Load(Path.Combine(root, "docs/RaceDay-ERD.svg"));
        XNamespace svgNamespace = "http://www.w3.org/2000/svg";
        Check(diagram.Root?.Name == "mxfile", "Invalid draw.io root element.");
        Check(svg.Root?.Name == svgNamespace + "svg", "Invalid SVG root element.");
        var cells = diagram.Descendants("mxCell").ToDictionary(cell => (string)cell.Attribute("id")!);
        var tables = Regex.Matches(sql, @"CREATE TABLE dbo\.(\w+)\s*\((.*?)\n    \);", RegexOptions.Singleline);
        Check(tables.Select(table => table.Groups[1].Value).Order().SequenceEqual(TableNames.Order()),
            "Expected the seven RaceDay table definitions.");
        var svgTables = TableNames.ToDictionary(name => name, _ => new List<string>());
        string? currentTable = null;
        foreach (var node in svg.Descendants(svgNamespace + "text"))
        {
            if (svgTables.ContainsKey(node.Value))
                currentTable = node.Value;
            else if (currentTable is not null)
                svgTables[currentTable].Add(node.Value);
        }
        int columnCount = 0;
        var foreignKeys = new List<string>();
        foreach (Match table in tables)
        {
            string name = table.Groups[1].Value;
            string body = table.Groups[2].Value;
            var section = Regex.Match(dictionary, @"### 5\.\d+ " + Regex.Escape(name) + @"\n(.*?)(?=\n### |\n## |\z)", RegexOptions.Singleline);
            Check(section.Success && cells.ContainsKey(name), $"{name} is missing from the dictionary or ERD.");
            var dictionaryColumns = Regex.Matches(section.Groups[1].Value, @"^\| `(\w+)` \| `([^`]+)` \| (Yes|No) \|", RegexOptions.Multiline)
                .Select(col => (col.Groups[1].Value, col.Groups[2].Value, col.Groups[3].Value == "Yes" ? "NULL" : "NOT NULL"));
            var columns = Regex.Matches(body, @"^        (\w+) ((?:INT|TINYINT|NVARCHAR\(\d+\)|DECIMAL\(\d+,\d+\)|DATETIME2\(0\)|DATE|BIT)(?: IDENTITY\(1,1\))?) (NOT NULL|NULL)", RegexOptions.Multiline)
                .Select(col => (Name: col.Groups[1].Value, Type: col.Groups[2].Value, Null: col.Groups[3].Value)).ToArray();
            Check(columns.SequenceEqual(dictionaryColumns), $"{name} columns differ between SQL and dictionary.");
            string label = WebUtility.HtmlDecode((string?)cells[name].Attribute("value") ?? "");
            label = Regex.Replace(label, @"<[^>]+>", "\n");
            foreach (var column in columns)
            {
                string pattern = "^" + Regex.Escape(column.Name) + @"\s+" + Regex.Escape(column.Type) + @"\s+" + Regex.Escape(column.Null) + @"(?:\s|$)";
                Check(Regex.IsMatch(label, pattern, RegexOptions.Multiline), $"{name}.{column.Name} differs in draw.io.");
                Check(svgTables[name].Any(line => Regex.IsMatch(line, pattern)), $"{name}.{column.Name} differs in SVG.");
            }
            columnCount += columns.Length;
            foreignKeys.AddRange(Regex.Matches(body, @"FOREIGN KEY \([^)]+\)\s+REFERENCES dbo\.(\w+)")
                .Select(key => $"{key.Groups[1].Value}|{name}"));
        }
        Check(columnCount == 71, "Expected 71 columns across all seven tables.");
        var links = cells.Values.Where(cell => (string?)cell.Attribute("edge") == "1")
            .Select(cell => $"{cell.Attribute("source")?.Value}|{cell.Attribute("target")?.Value}").ToArray();
        Check(links.Length == 9 && links.Order().SequenceEqual(foreignKeys.Order()),
            "The nine ERD relationships must match the SQL foreign keys.");
        Console.WriteLine("Passed: seven tables, 71 matching column definitions and nine relationships.");
    }

    private static void CheckSqlSections(string root)
    {
        string sql = Read(root, "docs/RaceDay.sql");
        foreach (string table in TableNames)
        {
            Check(Regex.IsMatch(sql, @"CREATE TABLE dbo\." + table + @"(?:\s|$)"), $"CREATE TABLE is missing for {table}.");
            Check(Regex.IsMatch(sql, @"INSERT INTO dbo\." + table + @"(?:\s|$)"), $"Seed INSERT is missing for {table}.");
        }
        string[] sections =
        [
            "PRIMARY KEY", "FOREIGN KEY", "CHECK", "DEFAULT", "CREATE UNIQUE NONCLUSTERED INDEX",
            "DistanceKm DECIMAL(6,2) NOT NULL", "MaximumAge TINYINT NULL",
            "ProfilePictureBlobName NVARCHAR(255) NULL", "BannerImageBlobName NVARCHAR(255) NULL"
        ];
        foreach (string section in sections)
            Check(sql.Contains(section), $"Required SQL section is missing: {section}");
        Console.WriteLine("Passed: SQL table, seed and constraint sections.");
    }

    private static void CheckWritingAndHistory(string root)
    {
        string readme = Read(root, "README.md");
        Check(readme.Contains("Organiser") && readme.Contains("Participant"), "README role descriptions are missing.");
        var files = new List<string> { Path.Combine(root, "README.md"), Path.Combine(root, "README-Part1.md") };
        foreach (string folder in new[] { "docs", ".github" })
            files.AddRange(Directory.EnumerateFiles(Path.Combine(root, folder), "*", SearchOption.AllDirectories)
                .Where(file => Path.GetExtension(file) is ".md" or ".yml" or ".sql" or ".svg" or ".drawio"));
        foreach (string file in files)
        {
            string text = File.ReadAllText(file);
            Check(!text.Contains('\u2013') && !text.Contains('\u2014'), $"Use standard punctuation in {Path.GetRelativePath(root, file)}.");
        }
        var start = new ProcessStartInfo("git") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string argument in new[] { "-C", root, "rev-list", "--count", "HEAD" })
            start.ArgumentList.Add(argument);
        using var git = Process.Start(start) ?? throw new InvalidOperationException("Git could not be started.");
        string output = git.StandardOutput.ReadToEnd();
        string error = git.StandardError.ReadToEnd();
        git.WaitForExit();
        Check(git.ExitCode == 0, $"Git history could not be checked: {error.Trim()}");
        Check(int.TryParse(output.Trim(), out int count) && count >= 20, "Expected at least 20 repository commits.");
        Console.WriteLine("Passed: README roles, writing conventions and repository commit count.");
    }
}
