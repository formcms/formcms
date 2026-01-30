// File: Infrastructure/HandlebarsConfiguration.cs  (or similar)

using HandlebarsDotNet;
using System;

public static class HandlebarsConfiguration
{
    private static readonly Lazy<IHandlebars> _handlebars = new Lazy<IHandlebars>(Initialize);

    public static IHandlebars Instance => _handlebars.Value;

    private static IHandlebars Initialize()
    {
        var hb = Handlebars.Create();

        // ───────────────────────────────────────
        //   Register your exact JS helpers in C#
        // ───────────────────────────────────────

        hb.RegisterHelper("gt", (writer, context, parameters) =>
        {
            if (parameters.Length < 2) return;
            var a = Convert.ToDouble(parameters[0]);
            var b = Convert.ToDouble(parameters[1]);
            writer.Write(a > b);
        });

        hb.RegisterHelper("lt", (writer, context, parameters) =>
        {
            if (parameters.Length < 2) return;
            var a = Convert.ToDouble(parameters[0]);
            var b = Convert.ToDouble(parameters[1]);
            writer.Write(a < b);
        });

        hb.RegisterHelper("eq", (writer, context, parameters) =>
        {
            if (parameters.Length < 2) return;
            writer.Write(Equals(parameters[0], parameters[1]));
        });

        // and – all arguments must be truthy (ignores last param = options)
        hb.RegisterHelper("and", (writer, context, parameters) =>
        {
            if (parameters.Length < 2) return;
            bool result = true;
            for (int i = 0; i < parameters.Length - 1; i++)
            {
                result &= IsTruthy(parameters[i]);
            }
            writer.Write(result);
        });

        // or – at least one argument must be truthy
        hb.RegisterHelper("or", (writer, context, parameters) =>
        {
            if (parameters.Length < 2) return;
            bool result = false;
            for (int i = 0; i < parameters.Length - 1; i++)
            {
                if (IsTruthy(parameters[i]))
                {
                    result = true;
                    break;
                }
            }
            writer.Write(result);
        });

        // Optional: many people also like these
        hb.RegisterHelper("ne", (writer, context, p) => writer.Write(!Equals(p[0], p[1])));
        hb.RegisterHelper("gte", (writer, context, p) => writer.Write(Convert.ToDouble(p[0]) >= Convert.ToDouble(p[1])));
        hb.RegisterHelper("lte", (writer, context, p) => writer.Write(Convert.ToDouble(p[0]) <= Convert.ToDouble(p[1])));

        // Bonus: register Handlebars.Net.Helpers categories if useful
        // Handlebars.Net.Helpers.HandlebarsHelpers.Register(hb, Handlebars.Net.Helpers.Category.Math);
        // Handlebars.Net.Helpers.HandlebarsHelpers.Register(hb, Handlebars.Net.Helpers.Category.Boolean);

        return hb;
    }

    private static bool IsTruthy(object? value)
    {
        if (value == null) return false;
        if (value is bool b) return b;
        if (value is string s) return !string.IsNullOrEmpty(s);
        if (value is IConvertible c) return Convert.ToDouble(c) != 0;
        return true; // objects, arrays, etc → truthy
    }
}