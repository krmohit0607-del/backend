namespace MultiTenantSaaS.Application.Common.Exceptions;

public class ApiException : Exception
{
    public int StatusCode { get; }
    public List<string>? Errors { get; }

    public ApiException(string message, int statusCode = 500, List<string>? errors = null) 
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string name, object key) 
        : base($"Entity \"{name}\" ({key}) was not found.", 404)
    {
    }

    public NotFoundException(string message) 
        : base(message, 404)
    {
    }
}

public class ValidationException : ApiException
{
    public IDictionary<string, string[]> Failures { get; }

    public ValidationException() 
        : base("One or more validation failures have occurred.", 400)
    {
        Failures = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
        : this()
    {
        Failures = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }

    public ValidationException(string propertyName, string errorMessage)
        : this()
    {
        Failures = new Dictionary<string, string[]>
        {
            { propertyName, new[] { errorMessage } }
        };
    }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized access.") 
        : base(message, 401)
    {
    }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "You do not have permission to perform this action.") 
        : base(message, 403)
    {
    }
}
