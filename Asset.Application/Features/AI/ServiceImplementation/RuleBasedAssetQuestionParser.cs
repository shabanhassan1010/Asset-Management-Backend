#region
using Asset.Application.Features.AI.DTos;
using Asset.Application.Features.AI.Enums;
using Asset.Application.Features.AI.Interfases;
using Asset.Domain.Enum;
using System.Text.RegularExpressions;
#endregion

namespace Asset.Application.Features.AI.ServiceImplementation
{
    public class RuleBasedAssetQuestionParser : IAssetQuestionParserService
    {
        #region Fields
        // ReDoS (Regular expression Denial of Service. 
        // After 100ms, regex will throw a timeout exception to avoid performance issues with complex patterns.
        private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(100); 
        private const RegexOptions Options = RegexOptions.IgnoreCase | RegexOptions.Compiled;

        private static readonly Regex DepartmentRegex = new(
            @"\b(?:the\s+)?([A-Za-z][A-Za-z\-]*(?:\s+[A-Za-z][A-Za-z\-]*){0,2})\s+department\b"     
                                                    +
            @"|\bdepartment\s+(?:of\s+)?([A-Za-z][A-Za-z\-]*(?:\s+[A-Za-z][A-Za-z\-]*){0,2})", 
            Options, RegexTimeout);

        private static readonly Regex EmployeeRegex = new(
            @"\b(?:assigned\s+to|allocated\s+to|issued\s+to|given\s+to|belongs?\s+to"
          + @"|belonging\s+to|owned\s+by|held\s+by|used\s+by|under)\s+(?:the\s+)?"
          + @"([A-Za-z]+(?:\s+[A-Za-z]+){0,2})",
            Options, RegexTimeout);

        private static readonly Regex PolitePhrases = new(
             @"\b(?:show|give|tell|find|get|list|bring)\s+me\b" ,
            Options, RegexTimeout);

        private static readonly Regex SelfReference = new(
             @"\b(?:assigned|allocated|issued|given|registered)\s+to\s+me\b"
           + @"|\b(?:for|to|with)\s+me\b"
           + @"|\bmy\s+(?:assets?|devices?|equipment|stuff|name)\b"
           + @"|\bunder\s+my\s+name\b"
           + @"|\bdo\s+i\s+have\b"
           + @"|\b(?:i|me|my|mine|myself)\b"
           , Options, RegexTimeout);

        private static readonly Regex GreetingRegex = new(
             @"^\s*(?:hi|hey|hello|yo|salam|assalamu\s+alaikum|good\s+(?:morning|afternoon|evening|day))\b"
           + @"|\bhow\s+are\s+you\b|\bhow's\s+it\s+going\b|\bwhat's\s+up\b|\bhow\s+are\s+things\b"
           + @"|\bwhat\s+are\s+you\b|\bhow\s+do\s+you\s+(?:work|do)\b"
           + @"|\b(?:thanks|thank\s+you|shukran)\b|\b(?:bye|goodbye|see\s+you)\b"
           + @"|\bwho\s+are\s+you\b|\bwhat\s+can\s+you\s+do\b|\bcan\s+you\s+help\b",
            Options, RegexTimeout);

        private static readonly Regex FollowUpRegex = new(
            @"\b(?:this|these|those|them|the)\s+assets?\b"
          + @"|\b(?:show|list|give)\s+(?:me\s+)?(?:them|those|these)\b"
          + @"|\bwhat\s+about\b|\band\s+the\b",
            Options, RegexTimeout);

        private static readonly string[] KnownAssetTypes =
        {
            "Docking Station", "Access Point", "Conference Phone",
            "Laptop", "Desktop", "Monitor", "Printer", "Scanner", "Server",
            "Projector", "Router", "Switch", "Tablet", "Phone", "Camera",
            "Desk", "Chair", "Cabinet", "Van", "Car", "Vehicle",
            "PC", "Computer", "Keyboard", "Mouse", "Headset", "Screen", "UPS"
        };

        private static readonly string[] KnownManufacturers =
        {
            "Dell", "HP", "Lenovo", "Apple", "Asus", "Acer", "Samsung",
            "Microsoft", "Toshiba", "Canon", "Epson", "Brother", "Logitech",
            "Sony", "LG", "Huawei", "Xiaomi", "IKEA", "Toyota", "Nissan"
        };

        private static readonly HashSet<string> NotAName = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "all", "any", "our", "us", "them", "and", "or", "in", "of",
            "for", "from", "to", "at", "is", "are", "was", "were", "this", "that",
            "available", "assigned", "retired", "maintenance", "anyone", "someone",
            "no", "one", "asset", "assets", "which", "what", "who", "show", "list",
             "me", "my", "mine", "myself", "i", "you", "your", "everyone", "nobody",
             "department", "these", "those", "some", "many", "much"
        };

        #endregion

        #region Public API
        public Task<ParsedAssetQuestion> ParseAsync(string question, ParsedAssetQuestion? previous ,CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(question))
            {
                return Task.FromResult(new ParsedAssetQuestion
                {
                    Intent = AssetQuestionIntent.Unsupported
                });
            }

            // Normalize the question to lowercase for easier matching of keywords and phrases.
            var lower = question.ToLowerInvariant();

