namespace Asset.Application.Features.Employees
{
    // Employee codes are made by the server, never typed by the user: EMP-001, EMP-002, ...
    // The next code = the biggest number already used + 1.
    public static class EmployeeCodes
    {
        public const string Prefix = "EMP-";

        // Builds the next code from the codes that already exist.
        //   [ "EMP-001", "EMP-007", "EMP-008" ]  →  "EMP-009"
        //   [ ]                                  →  "EMP-001"
        //   [ "EMP-999" ]                        →  "EMP-1000"  (D3 = at least 3 digits, more when needed)
        // A code that is not "EMP-" + digits (e.g. "SW") is ignored, so it can't break the numbering.
        public static string Next(IEnumerable<string> existingCodes)
        {
            var biggest = 0;

            foreach (var code in existingCodes)
            {
                var digits = code.Substring(Prefix.Length);
                if (int.TryParse(digits, out var number) && number > biggest)
                    biggest = number;
            }

            return $"{Prefix}{biggest + 1:D3}";
        }
    }
}

/*
     Admin clicks "+ New employee"
              │
              ▼
     GET /employees/next-code  ──▶  "EMP-009"   (shown in the disabled field)
              │
     Admin clicks Save
              ▼
     POST /employees  ──▶  the server works the code out AGAIN and saves it
                          (someone else may have saved EMP-009 in the meantime → this one gets EMP-010)
*/
