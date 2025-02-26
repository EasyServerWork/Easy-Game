namespace EasyServer.Core;

/// <summary>
/// Represents a fulfillable promise for a response to a request.
/// </summary>
internal interface IResponseCompletionSource
{
    /// <summary>
    /// Sets the result.
    /// </summary>
    /// <param name="value">The result value.</param>
    void Complete(Response value);

    /// <summary>
    /// Sets the result to the default value.
    /// </summary>
    void Complete();

    Type? GetResultType();
}