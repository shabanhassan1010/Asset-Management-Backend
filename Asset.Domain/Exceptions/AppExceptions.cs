namespace Asset.Domain.Exceptions;


// Asset was not found → API returns 404.
public class NotFoundException(string message) : Exception(message);

// A business rule was violated → API returns 422.
public class BusinessException(string message) : Exception(message);

// Duplicate/conflict → API returns 409.  -. Use for redendency - Duplicate
public class ConflictException(string message) : Exception(message);
public class AuthenticationFailedException(string message) : Exception(message);
public class ConcurrencyException(string message) : Exception(message) { }
