using System;
using System.IO;
using System.Linq;
using HSAdaptCheck.Core;
if(args.Length!=3) { Console.Error.WriteLine("Usage: HSAdaptCheck.Cli snapshot.json config.json output-directory (new directory required)"); return 2; }
try {
    var report=Checker.Run(Json.Read<Snapshot>(args[0]),Json.Read<Configuration>(args[1]));
    if(Directory.Exists(args[2])||File.Exists(args[2])) throw new IOException("Output already exists; choose a new snapshot directory.");
    Directory.CreateDirectory(args[2]);Json.Write(Path.Combine(args[2],"report.json"),report);File.WriteAllText(Path.Combine(args[2],"report.html"),HtmlReport.Render(report));
    foreach(var status in Enum.GetValues<Status>()) Console.WriteLine(status+": "+report.Results.Count(x=>x.Status==status));
    Console.WriteLine(report.Notice);
    return report.Results.Any(x=>x.Status==Status.FAIL)?1:report.Results.Any(x=>x.Status==Status.NOT_VERIFIED)?3:0;
} catch(Exception ex) { Console.Error.WriteLine("Report failed ("+ex.GetType().Name+"). Check input schema, file access and output location; no readiness claim."); return 2; }
