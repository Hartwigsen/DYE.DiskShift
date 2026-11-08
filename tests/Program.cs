using DiskShift;
using System.Diagnostics;
var root = Path.GetFullPath(args[0]);
var qemu = Environment.GetEnvironmentVariable("DISKSHIFT_QEMU_TEST_ENGINE") ?? Path.Combine(root, "Release-0.2.0", "qemu", "qemu-img.exe");
var engine = new ConverterEngine(qemu);
var folder = Path.Combine(root, "test-results", DateTime.Now.ToString("yyyyMMdd-HHmmss"));
Directory.CreateDirectory(folder);
var results = new List<string>();
async Task Check(string name, Func<Task> test) { try { await test(); results.Add("PASS " + name); } catch(Exception ex) { results.Add("FAIL " + name + ": " + ex.Message); } }
var raw = Path.Combine(folder, "Quelle mit Leerzeichen.raw");
using(var file = File.Create(raw)) { var bytes = new byte[1024*1024]; new Random(42).NextBytes(bytes); for(var i=0;i<16;i++) file.Write(bytes); }
var sourceHash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(raw)));
await Check("Paket-QEMU startet", async () => { if (!(await engine.Version(default)).Contains("qemu-img version")) throw new Exception("Version unerwartet"); });
foreach(var format in new[]{"vhdx", "vmdk", "qcow2"})
    await Check("RAW nach " + format + " mit Datenvergleich", async () => { var dest=Path.Combine(folder,"Ergebnis."+format); await engine.Convert(raw,dest,format,true,new Progress<(double,string)>(),default); var info=await engine.Inspect(dest,default); if(info.VirtualSize!=16*1024*1024) throw new Exception("Falsche Kapazität"); });
