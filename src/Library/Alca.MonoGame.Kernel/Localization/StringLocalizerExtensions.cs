using Microsoft.Extensions.Localization;

namespace Alca.MonoGame.Kernel.Localization;

/// <summary>Extension methods for IStringLocalizer providing game-oriented helpers.</summary>
public static class StringLocalizerExtensions
{
    /// <summary>Returns a formatted localized string using string.Format semantics.</summary>
    /// <param name="localizer">The localizer instance.</param>
    /// <param name="key">The string key to look up.</param>
    /// <param name="args">Format arguments for string.Format placeholders.</param>
    public static string Get(this IStringLocalizer localizer, string key, params object[] args)
        => string.Format(localizer[key], args);
}
