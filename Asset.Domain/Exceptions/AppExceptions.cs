namespace Asset.Domain.Exceptions;

// Every app exception carries:
//   Message = the English text, with {0}, {1} where values go   e.g. "Department {0} does not exist."
//   Args    = those values                                       e.g. [ 7 ]
//
// Why not $"Department {id} does not exist." like before?
// The English text is also the KEY of the Arabic translation (SharedResources.ar.resx).
// With the value already inside, the text is different for every id and can never be found.
// ExceptionHandlingMiddleware translates the template first, then puts the values in.
// (The Domain project knows nothing about languages — it only keeps the two pieces.)
public abstract class AppException(string message, object[] args) : Exception(message)
{
    public object[] Args { get; } = args;
}

// Asset was not found → API returns 404.
public class NotFoundException(string message, params object[] args) : AppException(message, args);

// A business rule was violated → API returns 422.
public class BusinessException(string message, params object[] args) : AppException(message, args);

// Duplicate/conflict → API returns 409.  -. Use for redendency - Duplicate
public class ConflictException(string message, params object[] args) : AppException(message, args);
public class AuthenticationFailedException(string message, params object[] args) : AppException(message, args);
public class ConcurrencyException(string message, params object[] args) : AppException(message, args);
