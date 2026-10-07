using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace BobsBookstoreClassic.Data
{
    /// <summary>
    /// Static configuration accessor for Bookstore settings.
    /// Must be initialized with Initialize(IConfiguration) at application startup (in Program.cs).
    /// </summary>
    public sealed class BookstoreConfiguration
    {
        private static IConfiguration _configuration;
        private static readonly Dictionary<string, string> _overrides = new();

        /// <summary>
        /// Initialize from ASP.NET Core IConfiguration. Call once at startup.
        /// </summary>
        public static void Initialize(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public static void AddSetting(string key, string value)
        {
            _overrides[key] = value;
        }

        public static string GetSetting(string key)
        {
            if (_overrides.TryGetValue(key, out var overrideValue))
                return overrideValue;

            if (_configuration == null)
                throw new InvalidOperationException("BookstoreConfiguration has not been initialized. Call Initialize(IConfiguration) at startup.");

            // Support both flat key "Services/Authentication" and nested "Services:Authentication"
            var normalizedKey = key.Replace('/', ':');
            return _configuration[normalizedKey] ?? string.Empty;
        }

        public static T GetSetting<T>(string key)
        {
            var value = GetSetting(key);
            return (T)Convert.ChangeType(value, typeof(T));
        }

        public static void AddConnectionString(string key, string value)
        {
            _overrides[$"ConnectionStrings:{key}"] = value;
        }

        public static string GetConnectionString(string key)
        {
            if (_overrides.TryGetValue($"ConnectionStrings:{key}", out var overrideValue))
                return overrideValue;

            if (_configuration == null)
                throw new InvalidOperationException("BookstoreConfiguration has not been initialized. Call Initialize(IConfiguration) at startup.");

            return _configuration.GetConnectionString(key) ?? string.Empty;
        }
    }
}
