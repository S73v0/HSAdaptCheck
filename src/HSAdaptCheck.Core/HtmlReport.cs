using System.Linq;
using System.Net;
using System.Text;
namespace HSAdaptCheck.Core
{
    public static class HtmlReport
    {
        static string E(string x) { return WebUtility.HtmlEncode(x??"unavailable"); }
        public static string Render(Report r)
        {
            var s=new StringBuilder("<!doctype html><html lang='en'><meta charset='utf-8'><title>HSAdaptCheck snapshot</title><style>body{font:15px sans-serif;margin:2em}table{border-collapse:collapse;width:100%}td,th{border:1px solid #bbb;padding:8px;text-align:left;vertical-align:top} .FAIL{background:#ffd6d6}.REVIEW{background:#fff0c2}.NOT_VERIFIED{background:#e6e6ef}.PASS{background:#d9f3df}pre{white-space:pre-wrap;overflow-wrap:anywhere}</style><h1>HSAdaptCheck — development prototype</h1>");
            s.Append("<p>"+E(r.Notice)+"</p><p>Checker "+E(r.CheckerVersion)+" · configuration "+E(r.ConfigurationVersion)+" · report UTC "+E(r.CreatedUtc)+" · capture UTC "+E(r.Dataset.CapturedUtc)+"</p>");
            s.Append("<p>"+string.Join(" · ",System.Enum.GetValues(typeof(Status)).Cast<Status>().Select(x=>x+": "+r.Results.Count(y=>y.Status==x)))+". No overall treatment-release decision.</p>");
            s.Append("<table><tr><th>Check</th><th>Status</th><th>Observed</th><th>Expected</th><th>Reason</th><th>Source</th></tr>");
            foreach(var x in r.Results) s.Append("<tr class='"+x.Status+"'><td>"+E(x.Check)+"</td><td>"+x.Status.ToString().Replace("_"," ")+"</td><td>"+E(x.Observed)+"</td><td>"+E(x.Expected)+"</td><td>"+E(x.Reason)+"</td><td>"+E(x.Source)+"</td></tr>");
            s.Append("</table><h2>Snapshot identifiers and configuration</h2><p>Potentially sensitive clinical identifiers. Store only in approved locations. Source: "+E(r.Dataset.Source)+"</p><p>Snapshot SHA-256: "+E(r.SnapshotSha256)+"<br>Configuration SHA-256: "+E(r.ConfigurationSha256)+"</p><pre>"+E(Json.Serialize(r.Dataset))+"</pre><pre>"+E(Json.Serialize(r.Configuration))+"</pre><p>Outside scope: ARIA workflow/signatures, scheduling, delivered-fraction reconciliation, Mobius integration, continuous monitoring, deformable dose accumulation, automatic clinical contour assessment and treatment-release interlocks.</p></html>");
            return s.ToString();
        }
    }
}
