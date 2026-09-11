using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;

public class Change { public string path; public string kind; public string before_sha256; public string diff; }
public class Result { public string job; public int agent_exit_code; public Change[] changes; }
public class Settings { public string host; public string user; public string key; public string known_hosts; }
public class Backup { public string folder; public Change[] changes; public Dictionary<string,string> after; }

public partial class AgentLokal : Form {
    static readonly JavaScriptSerializer Json = new JavaScriptSerializer { MaxJsonLength=20000000 };
    static readonly string AppDir=AppDomain.CurrentDomain.BaseDirectory;
    static readonly string State=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AgentLokal","state");
    Settings cfg;
    readonly TextBox folder=new TextBox(), prompt=new TextBox();
    readonly RichTextBox transcript=new RichTextBox(), diff=new RichTextBox();
    readonly ListBox changes=new ListBox();
    readonly Label status=new Label();
    readonly Button run=new Button(), stop=new Button(), apply=new Button(), rollback=new Button(), browse=new Button();
    readonly TabControl tabs=new TabControl();
    Result result; string job,activeFolder,resultZip,lastBackup; bool busy; volatile bool canceled;
    public AgentLokal() {
        cfg=File.Exists(ConfigPath)?Json.Deserialize<Settings>(File.ReadAllText(ConfigPath)):new Settings();
        Directory.CreateDirectory(State);
        Text="Agent Lokal · Coding di Windows"; Width=1120; Height=800; MinimumSize=new Size(820,620);
        StartPosition=FormStartPosition.CenterScreen; BackColor=Color.FromArgb(246,248,250); Font=new Font("Segoe UI",10);
        var layout=new TableLayoutPanel { Dock=DockStyle.Fill, RowCount=5,ColumnCount=1,Padding=new Padding(20) };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute,55)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,45));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent,100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,125)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute,42));
        Controls.Add(layout);
        var title=new Label { Text="Agent Lokal", Font=new Font("Segoe UI",22,FontStyle.Bold),AutoSize=true,ForeColor=Color.FromArgb(18,57,65) };
        var heading=new Panel { Dock=DockStyle.Fill }; heading.Controls.Add(title);
        var connection=new Button { Text="Koneksi / pindah PC",AutoSize=true,Dock=DockStyle.Right }; connection.Click+=(s,e)=>{if(!busy)ConnectionDialog();};heading.Controls.Add(connection);layout.Controls.Add(heading,0,0);
        var project=new TableLayoutPanel { Dock=DockStyle.Fill,ColumnCount=2 }; project.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100)); project.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,145));
        folder.Dock=DockStyle.Fill; folder.ReadOnly=true; folder.PlaceholderCompat();
        browse.Text="Pilih folder…"; browse.Dock=DockStyle.Fill; browse.Click+=(s,e)=>{using(var d=new FolderBrowserDialog()){ d.Description="Pilih proyek yang akan dikerjakan agent";if(d.ShowDialog()==DialogResult.OK)folder.Text=d.SelectedPath;}};
        project.Controls.Add(folder,0,0); project.Controls.Add(browse,1,0); layout.Controls.Add(project,0,1);
        tabs.Dock=DockStyle.Fill; var chatTab=new TabPage("Percakapan & proses"); var changesTab=new TabPage("Tinjau perubahan"); tabs.TabPages.Add(chatTab); tabs.TabPages.Add(changesTab);
        transcript.Dock=DockStyle.Fill; transcript.ReadOnly=true; transcript.BackColor=Color.White; transcript.BorderStyle=BorderStyle.None; transcript.Font=new Font("Consolas",10);
        transcript.Text="Pilih folder proyek dan tulis instruksi bahasa Indonesia.\n\nModel berjalan di server lokal. File asli di Windows baru berubah setelah Anda memilih Terapkan perubahan.\n\nFolder .git, dependensi, file rahasia umum, dan file lebih dari 5 MB tidak dikirim. Batas: 2.000 file / 100 MB.\n"; chatTab.Controls.Add(transcript);
        var split=new SplitContainer {Width=1000,Dock=DockStyle.Fill,SplitterDistance=280}; changes.Dock=DockStyle.Fill; diff.Dock=DockStyle.Fill;diff.ReadOnly=true;diff.Font=new Font("Consolas",10);diff.BackColor=Color.White;
        changes.SelectedIndexChanged+=(s,e)=>{if(result!=null&&changes.SelectedIndex>=0)diff.Text=result.changes[changes.SelectedIndex].diff;};
        split.Panel1.Controls.Add(changes);split.Panel2.Controls.Add(diff);changesTab.Controls.Add(split);layout.Controls.Add(tabs,0,2);
        prompt.Multiline=true;prompt.Dock=DockStyle.Fill;prompt.ScrollBars=ScrollBars.Vertical;prompt.Text="Periksa proyek ini dan jalankan tesnya.";layout.Controls.Add(prompt,0,3);
        var bottom=new FlowLayoutPanel { Dock=DockStyle.Fill,FlowDirection=FlowDirection.LeftToRight,WrapContents=false };
        run.Text="Jalankan";stop.Text="Hentikan";apply.Text="Terapkan perubahan";rollback.Text="Rollback terakhir";
        foreach(var b in new[]{run,stop,apply,rollback}){b.AutoSize=true;b.Height=32;bottom.Controls.Add(b);}
        status.AutoSize=true;status.Padding=new Padding(12,7,0,0);status.Text="Siap · SSH "+cfg.host;bottom.Controls.Add(status);layout.Controls.Add(bottom,0,4);
        run.Click+=async(s,e)=>await StartJob();stop.Click+=async(s,e)=>await StopJob();apply.Click+=(s,e)=>ApplyChanges();rollback.Click+=(s,e)=>Rollback();
        stop.Enabled=false;apply.Enabled=false;rollback.Enabled=false;
        string saved=Path.Combine(State,"last-backup.txt");if(File.Exists(saved)){lastBackup=File.ReadAllText(saved);rollback.Enabled=Directory.Exists(lastBackup);}
        FormClosing+=(s,e)=>{if(busy){e.Cancel=true;MessageBox.Show("Hentikan tugas terlebih dahulu sebelum menutup aplikasi.");}};
    }
    void UI(Action action){if(!IsDisposed)BeginInvoke(action);}
    void Log(string line){UI(()=>{transcript.AppendText(Regex.Replace(line,@"\x1B\[[0-?]*[ -/]*[@-~]","")+Environment.NewLine);transcript.SelectionStart=transcript.TextLength;transcript.ScrollToCaret();});}
    static string Q(string s){return "\""+s.Replace("\"","\\\"")+"\"";}
    string SSHArgs(string command){return "-i "+Q(cfg.key)+" -o BatchMode=yes -o ConnectTimeout=15 -o ServerAliveInterval=15 -o ServerAliveCountMax=3 -o StrictHostKeyChecking=yes -o UserKnownHostsFile="+Q(cfg.known_hosts)+" "+cfg.user+"@"+cfg.host+" "+Q(command);}
    int Execute(string exe,string args,bool log=true){
        using(var p=new Process()){
            p.StartInfo=new ProcessStartInfo(exe,args){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,StandardOutputEncoding=Encoding.UTF8,StandardErrorEncoding=Encoding.UTF8};
            p.OutputDataReceived+=(s,e)=>{if(log&&e.Data!=null)Log(e.Data);};p.ErrorDataReceived+=(s,e)=>{if(log&&e.Data!=null)Log(e.Data);};
            p.Start();p.BeginOutputReadLine();p.BeginErrorReadLine();p.WaitForExit();return p.ExitCode;
        }
    }
    string SSH {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"OpenSSH","ssh.exe");}}
    string SCP {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System),"OpenSSH","scp.exe");}}
    void Transfer(string source,string dest){
        string args="-i "+Q(cfg.key)+" -o BatchMode=yes -o ConnectTimeout=15 -o StrictHostKeyChecking=yes -o UserKnownHostsFile="+Q(cfg.known_hosts)+" "+Q(source)+" "+Q(dest);
        if(Execute(SCP,args)!=0)throw new Exception("Transfer SSH gagal. Lihat keluaran proses.");
    }
    static bool Excluded(string path){
        string name=Path.GetFileName(path).ToLowerInvariant();
        return new[]{".git","node_modules",".venv","venv","__pycache__",".pytest_cache",".ssh",".aws",".azure",".codex","dist","build"}.Contains(name)||name==".env"||name.StartsWith(".env.")||name.StartsWith("id_rsa")||name.StartsWith("id_ed25519")||Regex.IsMatch(name,@"\.(pem|pfx|p12|key)$")||name=="credentials";
    }
    static IEnumerable<string> Walk(string dir){foreach(string path in Directory.EnumerateFileSystemEntries(dir)){if(Excluded(path)||(File.GetAttributes(path)&FileAttributes.ReparsePoint)!=0)continue;if(Directory.Exists(path)){foreach(string f in Walk(path))yield return f;}else yield return path;}}
    static string Hash(byte[] bytes){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
    public static string SafePath(string root,string relative){
        if(Path.IsPathRooted(relative)||relative.Contains(":")||relative.Split('/','\\').Any(p=>p==".."||p==".git"))throw new Exception("Path hasil tidak aman: "+relative);
        string full=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar)));
        string prefix=Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;
        if(!full.StartsWith(prefix,StringComparison.OrdinalIgnoreCase))throw new Exception("Path keluar workspace");
        string current=root; foreach(string part in relative.Split('/','\\')){current=Path.Combine(current,part);if((File.Exists(current)||Directory.Exists(current))&&(File.GetAttributes(current)&FileAttributes.ReparsePoint)!=0)throw new Exception("Symlink/reparse point ditolak: "+current);}
        return full;
    }
    async Task StartJob(){
        if(busy)return;if(string.IsNullOrEmpty(cfg.host)){ConnectionDialog();return;}if(!Directory.Exists(folder.Text)||string.IsNullOrWhiteSpace(prompt.Text)){MessageBox.Show("Pilih folder dan isi instruksi terlebih dahulu.");return;}
        activeFolder=Path.GetFullPath(folder.Text);if(activeFolder.TrimEnd('\\').Equals(Path.GetPathRoot(activeFolder).TrimEnd('\\'),StringComparison.OrdinalIgnoreCase)){MessageBox.Show("Pilih folder proyek, bukan seluruh drive.");return;}
        string taskPrompt=prompt.Text;job=Guid.NewGuid().ToString("N");string local=Path.Combine(State,job);Directory.CreateDirectory(local);resultZip=Path.Combine(local,"result.zip");
        canceled=false;result=null;changes.Items.Clear();diff.Clear();busy=true;run.Enabled=false;browse.Enabled=false;apply.Enabled=false;stop.Enabled=true;status.Text="Mengirim proyek…";tabs.SelectedIndex=0;
        Log("\nANDA: "+taskPrompt+"\n");
        try{
            await Task.Run(()=>{
                string zip=Path.Combine(local,"request.zip");int count=0,skipped=0;long total=0;
                using(var z=ZipFile.Open(zip,ZipArchiveMode.Create)){
                    foreach(string file in Walk(activeFolder)){
                        if(canceled)throw new Exception("Tugas dibatalkan.");
                        long size=new FileInfo(file).Length;if(size>5000000){skipped++;continue;}total+=size;count++;if(count>2000||total>100000000)throw new Exception("Proyek melewati batas 2.000 file / 100 MB.");
                        string rel=file.Substring(activeFolder.TrimEnd('\\').Length+1).Replace('\\','/');if(rel=="_request.json")throw new Exception("Nama _request.json dicadangkan aplikasi.");
                        z.CreateEntryFromFile(file,rel,CompressionLevel.Fastest);
                    }
                    using(var w=new StreamWriter(z.CreateEntry("_request.json").Open(),new UTF8Encoding(false)))w.Write(Json.Serialize(new{prompt=taskPrompt}));
                }
                Log("Snapshot: "+count+" file, "+(total/1024)+" KB. File besar dilewati: "+skipped+".");
                if(canceled)throw new Exception("Tugas dibatalkan.");
                if(Execute(SSH,SSHArgs("python3 /home/kevin/local-coding-agent/integration/windows_bridge.py init"),false)!=0)throw new Exception("Tidak bisa mengakses server SSH.");
                Transfer(zip,cfg.user+"@"+cfg.host+":/home/kevin/local-coding-agent/state/windows/"+job+".zip");
                if(canceled)throw new Exception("Tugas dibatalkan.");
                UI(()=>status.Text="Agent sedang bekerja…");
                int code=Execute(SSH,SSHArgs("python3 /home/kevin/local-coding-agent/integration/windows_bridge.py run "+job));
                if(code!=0)Log("Proses bridge berakhir dengan kode "+code+". Mencoba mengambil hasil yang tersedia.");
                Transfer(cfg.user+"@"+cfg.host+":/home/kevin/local-coding-agent/state/windows/"+job+"/result.zip",resultZip);
            });
            using(var z=ZipFile.OpenRead(resultZip))using(var r=new StreamReader(z.GetEntry("manifest.json").Open()))result=Json.Deserialize<Result>(r.ReadToEnd());
            foreach(var c in result.changes)changes.Items.Add(c.kind+" · "+c.path);
            apply.Enabled=result.changes.Length>0;status.Text=(result.agent_exit_code==0?"Selesai":"Agent berhenti ("+result.agent_exit_code+")")+" · "+result.changes.Length+" perubahan";if(result.changes.Length>0){tabs.SelectedIndex=1;changes.SelectedIndex=0;}
        }catch(Exception ex){Log("ERROR: "+ex.Message);status.Text="Gagal / dihentikan";}
        finally{busy=false;run.Enabled=true;browse.Enabled=true;stop.Enabled=false;}
    }
    async Task StopJob(){if(!busy)return;canceled=true;stop.Enabled=false;status.Text="Menghentikan…";await Task.Run(()=>Execute(SSH,SSHArgs("python3 /home/kevin/local-coding-agent/integration/windows_bridge.py stop "+job)));}
    void ApplyChanges(bool confirm=true){
        if(result==null||busy)return;
        try{
            foreach(var c in result.changes){string p=SafePath(activeFolder,c.path);string actual=File.Exists(p)?Hash(File.ReadAllBytes(p)):null;if(actual!=c.before_sha256)throw new Exception("Konflik: "+c.path+" berubah sejak snapshot. Tidak ada perubahan diterapkan.");}
            if(confirm&&MessageBox.Show("Terapkan "+result.changes.Length+" perubahan ke:\n"+activeFolder+"\n\nVersi sebelumnya akan dicadangkan.","Tinjau & terapkan",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            var content=new Dictionary<string,byte[]>();
            using(var z=ZipFile.OpenRead(resultZip))foreach(var c in result.changes){if(c.kind=="deleted"){content[c.path]=null;continue;}var entry=z.GetEntry("files/"+c.path);if(entry==null||entry.Length>5000000)throw new Exception("Isi hasil tidak lengkap/terlalu besar: "+c.path);using(var src=entry.Open())using(var mem=new MemoryStream()){src.CopyTo(mem);content[c.path]=mem.ToArray();}}
            string backup=Path.Combine(State,"backups",result.job);Directory.CreateDirectory(backup);var info=new Backup{folder=activeFolder,changes=result.changes,after=new Dictionary<string,string>()};
            foreach(var c in result.changes){string p=SafePath(activeFolder,c.path);if(File.Exists(p)){string b=SafePath(Path.Combine(backup,"files"),c.path);Directory.CreateDirectory(Path.GetDirectoryName(b));File.Copy(p,b,true);}}
            foreach(var c in result.changes)info.after[c.path]=content[c.path]==null?null:Hash(content[c.path]);
            File.WriteAllText(Path.Combine(backup,"_backup.json"),Json.Serialize(info));
            var attempted=new List<Change>();
            try{foreach(var c in result.changes){string p=SafePath(activeFolder,c.path);attempted.Add(c);if(content[c.path]==null)File.Delete(p);else{Directory.CreateDirectory(Path.GetDirectoryName(p));File.WriteAllBytes(p,content[c.path]);}}}
            catch{foreach(var c in attempted){string p=SafePath(activeFolder,c.path);if(c.before_sha256==null)File.Delete(p);else File.Copy(SafePath(Path.Combine(backup,"files"),c.path),p,true);}throw;}
            File.WriteAllText(Path.Combine(backup,"_backup.json"),Json.Serialize(info));lastBackup=backup;File.WriteAllText(Path.Combine(State,"last-backup.txt"),backup);rollback.Enabled=true;apply.Enabled=false;status.Text="Perubahan diterapkan";Log("Perubahan diterapkan ke "+activeFolder+". Cadangan: "+backup);
        }catch(Exception ex){if(!confirm)throw;MessageBox.Show(ex.Message,"Tidak dapat menerapkan",MessageBoxButtons.OK,MessageBoxIcon.Error);}
    }
    void Rollback(bool confirm=true){
        try{
            if(busy||string.IsNullOrEmpty(lastBackup))return;var info=Json.Deserialize<Backup>(File.ReadAllText(Path.Combine(lastBackup,"_backup.json")));
            foreach(var c in info.changes){string p=SafePath(info.folder,c.path);string actual=File.Exists(p)?Hash(File.ReadAllBytes(p)):null;if(actual!=info.after[c.path])throw new Exception("Rollback ditahan: "+c.path+" sudah diedit lagi.");}
            if(confirm&&MessageBox.Show("Kembalikan perubahan terakhir di "+info.folder+"?","Rollback",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            foreach(var c in info.changes){string p=SafePath(info.folder,c.path);if(c.before_sha256==null)File.Delete(p);else{Directory.CreateDirectory(Path.GetDirectoryName(p));File.Copy(SafePath(Path.Combine(lastBackup,"files"),c.path),p,true);}}
            rollback.Enabled=false;File.Delete(Path.Combine(State,"last-backup.txt"));status.Text="Rollback selesai";
        }catch(Exception ex){if(!confirm)throw;MessageBox.Show(ex.Message,"Rollback tidak dijalankan");}
    }
    [STAThread] public static void Main(string[] args){
        if(args.Contains("--self-test")){
            string root=Path.Combine(Path.GetTempPath(),"AgentLokal-path-test");Directory.CreateDirectory(root);
            SafePath(root,"src/main.py");bool blocked=false;try{SafePath(root,"../outside.txt");}catch{blocked=true;}if(!blocked)Environment.Exit(2);
            if(Hash(Encoding.UTF8.GetBytes("abc"))!="ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")Environment.Exit(3);
            var parsed=Json.Deserialize<Result>("{\"job\":\"test\",\"changes\":[]}");if(parsed.job!="test"||parsed.changes.Length!=0)Environment.Exit(4);
            CryptoTest();File.WriteAllText(Path.Combine(AppDir,"self-test.txt"),"PASS: path confinement; JSON; SHA256; encrypted profile roundtrip and tamper rejection");return;
        }
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        try{
            var app=new AgentLokal();
            if(args.Contains("--ui-smoke")){app.Shown+=(s,e)=>{using(var bmp=new Bitmap(app.Width,app.Height)){app.DrawToBitmap(bmp,new Rectangle(Point.Empty,app.Size));bmp.Save(Path.Combine(AppDir,"ui-preview.png"));}app.Close();};}
            if(args.Length==2&&args[0]=="--workflow-test"){
                app.folder.Text=args[1];app.prompt.Text="Buat modul greet.py berisi fungsi greet(name) yang mengembalikan Halo, diikuti nama dan tanda seru. Buat test_greet.py dengan unittest untuk dua nama, lalu jalankan tes. Kerjakan melalui alat.";
                app.Shown+=async(s,e)=>{try{await app.StartJob();if(app.result==null||app.result.agent_exit_code!=0||app.result.changes.Length<2)throw new Exception("Agent did not produce expected files");app.ApplyChanges(false);foreach(var c in app.result.changes)if(c.kind!="deleted"&&!File.Exists(SafePath(app.activeFolder,c.path)))throw new Exception("Apply failed");app.Rollback(false);foreach(var c in app.result.changes){string p=SafePath(app.activeFolder,c.path);if((File.Exists(p)?Hash(File.ReadAllBytes(p)):null)!=c.before_sha256)throw new Exception("Rollback failed");}File.WriteAllText(Path.Combine(AppDir,"workflow-test.json"),Json.Serialize(app.result));File.WriteAllText(Path.Combine(AppDir,"workflow-validation.txt"),"PASS: Windows snapshot, SSH, real agent, result review, apply and rollback");}catch(Exception ex){File.WriteAllText(Path.Combine(AppDir,"workflow-validation.txt"),"FAIL: "+ex.ToString());}File.WriteAllText(Path.Combine(AppDir,"workflow-transcript.txt"),app.transcript.Text);using(var bmp=new Bitmap(app.Width,app.Height)){app.DrawToBitmap(bmp,new Rectangle(Point.Empty,app.Size));bmp.Save(Path.Combine(AppDir,"workflow-preview.png"));}app.Close();};
            }
            Application.Run(app);
        }catch(Exception e){MessageBox.Show(e.Message,"Agent Lokal");}
    }
}
public static class Compat {public static void PlaceholderCompat(this TextBox box){box.Text="";}}