            var department    = DetectDepartment(question);
            var isAboutSelf   = DetectSelfReference(question);
            var assetTypeName = DetectAssetType(question);
            var manufacturer  = DetectManufacturer(question);
            var status        = DetectStatus(lower);
            var greeting      = GreetingRegex.IsMatch(question);

            // If the question is not about a department and not about self, try to detect an employee name.
            string? employeeName = null;
            if (department is null && !isAboutSelf)
            {
                employeeName = DetectEmployee(question);
            }
            var hasAnyFilter = assetTypeName is not null || manufacturer is not null|| status is not null|| department is not null|| employeeName is not null || isAboutSelf;

            if (!hasAnyFilter && previous is not null && FollowUpRegex.IsMatch(question))
            {
                return Task.FromResult(new ParsedAssetQuestion
                {
                    AssetTypeName = previous.AssetTypeName,
                    Manufacturer = previous.Manufacturer,
                    Status = previous.Status,
                    DepartmentName = previous.DepartmentName,
                    IsAboutSelf = previous.IsAboutSelf,
                    EmployeeName = previous.EmployeeName,
                    Intent = DetectIntent(lower, true, greeting)
                });
            }

            var intent = DetectIntent(lower, hasAnyFilter, greeting);

            // Now he have the shape of the parsed Question from user
            // and he retuns it into handler to process it and return the result to the user.
            var parsed = new ParsedAssetQuestion
            {
                AssetTypeName = DetectAssetType(question),
                Manufacturer = DetectManufacturer(question),
                Status = DetectStatus(lower),
                DepartmentName = department,
                IsAboutSelf = isAboutSelf,      // If the question is about the user himself, this will be true.
                EmployeeName = employeeName,    // If the question is about a specific employee, this will be their name.
                Intent = intent                 // If the questions has any filter, greeting or not
            };

            return Task.FromResult(parsed);
        }

        #endregion

        #region Detection
        private static AssetQuestionIntent DetectIntent(string lower, bool hasFilter , bool isGreeting)
        {
            // Greeting only, no filters → Greeting
            if (isGreeting && !hasFilter)
                return AssetQuestionIntent.Greeting;

            if (lower.Contains("how many")   || lower.Contains("count")        ||
                lower.Contains("number of")  || lower.Contains("total number") ||
                lower.Contains("total")      || lower.Contains("how much")) 
                return AssetQuestionIntent.CountAssets;

            if (lower.Contains("show")       || lower.Contains("list") ||
                lower.Contains("which")      || lower.Contains("what asset") ||
                lower.Contains("give me")    || lower.Contains("tell me") ||
                lower.Contains("find")       || lower.Contains("display") ||
                lower.Contains("do we have") || lower.Contains("do i have") ||
                lower.Contains("what do")    || lower.Contains("bring me") ||
                lower.Contains("get me")     || lower.Contains("see all") ||
                lower.Contains("view"))
            {
                return AssetQuestionIntent.ListAssets;
            }

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
            // "Show me laptops" -> "laptops" is the asset type we want to detect.
            // We will check if the question contains any of the known asset types, and if so, return that asset type.
            return KnownAssetTypes.FirstOrDefault(type =>
            {
                                          // "Docking Station".Split(' ')
                var pattern = @"\b" + string.Join(@"\s+", type.Split(' ').Select(Regex.Escape)) + @"s?\b";
                return Regex.IsMatch(question, pattern, Options, RegexTimeout);
            });
        }

        private static string? DetectManufacturer(string question)
        {
            return KnownManufacturers.FirstOrDefault(maker =>
                Regex.IsMatch(question, $@"\b{Regex.Escape(maker)}\b", Options, RegexTimeout));
        }

        private static bool DetectSelfReference(string question)
        {
            // [Give me my assigned assets] -> remove "Give me" from the question because he is being PolitePhrases,
            // leaving "my assigned assets" to be checked for self-reference.
            var stripped = PolitePhrases.Replace(question, " ");

            // Check if the stripped question contains any self-reference words like "me", "my", "mine", or "I".
            // and in this case, will return true because the Question is about the user himself (my)
            return SelfReference.IsMatch(stripped);
        }

        private static string? DetectDepartment(string question)
        {
            var match = DepartmentRegex.Match(question);
            if (!match.Success)
                return null;

            string value;
            if (match.Groups[1].Success)
                value = match.Groups[1].Value;
            else if (match.Groups[2].Success)
                value = match.Groups[2].Value;
            else
                return null;

            return CleanName(value);
        }

        private static string? DetectEmployee(string question)
        {
            var match = EmployeeRegex.Match(question);
            if (!match.Success)
                return null;

            return CleanName(match.Groups[1].Value);
        }

        private static string? CleanName(string captured)
        {
            // split "for It" -> ["for", "It"] and remove any leading or trailing words that are in the NotAName set.
            var words = captured.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

            while (words.Count > 0 && NotAName.Contains(words[0]))
                words.RemoveAt(0);

            while (words.Count > 0 && NotAName.Contains(words[^1]))
                words.RemoveAt(words.Count - 1);

            // If all words.count == 0 ---> return null.
            // Otherwise, join the remaining words back into a single string. like ["Human", "Resources"] -> "Human Resources"
            return words.Count == 0 ? null : string.Join(' ', words);
        }

        #endregion
    }
}