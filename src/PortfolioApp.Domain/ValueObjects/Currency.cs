using PortfolioApp.Domain.Exceptions;

namespace PortfolioApp.Domain.ValueObjects;

/// <summary>
/// An ISO 4217 currency, identified by its three-letter code (e.g. "USD"). A value object:
/// two instances are equal when their codes match, and an instance can only be created
/// through <see cref="From"/>, which guarantees the code is recognised. Persisted as its
/// three-character <see cref="Code"/> string via an EF Core value converter.
/// </summary>
public sealed record Currency
{
    /// <summary>The validated, upper-cased ISO 4217 code.</summary>
    public string Code { get; }

    private Currency(string code) => Code = code;

    /// <summary>
    /// Creates a <see cref="Currency"/> from a code, normalising case/whitespace.
    /// </summary>
    /// <exception cref="DomainException">
    /// Thrown when the code is empty or not a recognised ISO 4217 currency.
    /// </exception>
    public static Currency From(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new DomainException("A currency code must be provided.");
        }

        string normalized = code.Trim().ToUpperInvariant();

        if (!Iso4217.Codes.Contains(normalized))
        {
            throw new DomainException($"'{code}' is not a recognised ISO 4217 currency code.");
        }

        return new Currency(normalized);
    }

    /// <summary>US dollar — the default portfolio base currency.</summary>
    public static readonly Currency Usd = From("USD");

    public override string ToString() => Code;
}
