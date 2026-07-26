namespace RentalApp.Contracts;

public sealed record ApiError(string Error, IReadOnlyDictionary<string, string[]>? ValidationErrors = null);