await Check("VHDX nach QCOW2", async () => { await engine.Convert(Path.Combine(folder,"Ergebnis.vhdx"),Path.Combine(folder,"Rückweg.qcow2"),"qcow2",true,new Progress<(double,string)>(),default); });
await Check("Vorhandenes Ziel wird abgewiesen", async () => { try { await engine.Convert(raw,Path.Combine(folder,"Ergebnis.vhdx"),"vhdx",false,new Progress<(double,string)>(),default); } catch(IOException) { return; } throw new Exception("Ziel überschrieben"); });
await Check("Quelle als Ziel wird abgewiesen", async () => { try { await engine.Convert(raw,raw,"qcow2",false,new Progress<(double,string)>(),default); } catch(IOException) { return; } throw new Exception("Quelle akzeptiert"); });
await Check("Abbruch eines laufenden Vorgangs", async () => {
    using var cancel = new CancellationTokenSource();
    var target=Path.Combine(folder,"Abbruch.qcow2");
    var progress=new ImmediateProgress(p => { if(p.Item2.StartsWith("Konvertierung")) cancel.Cancel(); });
    try { await engine.Convert(raw,target,"qcow2",true,progress,cancel.Token); } catch(OperationCanceledException) { if(File.Exists(target)) throw new Exception("Fertiges Ziel nach Abbruch"); return; }
    throw new Exception("Kein Abbruch");
});
await Check("Snapshot-Kette abgewiesen", async () => {
    var backing=Path.Combine(folder,"Kette.qcow2"); var start=new ProcessStartInfo(qemu){UseShellExecute=false,CreateNoWindow=true};
    foreach(var a in new[]{"create","-f","qcow2","-F","raw","-b",raw,backing}) start.ArgumentList.Add(a);
    using var p=Process.Start(start)!; await p.WaitForExitAsync();
    try { await engine.Inspect(backing,default); } catch(IOException) { return; } throw new Exception("Kette akzeptiert");
});
await Check("Quelldaten unverändert", () => { if(sourceHash!=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(raw)))) throw new Exception("Hash verändert"); return Task.CompletedTask; });
await Check("Keine temporären Restordner", () => { if(Directory.GetDirectories(folder,".diskshift-*").Length!=0) throw new Exception("Restordner vorhanden"); return Task.CompletedTask; });
if(args.Length > 1) await Check("Reales Parallels-Image nur lesend erkennen", async () => { var info = await engine.Inspect(args[1], default); if(info.Format != "parallels") throw new Exception("Kein Parallels-Image"); });
await Check("Updatevergleich erkennt neuere Version", () => { var r=Updates.Parse("[{\"draft\":false,\"tag_name\":\"v0.2.4\",\"html_url\":\"https://github.com/Hartwigsen/DYE.DiskShift/releases/tag/v0.2.4\"}]",new Version(0,2,3)); if(r.Url==null) throw new Exception("Update fehlt"); return Task.CompletedTask; });
await Check("Gleiche Version ist kein Update", () => { var r=Updates.Parse("[{\"draft\":false,\"tag_name\":\"v0.2.3\",\"html_url\":\"https://github.com/Hartwigsen/DYE.DiskShift/releases/tag/v0.2.3\"}]",new Version(0,2,3)); if(r.Url!=null) throw new Exception("Falsches Update"); return Task.CompletedTask; });
await Check("Fremde Release-URL wird ignoriert", () => { var r=Updates.Parse("[{\"draft\":false,\"tag_name\":\"v9.0.0\",\"html_url\":\"https://example.com/release\"}]",new Version(0,2,3)); if(r.Url!=null) throw new Exception("Fremdes Ziel"); return Task.CompletedTask; });
await Check("Support-Client erkennt Empfangsbestätigung", async () => { var r=await SupportClient.Send(new{id=Guid.NewGuid()},new string('x',48),new ReplyHandler(202,"{\"id\":\"671c6730-79cb-4a54-8b76-c5904a28e49e\",\"message\":\"Angenommen\"}")); if(!r.Contains("Angenommen"))throw new Exception("Bestätigung fehlt"); });
await Check("Support-Client weist ungültigen Code vor Netzwerk ab", async () => { try { await SupportClient.Send(new{},"kurz",new ReplyHandler(202,"{}")); } catch(IOException) {return;} throw new Exception("Code akzeptiert"); });
await Check("Support-Client behandelt HTML statt JSON", async () => { try { await SupportClient.Send(new{},new string('x',48),new ReplyHandler(200,"<html>nicht eingerichtet</html>")); } catch(IOException) {return;} throw new Exception("HTML akzeptiert"); });
await Check("Support-Client zeigt Zugriffssperre", async () => { try { await SupportClient.Send(new{},new string('x',48),new ReplyHandler(401,"{\"message\":\"Code gesperrt\"}")); } catch(IOException ex) {if(ex.Message.Contains("gesperrt"))return;} throw new Exception("Sperre fehlt"); });
await Check("Komprimiertes QCOW2 nach VHDX mit Datenvergleich", async () => {
    var pattern=Path.Combine(folder,"Muster.raw"); await File.WriteAllBytesAsync(pattern,Enumerable.Repeat((byte)77,4*1024*1024).ToArray());
    var compressed=Path.Combine(folder,"Komprimiert.qcow2"); var start=new ProcessStartInfo(qemu){UseShellExecute=false,CreateNoWindow=true};
    foreach(var a in new[]{"convert","-c","-f","raw","-O","qcow2",pattern,compressed}) start.ArgumentList.Add(a);
    using var p=Process.Start(start)!; await p.WaitForExitAsync(); if(p.ExitCode!=0)throw new Exception("Komprimierung fehlgeschlagen");
    await engine.Convert(compressed,Path.Combine(folder,"Dekomprimiert.vhdx"),"vhdx",true,new Progress<(double,string)>(),default);
});
await File.WriteAllLinesAsync(Path.Combine(folder,"results.txt"),results);
foreach(var result in results) Console.WriteLine(result);
Environment.ExitCode=results.Any(r=>r.StartsWith("FAIL"))?1:0;
sealed class ImmediateProgress(Action<(double,string)> callback):IProgress<(double,string)> { public void Report((double,string) p)=>callback(p); }




sealed class ReplyHandler(int code,string body):System.Net.Http.HttpMessageHandler
{
 protected override Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken token)=>Task.FromResult(new System.Net.Http.HttpResponseMessage((System.Net.HttpStatusCode)code){Content=new System.Net.Http.StringContent(body)});
}

