using Asset.Application.Interfaces.IRepository;
namespace Asset.Application.Features.AI.ServiceImplementation
{
    public static class AssetAnswerBuilder
    {
        public static string ForCount(int count)
        {
            return count == 1
                ? "I found 1 asset matching your question."
                : $"I found {count} assets matching your question.";
        }
        public static string ForList(int shown, int total)
        {
            if (total == 0)
                return "I couldn't find any assets matching your question.";

            // Only mention the cap when there actually is one, so the common case
            // reads naturally instead of always saying "showing 3 of 3".
            return shown < total
                ? $"I found {total} assets. Showing the first {shown}."
                : total == 1
                    ? "I found 1 asset matching your question."
                    : $"I found {total} assets matching your question.";
        }
        public static string OutOfScope()
        {
            // A refusal that teaches. Saying only "I don't understand" leaves the
            // person guessing; naming the shapes that work lets them retry successfully.
            return "I can only answer questions about looking up assets - for example "
                 + "\"which assets are available?\", \"how many Dell laptops do we have?\", "
                 + "or \"which assets are assigned to the Presales department?\". "
                 + "I can't create, change, transfer or delete anything.";
        }
        public static string UnknownAssetType(string typeName)
        {
            return $"I don't recognise \"{typeName}\" as an asset type in the system.";
        }        
        public static string UnknownDepartment(string departmentName)
        {
            return $"I couldn't find a department called \"{departmentName}\".";
        }          
        public static string UnknownEmployee(string name)
        {
            return $"I couldn't find anyone called \"{name}\".";
        }
        public static string AmbiguousEmployee(string name, IReadOnlyList<EmployeeLookup> matches)
        {
            var names = string.Join(", ", matches.Select(m => m.FullName));
            return $"More than one person matches \"{name}\": {names}. Which one did you mean?";
        }
        public static string NoEmployeeLink()
        {
            // Reached when a non-admin account has no EmployeeId claim.
            // We say what is wrong and who fixes it, without exposing why.
            return "Your account isn't linked to an employee record yet, "
                 + "so I can't look up assets assigned to you. Please contact an administrator.";
        }
        public static string Greeting(string lowerQuestion)
        {
            if (lowerQuestion.Contains("how are you") || lowerQuestion.Contains("how's it going")
                || lowerQuestion.Contains("what's up"))
            {
                return "I'm doing well, thanks for asking - ready whenever you are. "
                     + "How are you doing? And what would you like to know about your assets?";
            }

            if (lowerQuestion.Contains("thank"))
                return "Any time. Anything else you'd like to look up?";

            if (lowerQuestion.Contains("bye") || lowerQuestion.Contains("see you"))
                return "Goodbye. I'll be here when you need me.";

            if (lowerQuestion.Contains("who are you") || lowerQuestion.Contains("what can you do"))
            {
                return "I'm the asset assistant. I can look up assets by type, manufacturer, "
                     + "status, department, or the person they're assigned to - and count them. "
                     + "I only read data; I can't change anything.";
            }

            return "Hi. What would you like to know about your assets?";
        }

        // Sent with every greeting so the person has somewhere to start.
        public static IReadOnlyList<string> StarterQuestions() => new[]
        {
            "Which assets are currently available?",
            "How many Dell laptops do we have?",
            "Show me all laptops in the Presales department",
            "Which assets are assigned to me?"
        };
    }
}