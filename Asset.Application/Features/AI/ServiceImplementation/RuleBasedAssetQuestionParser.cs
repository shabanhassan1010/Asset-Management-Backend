#region
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.Enums.DTos;
using Asset.Application.Features.AI.Interfases;
using Asset.Domain.Enum;
using System.Text.RegularExpressions;
#endregion

namespace Asset.Application.Features.AI.ServiceImplementation
{
    // Deterministic stand-in for a language model.
    // It never builds SQL and never builds a string that reaches the database -
    // it only fills properties on a DTO. Everything it does not recognise is dropped.
    public class RuleBasedAssetQuestionParser : IAssetQuestionParser
    {
        #region Fields

        // Every regex gets a timeout. Without one, a crafted input can make the
        // engine backtrack for a very long time and pin a CPU core (ReDoS).
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100);

        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        // Up to three words before "department", so "Human Resources" and
        // "Information Technology" resolve - a single-word capture returned
        // "Resources" and then failed the lookup.
        // The engine tries the longest capture first and backs off, so
        // "in the Human Resources department" yields "Human Resources", not "Human".
        private static readonly Regex DepartmentRegex = new(
            @"\b(?:the\s+)?([A-Za-z][A-Za-z\-]*(?:\s+[A-Za-z][A-Za-z\-]*){0,2})\s+department\b"
          + @"|\bdepartment\s+(?:of\s+)?([A-Za-z][A-Za-z\-]*(?:\s+[A-Za-z][A-Za-z\-]*){0,2})",
            Options, RegexTimeout);

        // "assigned to Ahmed", "assigned to Ahmed Kamal", "belongs to Sara"
        private static readonly Regex EmployeeRegex = new(
            @"\b(?:assigned\s+to|belongs?\s+to|belonging\s+to|owned\s+by|held\s+by)\s+(?:the\s+)?"
          + @"([A-Za-z]+(?:\s+[A-Za-z]+){0,2})",
            Options, RegexTimeout);

        // "show me" is not a self-reference, it is a way of saying "display".
        // These phrases are stripped before we look for me / my / mine, otherwise
        // every "show me all laptops" would be read as a question about the caller.
        private static readonly Regex PolitePhrases = new(
            @"\b(?:show|give|tell|find|get|list|bring)\s+me\b",
            Options, RegexTimeout);

        private static readonly Regex SelfReference = new(
            @"\b(?:me|my|mine|i)\b",
            Options, RegexTimeout);

        private static readonly Regex GreetingRegex = new(
            @"^\s*(?:hi|hey|hello|yo|salam|good\s+(?:morning|afternoon|evening|day))\b"
          + @"|\bhow\s+are\s+you\b|\bhow's\s+it\s+going\b|\bwhat's\s+up\b"
          + @"|\b(?:thanks|thank\s+you|shukran)\b|\b(?:bye|goodbye|see\s+you)\b"
          + @"|\bwho\s+are\s+you\b|\bwhat\s+can\s+you\s+do\b",
            Options, RegexTimeout);

        // The stub's vocabulary, ordered longest-first so "Docking Station" is tried
        // before any single word inside it. These are guesses about the question text
        // only - the handler still resolves each one against AssetTypes.TypeName, so a
        // value that is not in the database produces a friendly answer, not a crash.
        private static readonly string[] KnownAssetTypes =
        {
            "Docking Station", "Access Point", "Conference Phone",
            "Laptop", "Desktop", "Monitor", "Printer", "Scanner", "Server",
            "Projector", "Router", "Switch", "Tablet", "Phone", "Camera",
            "Desk", "Chair", "Cabinet", "Van", "Car", "Vehicle"
        };

        private static readonly string[] KnownManufacturers =
        {
            "Dell", "HP", "Lenovo", "Apple", "Asus", "Acer", "Samsung",
            "Microsoft", "Toshiba", "Canon", "Epson", "Brother", "Logitech",
            "Sony", "LG", "Huawei", "Xiaomi", "IKEA", "Toyota", "Nissan"
        };

