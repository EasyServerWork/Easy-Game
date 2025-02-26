using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace EasyServer.Core;


public abstract class Response : IDisposable
{
    /// <summary>
    /// Creates a new response representing an exception.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>A new response.</returns>
    public static Response FromException(Exception exception) => new ExceptionResponse { Exception = exception };

    /// <summary>
    /// Creates a new response object which has been fulfilled with the provided value.
    /// </summary>
    /// <typeparam name="TResult">The underlying result type.</typeparam>
    /// <param name="value">The value.</param>
    /// <returns>A new response.</returns>
    public static Response FromResult<TResult>(TResult value)
    {
        var result = ResponsePool.Get<TResult>();
        result.TypedResult = value;
        return result;
    }

    /// <summary>
    /// Gets a completed response.
    /// </summary>
    public static Response Completed => CompletedResponse.Instance;
    
    public abstract object? Result { get; set; }
    public virtual Type? GetSimpleResultType() => null;
    
    public abstract Exception? Exception { get; set; }
    public abstract T GetResult<T>();
    public abstract void Dispose();
    public override string ToString() => Exception is { } ex ? ex.ToString() : Result?.ToString() ?? "[null]";
}


/// <summary>
/// Represents a completed <see cref="Response"/>.
/// </summary>
internal sealed class CompletedResponse : Response
{
    /// <summary>
    /// Gets the singleton instance of this class.
    /// </summary>
    public static CompletedResponse Instance { get; } = new CompletedResponse();
    
    public override object? Result { get => null; set => throw new InvalidOperationException($"Type {nameof(CompletedResponse)} is read-only"); }
    public override Exception? Exception { get => null; set => throw new InvalidOperationException($"Type {nameof(CompletedResponse)} is read-only"); }
    
    public override T GetResult<T>() => default!;
    
    public override void Dispose() { }
    
    public override string ToString() => "[Completed]";
}


/// <summary>
/// A <see cref="Response"/> which represents a typed value.
/// </summary>
/// <typeparam name="TResult">The underlying result type.</typeparam>
internal sealed class Response<TResult> : Response
{
    private TResult? _result;

    public TResult? TypedResult { get => _result; set => _result = value; }

    public override Exception? Exception
    {
        get => null;
        set => throw new InvalidOperationException($"Cannot set {nameof(Exception)} property for type {nameof(Response<TResult>)}");
    }

    public override object? Result
    {
        get => _result;
        set => _result = (TResult?)value;
    }

    public override Type GetSimpleResultType() => typeof(TResult);

    public override T GetResult<T>()
    {
        if (typeof(TResult).IsValueType && typeof(T).IsValueType && typeof(T) == typeof(TResult))
            return Unsafe.As<TResult, T>(ref _result!);

        return (T)(object)_result!;
    }

    public override void Dispose()
    {
        _result = default;
        ResponsePool.Return(this);
    }

    public override string ToString() => _result?.ToString() ?? "[null]";
}

/// <summary>
/// A <see cref="Response"/> which represents an exception, a broken promise.
/// </summary>
internal sealed class ExceptionResponse : Response
{
    /// <inheritdoc/>
    public override object? Result
    {
        get
        {
            ExceptionDispatchInfo.Capture(Exception!).Throw();
            return null;
        }

        set => throw new InvalidOperationException($"Cannot set result property on response of type {nameof(ExceptionResponse)}");
    }

    /// <inheritdoc/>
    public override Exception? Exception { get; set; }

    /// <inheritdoc/>
    public override T GetResult<T>()
    {
        ExceptionDispatchInfo.Capture(Exception!).Throw();
        return default;
    }

    /// <inheritdoc/>
    public override void Dispose() { }

    /// <inheritdoc/>
    public override string ToString() => Exception?.ToString() ?? "[null]";
}