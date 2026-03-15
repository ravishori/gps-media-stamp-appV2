using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;

namespace GpsMediaStamp.Infrastructure.Helpers
{
    /// <summary>
    /// Extracts structured information from exceptions (type, message,
    /// source file, line number, method, inner-exception chain) and
    /// formats a rich HTML email report.
    /// </summary>
    public static class ExceptionHelper
    {
        // ─────────────────────────────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Extract structured detail from an exception, including source file
        /// and line number when PDB symbols are available.
        /// </summary>
        public static ExceptionDetail Extract(Exception ex)
        {
            var detail = new ExceptionDetail
            {
                Timestamp      = DateTime.UtcNow,
                ExceptionType  = ex.GetType().FullName ?? ex.GetType().Name,
                Message        = ex.Message,
                FullStackTrace = ex.StackTrace ?? string.Empty,
                InnerExceptions = BuildInnerChain(ex.InnerException),
            };

            // Try to resolve file/line from the exception's own StackTrace
            // (requires PDB/debug symbols — available in Debug builds and when
            // IncludeAllContentForSelfExtract or PublishDebugSymbols is set)
            try
            {
                var st = new StackTrace(ex, fNeedFileInfo: true);
                var frame = st.GetFrames()?
                              .FirstOrDefault(f => f.GetFileLineNumber() > 0);

                if (frame != null)
                {
                    detail.SourceFile    = frame.GetFileName();
                    detail.LineNumber    = frame.GetFileLineNumber();
                    detail.ColumnNumber  = frame.GetFileColumnNumber();
                    var method           = frame.GetMethod();
                    detail.MethodName    = method?.Name;
                    detail.ClassName     = method?.DeclaringType?.FullName;
                }
                else
                {
                    // PDB not present — grab class/method from the top frame anyway
                    var top = st.GetFrame(0);
                    var method = top?.GetMethod();
                    detail.MethodName = method?.Name;
                    detail.ClassName  = method?.DeclaringType?.FullName;
                }
            }
            catch
            {
                // Reflection can fail in some trimmed/AOT builds — ignore
            }

            return detail;
        }

        /// <summary>
        /// Produce a fully styled HTML email body from an ExceptionDetail
        /// and optional HTTP request information.
        /// </summary>
        public static string FormatHtmlReport(
            ExceptionDetail detail,
            RequestInfo?    req = null)
        {
            var sb = new StringBuilder();

            sb.Append(HtmlHead());

            sb.Append("<h2>&#x1F6A8; GpsMediaStamp &mdash; Unhandled Exception Report</h2>");

            // ── Exception table ──────────────────────────────────────────────
            sb.Append(TableOpen("Exception Details"));
            Row(sb, "Timestamp (UTC)",  detail.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"));
            Row(sb, "Exception Type",   $"<span class='badge'>{Enc(detail.ExceptionType)}</span>");
            Row(sb, "Message",          Enc(detail.Message));
            Row(sb, "Source File",      Enc(detail.SourceFile ?? "<em>N/A — PDB symbols not available</em>"));
            Row(sb, "Line Number",      detail.LineNumber  > 0 ? detail.LineNumber.ToString() : "N/A");
            Row(sb, "Column",           detail.ColumnNumber > 0 ? detail.ColumnNumber.ToString() : "N/A");
            Row(sb, "Class",            Enc(detail.ClassName  ?? "N/A"));
            Row(sb, "Method",           Enc(detail.MethodName ?? "N/A"));
            sb.Append(TableClose());

            // ── Request table ─────────────────────────────────────────────────
            if (req != null)
            {
                sb.Append(TableOpen("HTTP Request"));
                Row(sb, "HTTP Method",   req.HttpMethod  ?? "N/A");
                Row(sb, "Path",          Enc(req.Path));
                Row(sb, "Query String",  Enc(req.QueryString));
                Row(sb, "Client IP",     Enc(req.ClientIp));
                Row(sb, "User Agent",    Enc(req.UserAgent));
                sb.Append(TableClose());
            }

            // ── Inner exception chain ─────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(detail.InnerExceptions))
            {
                sb.Append(TableOpen("Inner Exception Chain"));
                sb.Append($"<tr><td><pre>{Enc(detail.InnerExceptions)}</pre></td></tr>");
                sb.Append(TableClose());
            }

            // ── Full stack trace ──────────────────────────────────────────────
            sb.Append(TableOpen("Full Stack Trace"));
            sb.Append($"<tr><td><pre>{Enc(detail.FullStackTrace)}</pre></td></tr>");
            sb.Append(TableClose());

            sb.Append("</body></html>");
            return sb.ToString();
        }

        // ─────────────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────────────

        private static string BuildInnerChain(Exception? inner)
        {
            if (inner == null) return string.Empty;

            var sb    = new StringBuilder();
            int depth = 1;

            while (inner != null)
            {
                sb.AppendLine($"[Inner #{depth}] {inner.GetType().Name}: {inner.Message}");
                inner = inner.InnerException;
                depth++;
            }

            return sb.ToString();
        }

        private static string HtmlHead() => @"
<html>
<head>
<meta charset='utf-8'/>
<style>
  body  { font-family: Consolas, 'Courier New', monospace; background:#f5f5f5; padding:24px; color:#222; }
  h2    { color:#c0392b; margin-bottom:12px; }
  table { border-collapse:collapse; width:100%; margin-bottom:20px; }
  th    { background:#2c3e50; color:#fff; text-align:left; padding:10px; font-size:14px; }
  td    { padding:9px 12px; border:1px solid #ddd; vertical-align:top; word-break:break-all; font-size:13px; }
  tr:nth-child(even) td { background:#f9f9f9; }
  pre   { background:#1e1e1e; color:#d4d4d4; padding:14px; border-radius:4px;
          overflow-x:auto; white-space:pre-wrap; word-break:break-all; font-size:12px; margin:0; }
  .badge { background:#e74c3c; color:#fff; padding:2px 8px; border-radius:3px; font-size:12px; }
</style>
</head>
<body>
";

        private static string TableOpen(string title) =>
            $"<table><tr><th colspan='2'>{title}</th></tr>";

        private static string TableClose() => "</table>";

        private static void Row(StringBuilder sb, string label, string? value)
        {
            sb.Append("<tr>");
            sb.Append($"<td style='width:180px;font-weight:bold;'>{label}</td>");
            sb.Append($"<td>{value ?? "&nbsp;"}</td>");
            sb.Append("</tr>");
        }

        /// <summary>HTML-encode a value so it renders safely in an email.</summary>
        private static string Enc(string? text)
            => WebUtility.HtmlEncode(text ?? string.Empty);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Data classes
    // ─────────────────────────────────────────────────────────────────────────

    public class ExceptionDetail
    {
        public DateTime Timestamp      { get; set; }
        public string?  ExceptionType  { get; set; }
        public string?  Message        { get; set; }
        public string   FullStackTrace { get; set; } = string.Empty;
        public string?  InnerExceptions { get; set; }
        public string?  SourceFile     { get; set; }
        public int      LineNumber     { get; set; }
        public int      ColumnNumber   { get; set; }
        public string?  ClassName      { get; set; }
        public string?  MethodName     { get; set; }
    }

    public class RequestInfo
    {
        public string? HttpMethod  { get; set; }
        public string? Path        { get; set; }
        public string? QueryString { get; set; }
        public string? ClientIp    { get; set; }
        public string? UserAgent   { get; set; }
    }
}