        // Words that are never a person's or a department's name. Guards the captures
        // against filler like "the", "all", or a status word.
        private static readonly HashSet<string> NotAName = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "all", "any", "our", "us", "them", "and", "or", "in", "of",
            "for", "from", "to", "at", "is", "are", "was", "were", "this", "that",
            "available", "assigned", "retired", "maintenance", "anyone", "someone",
            "no", "one", "asset", "assets", "which", "what", "who", "show", "list"
        };

        #endregion

        #region Public API

        public Task<ParsedAssetQuestion> ParseAsync(string question, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Task.FromResult(new ParsedAssetQuestion
                {
                    Intent = AssetQuestionIntent.Unsupported
                });
            }

            // Lowercase copy for the simple Contains checks. The regexes run against
            // the original text so captured names keep their casing.
            var lower = question.ToLowerInvariant();

            var department = DetectDepartment(question);
            var isAboutSelf = DetectSelfReference(question);

            var parsed = new ParsedAssetQuestion
            {
                AssetTypeName = DetectAssetType(question),
                Manufacturer = DetectManufacturer(question),
                Status = DetectStatus(lower),
                DepartmentName = department,
                IsAboutSelf = isAboutSelf,

                // Order matters twice over.
                // "assigned to the Presales department" would otherwise capture "the"
                // as a person and send us hunting for someone who does not exist, so a
                // question naming a department is never also read as naming a person.
                // And "assigned to me" is a self-reference, not a colleague named "me".
                EmployeeName = department is null && !isAboutSelf
                    ? DetectEmployee(question)
                    : null,

                Intent = AssetQuestionIntent.Unsupported
            };

            // Intent is decided last so it can fall back on whether any filter was
            // actually recognised - see DetectIntent.
            parsed = parsed with
            {
                Intent = DetectIntent(lower, parsed.HasAnyFilter, GreetingRegex.IsMatch(question))
            };
            return Task.FromResult(parsed);
        }

        #endregion

        #region Detection

        private static AssetQuestionIntent DetectIntent(string lower, bool hasFilter , bool isGreeting)
        {
            if (isGreeting && !hasFilter)
                return AssetQuestionIntent.Greeting;

            if (lower.Contains("how many") || lower.Contains("count") || lower.Contains("number of") || lower.Contains("total number"))
                return AssetQuestionIntent.CountAssets;

            if (lower.Contains("how many") || lower.Contains("count") || lower.Contains("number of") || lower.Contains("total number"))
            {
                return AssetQuestionIntent.CountAssets;
            }

            if (lower.Contains("show")       ||
                lower.Contains("list")       ||
                lower.Contains("which")      ||
                lower.Contains("what asset") ||
                lower.Contains("give me")    ||
                lower.Contains("tell me")    ||
                lower.Contains("find")       ||
                lower.Contains("display")    ||
                lower.Contains("do we have") ||
                lower.Contains("do i have"))
            {
                return AssetQuestionIntent.ListAssets;
            }

            // Fallback: if the sentence named something concrete we can filter on -
            // a type, a manufacturer, a status, a department, a person - then listing
            // those assets answers it, whatever verb was used. This is what makes
            // "Dell laptops in Presales" and "available printers" work without
            // needing every possible phrasing in the list above.
            //
            // It is safe precisely because it requires a filter: a sentence with no
            // recognised filter still falls through to Unsupported.
            if (hasFilter)
            {
                return AssetQuestionIntent.ListAssets;
            }

            return AssetQuestionIntent.Unsupported;
        }

        private static AssetStatus? DetectStatus(string lower)
        {
            if (lower.Contains("maintenance") || lower.Contains("repair"))
                return AssetStatus.UnderMaintenance;

            if (lower.Contains("available") || lower.Contains("unassigned")
                || lower.Contains("free") || lower.Contains("spare"))
                return AssetStatus.Available;

            if (lower.Contains("retired") || lower.Contains("disposed"))
                return AssetStatus.Retired;

            if (lower.Contains("assigned") || lower.Contains("in use"))
                return AssetStatus.Assigned;

            return null;
        }

        private static string? DetectAssetType(string question)
        {
            // Matches singular and plural, and tolerates any spacing inside a
            // multi-word type ("docking station" / "docking  stations").
            return KnownAssetTypes.FirstOrDefault(type =>
            {
                var pattern = @"\b" + string.Join(@"\s+", type.Split(' ').Select(Regex.Escape)) + @"s?\b";
                return Regex.IsMatch(question, pattern, Options, RegexTimeout);
            });
        }

        private static string? DetectManufacturer(string question)
        {
            // Word boundaries matter here. Without \b, "hp" would match inside other words.
            return KnownManufacturers.FirstOrDefault(maker =>
                Regex.IsMatch(question, $@"\b{Regex.Escape(maker)}\b", Options, RegexTimeout));
        }

        private static bool DetectSelfReference(string question)
        {
            // Remove "show me" and friends first, then see if a first-person word
            // survives. "Show me all laptops" loses its only "me" and is correctly
            // read as a general question; "show me my laptops" keeps "my".
            var stripped = PolitePhrases.Replace(question, " ");

            return SelfReference.IsMatch(stripped);
        }

        private static string? DetectDepartment(string question)
        {
            var match = DepartmentRegex.Match(question);
            if (!match.Success)
                return null;

            // The pattern has two alternatives, so take whichever group captured.
            var value = match.Groups[1].Success
                ? match.Groups[1].Value
                : match.Groups[2].Value;

            return CleanName(value);
        }

        private static string? DetectEmployee(string question)
        {
            var match = EmployeeRegex.Match(question);

            return match.Success ? CleanName(match.Groups[1].Value) : null;
        }

        // A capture can pull in filler at either end ("in the Human Resources",
        // "Ahmed and"). Trim non-name words from both sides and keep what is left.
        private static string? CleanName(string captured)
        {
            var words = captured
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .ToList();

            while (words.Count > 0 && NotAName.Contains(words[0]))
                words.RemoveAt(0);

            while (words.Count > 0 && NotAName.Contains(words[^1]))
                words.RemoveAt(words.Count - 1);

            return words.Count == 0 ? null : string.Join(' ', words);
        }

        #endregion
    }
}