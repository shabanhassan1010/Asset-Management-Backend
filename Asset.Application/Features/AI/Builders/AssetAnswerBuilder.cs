using Asset.Application.Interfaces.IRepository;
using Microsoft.Extensions.Localization;

namespace Asset.Application.Features.AI.Builders
{
    // The template answers. They are the "facts" given to Gemini, and the whole answer when Gemini is off.
    //
    // Every method takes the localizer ("t") as its first parameter: the English text is the key,
    // the Arabic is in SharedResources.ar.resx, and {0} is where a value goes. So the answer comes
    // back in the language of the request (Accept-Language) without a second copy of this class.
    public static class AssetAnswerBuilder
    {
        public static string ForCount(IStringLocalizer t, int count)
        {
            return count == 1
                ? t["I found 1 asset matching your question."]
                : t["I found {0} assets matching your question.", count];
        }
        public static string ForList(IStringLocalizer t, int shown, int total)
        {
            if (total == 0)
                return t["I couldn't find any assets matching your question."];

            // Only mention the cap when there actually is one, so the common case
            // reads naturally instead of always saying "showing 3 of 3".
            return shown < total
                ? t["I found {0} assets. Showing the first {1}.", total, shown]
                : total == 1
                    ? t["I found 1 asset matching your question."]
                    : t["I found {0} assets matching your question.", total];
        }
        public static string OutOfScope(IStringLocalizer t)
        {
            // A refusal that teaches. Saying only "I don't understand" leaves the
            // person guessing; naming the shapes that work lets them retry successfully.
            return t["I can only answer questions about looking up assets - for example \"which assets are available?\", \"how many Dell laptops do we have?\", or \"which assets are assigned to the Presales department?\". I can't create, change, transfer or delete anything."];
        }
        public static string UnknownAssetType(IStringLocalizer t, string typeName)
        {
            return t["I don't recognise \"{0}\" as an asset type in the system.", typeName];
        }
        public static string UnknownDepartment(IStringLocalizer t, string departmentName)
        {
            return t["I couldn't find a department called \"{0}\".", departmentName];
        }
        public static string UnknownEmployee(IStringLocalizer t, string name)
        {
            return t["I couldn't find anyone called \"{0}\".", name];
        }
        // The employee exists but has no assets that match (e.g. "Hesham2").
        public static string NoAssetsForEmployee(IStringLocalizer t, string fullName)
        {
            return t["{0} has no assets assigned that match your question.", fullName];
        }

        public static string AmbiguousEmployee(IStringLocalizer t, string name, IReadOnlyList<EmployeeLookup> matches)
        {
            var names = string.Join(", ", matches.Select(m => m.DisplayName));
            return t["More than one person matches \"{0}\": {1}. Which one did you mean?", name, names];
        }
        public static string NoEmployeeLink(IStringLocalizer t)
        {
            // Reached when a non-admin account has no EmployeeId claim.
            // We say what is wrong and who fixes it, without exposing why.
            return t["Your account isn't linked to an employee record yet, so I can't look up assets assigned to you. Please contact an administrator."];
        }
        public static string Greeting(IStringLocalizer t, string lowerQuestion)
        {
            // English and Arabic (formal + Egyptian) phrasings, so a greeting in either language is recognised.
            if (ContainsAny(lowerQuestion, "how are you", "how's it going", "what's up", "كيف حالك", "ازيك", "إزيك", "عامل ايه", "عامل إيه"))
                return t["I'm doing well, thanks for asking - ready whenever you are. How are you doing? And what would you like to know about your assets?"];

            if (ContainsAny(lowerQuestion, "thank", "شكرا", "شكراً", "متشكر"))
                return t["Any time. Anything else you'd like to look up?"];

            if (ContainsAny(lowerQuestion, "bye", "see you", "مع السلامة", "سلام"))
                return t["Goodbye. I'll be here when you need me."];

            if (ContainsAny(lowerQuestion, "who are you", "what can you do", "من أنت", "مين انت", "انت مين", "ماذا تستطيع"))
                return t["I'm the asset assistant. I can look up assets by type, manufacturer, status, department, or the person they're assigned to - and count them. I only read data; I can't change anything."];

            return t["Hi. What would you like to know about your assets?"];
        }

        // Sent with every greeting so the person has somewhere to start.
        // Translated, so a chip clicked in Arabic sends an Arabic question (Gemini understands both).
        public static IReadOnlyList<string> StarterQuestions(IStringLocalizer t) => new string[]
        {
            t["Which assets are currently available?"],
            t["How many Dell laptops do we have?"],
            t["Show me all laptops in the Presales department"],
            t["Which assets are assigned to me?"]
        };
        public static string CannotLookUpOthers(IStringLocalizer t)
        {
            // Silently answering with the caller's own assets would be misleading -
            // they'd think they were looking at someone else's.
            return t["You can only look up assets assigned to you. Try \"which assets are assigned to me?\"."];
        }

        private static bool ContainsAny(string text, params string[] phrases)
        {
            return phrases.Any(text.Contains);
        }
    }
}
