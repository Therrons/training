using fraud_poc_project.CustomAttributes;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace fraud_poc_project.Middleware
{
    // checks if query string or request content has potential XSS attack,
    // an additional filter has been applied to the endpoint to check for XSS attacks
    // , if the filter is not applied, this middleware will not check for XSS attacks
    public class CheckForXssMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _statusCode = (int)HttpStatusCode.BadRequest;

        public CheckForXssMiddleware(RequestDelegate next)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get the endpoint from the current request
            var endpoint = context.GetEndpoint();

            // Check if the endpoint has the ValidateXss attribute
            var hasValidateXssAttribute = endpoint?.Metadata
                .GetMetadata<ValidateXssAttribute>() != null;

            if (hasValidateXssAttribute)
            {
                var endPointPath = (bool)(context?.Request?.Path.HasValue) ? context.Request.Path.Value : "";

                if (!string.IsNullOrWhiteSpace(endPointPath))
                {
                    // Check XSS in URL
                    if (!string.IsNullOrWhiteSpace(context.Request.Path.Value))
                    {
                        var url = context.Request.Path.Value;

                        if (XSSValidation.IsPotentialXSS(url))
                        {
                            await FlagAsError(context).ConfigureAwait(false);
                            return;
                        }
                    }

                    // Check XSS in query string
                    if (!string.IsNullOrWhiteSpace(context.Request.QueryString.Value))
                    {
                        var queryString = WebUtility.UrlDecode(context.Request.QueryString.Value);

                        if (XSSValidation.IsPotentialXSS(queryString))
                        {
                            await FlagAsError(context).ConfigureAwait(false);
                            return;
                        }
                    }

                    // Check XSS in request content
                    var content = await ReadRequestBody(context);

                    if (XSSValidation.IsPotentialXSS(content))
                    {
                        await FlagAsError(context).ConfigureAwait(false);
                        return;
                    }
                }
            }
            await _next(context).ConfigureAwait(false);
        }

        private static async Task<string> ReadRequestBody(HttpContext context)
        {
            var buffer = new MemoryStream();
            await context.Request.Body.CopyToAsync(buffer);
            context.Request.Body = buffer;
            buffer.Position = 0;

            var encoding = Encoding.UTF8;

            var requestContent = await new StreamReader(buffer, encoding).ReadToEndAsync();
            context.Request.Body.Position = 0;

            return requestContent;
        }

        private async Task FlagAsError(HttpContext context)
        {
            context.Response.Headers.Add("XXSValidation", "Failed - Potential XSS attack");
            context.Response.StatusCode = _statusCode;
        }
    }

    public static class XSSValidation
    {
        private static readonly char[] RedFlagChars = { '<', '&', '%' };

        private static bool IsAtoZ(char c)
        {
            return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');
        }

        public static bool IsPotentialXSS(string s)
        {
            var strLen = s.Length - 1;

            for (int i = 0; ;)
            {
                // Look for the start of one of our patterns
                var n = s.IndexOfAny(RedFlagChars, i);

                // If not found, the string is safe
                if (n < 0) return false;

                // If it's the last char, it's safe
                if (n == strLen) return false;

                switch (s[n])
                {
                    case '<':
                        // If the < is followed by a letter or '!', it's unsafe (looks like a tag or HTML comment)
                        if (IsAtoZ(s[n + 1]) || s[n + 1] == '!' || s[n + 1] == '/' || s[n + 1] == '?') return true;
                        break;

                    case '&':
                        // If the & is followed by a #, it's unsafe (e.g. S)
                        if (s[n + 1] == '#') return true;
                        break;

                    case '%':
                        // If the % is followed by '3C', then this may be a HTML Encoded string - which is a '<'
                        if ((n + 4) < strLen)
                        {
                            var startTag = s.Substring(n, 3); // make it lower case to ensure we check irrespective of casing
                            return (startTag.ToLower() == "%3c") && (IsAtoZ(s[n + 3]) || s[n + 3] == '!' || s[n + 3] == '/' || s[n + 3] == '?');
                        }
                        return false;
                }
                i++;
                if (i >= strLen) break;
            }
            return false;
        }
    }
}