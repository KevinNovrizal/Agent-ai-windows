using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Forms;
using System.Drawing;
public class ConnectionProfile { public string host; public string user; public string private_key; public string known_hosts; }
public partial class AgentLokal {
    static string ConfigDir {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"AgentLokal");}}
    static string ConfigPath {get{return Path.Combine(ConfigDir,"settings.json");}}
    static byte[] Protect(byte[] data,string password,bool decrypt){
        byte[] magic=Encoding.ASCII.GetBytes("AGLOK001"),salt=new byte[16],iv=new byte[16];
        if(decrypt){if(data.Length<88||!data.Take(8).SequenceEqual(magic))throw new Exception("Format profil tidak dikenal.");Array.Copy(data,8,salt,0,16);Array.Copy(data,24,iv,0,16);}
        else using(var rng=RandomNumberGenerator.Create()){rng.GetBytes(salt);rng.GetBytes(iv);}
        using(var derive=new Rfc2898DeriveBytes(password,salt,200000)){
            byte[] enc=derive.GetBytes(32),auth=derive.GetBytes(32);
            using(var aes=Aes.Create())using(var hmac=new HMACSHA256(auth)){
                aes.Key=enc;aes.IV=iv;aes.Mode=CipherMode.CBC;aes.Padding=PaddingMode.PKCS7;
                if(decrypt){byte[] body=data.Take(data.Length-32).ToArray(),tag=hmac.ComputeHash(body);int difference=0;for(int i=0;i<32;i++)difference|=tag[i]^data[body.Length+i];if(difference!=0)throw new Exception("Password salah atau profil rusak.");using(var transform=aes.CreateDecryptor())return transform.TransformFinalBlock(data,40,data.Length-72);}
                using(var transform=aes.CreateEncryptor()){byte[] cipher=transform.TransformFinalBlock(data,0,data.Length);byte[] body=magic.Concat(salt).Concat(iv).Concat(cipher).ToArray();return body.Concat(hmac.ComputeHash(body)).ToArray();}
            }
        }
    }
    static void CryptoTest(){byte[] plain=Encoding.UTF8.GetBytes("dummy key for test only"),sealedData=Protect(plain,"test password only",false);if(!Protect(sealedData,"test password only",true).SequenceEqual(plain))throw new Exception("Crypto roundtrip failed");sealedData[45]^=1;bool rejected=false;try{Protect(sealedData,"test password only",true);}catch{rejected=true;}if(!rejected)throw new Exception("Tampering accepted");}
    static string Password(bool exporting){using(var d=new Form {Text=exporting?"Lindungi profil koneksi":"Buka profil koneksi",Width=430,Height=215,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
        var label=new Label {Text=exporting?"Buat password minimal 12 karakter. Simpan sendiri;\npassword diperlukan saat impor di PC lain.":"Masukkan password yang dibuat saat ekspor.",Left=16,Top=15,Width=390,Height=45};var input=new TextBox {Left=16,Top=65,Width=380,UseSystemPasswordChar=true};var ok=new Button {Text="Lanjut",Left=285,Top=115,Width=110,DialogResult=DialogResult.OK};d.Controls.AddRange(new Control[]{label,input,ok});d.AcceptButton=ok;if(d.ShowDialog()!=DialogResult.OK)return null;if(exporting&&input.Text.Length<12)throw new Exception("Gunakan password minimal 12 karakter.");return input.Text;}}
    static void ValidateConnection(string host,string user){if(string.IsNullOrEmpty(host)||!System.Text.RegularExpressions.Regex.IsMatch(host,@"^[a-zA-Z0-9][a-zA-Z0-9.\-]*$")||string.IsNullOrEmpty(user)||!System.Text.RegularExpressions.Regex.IsMatch(user,@"^[a-zA-Z_][a-zA-Z0-9_\-]*$"))throw new Exception("Alamat server atau username tidak valid.");}
    void SaveProfile(ConnectionProfile profile){
        ValidateConnection(profile.host,profile.user);if(string.IsNullOrEmpty(profile.private_key)||!profile.private_key.Contains("PRIVATE KEY")||string.IsNullOrWhiteSpace(profile.known_hosts))throw new Exception("Kunci atau identitas server tidak lengkap.");
        Directory.CreateDirectory(ConfigDir);string key=Path.Combine(ConfigDir,"connection-"+Guid.NewGuid().ToString("N")+".key");
        var acl=new FileSecurity();acl.SetAccessRuleProtection(true,false);var sid=WindowsIdentity.GetCurrent().User;acl.SetOwner(sid);acl.AddAccessRule(new FileSystemAccessRule(sid,FileSystemRights.FullControl,AccessControlType.Allow));
        using(var stream=new FileStream(key,FileMode.CreateNew,FileSystemRights.Write,FileShare.None,4096,FileOptions.None,acl))using(var w=new StreamWriter(stream,new UTF8Encoding(false)))w.Write(profile.private_key);
        string hosts=key+".hosts";File.WriteAllText(hosts,profile.known_hosts,new UTF8Encoding(false));
        cfg=new Settings {host=profile.host,user=profile.user,key=key,known_hosts=hosts};File.WriteAllText(ConfigPath,Json.Serialize(cfg));status.Text="Siap · SSH "+cfg.host;
    }
    void ConnectionDialog(){using(var d=new Form {Text="Koneksi dan pindah laptop / PC",Width=520,Height=320,StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false}){
        var label=new Label {Left=20,Top=15,Width=465,Height=90,Text="Server: "+(cfg.host??"belum diatur")+"\n\nPC baru: impor profil dari PC lama. Model dan memori tetap di server. Salin folder proyek secara terpisah. Kedua PC harus bisa menjangkau server melalui jaringan lokal/VPN."};
        var export=new Button {Text="Ekspor koneksi…",Left=20,Top=115,Width=220,Height=36,Enabled=!string.IsNullOrEmpty(cfg.key)};var import=new Button {Text="Impor koneksi…",Left=255,Top=115,Width=220,Height=36};var test=new Button {Text="Tes koneksi",Left=20,Top=165,Width=220,Height=36};
        var resultLabel=new Label {Left=20,Top=215,Width=460,Height=45};d.Controls.AddRange(new Control[]{label,export,import,test,resultLabel});
        export.Click+=(s,e)=>{try{string password=Password(true);if(password==null)return;using(var file=new SaveFileDialog {Filter="Profil Agent Lokal|*.agentlokal",FileName="koneksi-agent.agentlokal"})if(file.ShowDialog()==DialogResult.OK){var profile=new ConnectionProfile {host=cfg.host,user=cfg.user,private_key=File.ReadAllText(cfg.key),known_hosts=File.ReadAllText(cfg.known_hosts)};File.WriteAllBytes(file.FileName,Protect(Encoding.UTF8.GetBytes(Json.Serialize(profile)),password,false));resultLabel.Text="Profil terenkripsi tersimpan. Password tidak disimpan.";}}catch(Exception ex){MessageBox.Show(ex.Message);}};
        import.Click+=(s,e)=>{try{using(var file=new OpenFileDialog {Filter="Profil Agent Lokal|*.agentlokal"})if(file.ShowDialog()==DialogResult.OK){if(new FileInfo(file.FileName).Length>1000000)throw new Exception("Profil terlalu besar.");string password=Password(false);if(password==null)return;SaveProfile(Json.Deserialize<ConnectionProfile>(Encoding.UTF8.GetString(Protect(File.ReadAllBytes(file.FileName),password,true))));d.Close();}}catch(Exception ex){MessageBox.Show(ex.Message);}};
        test.Click+=async(s,e)=>{test.Enabled=false;try{ValidateConnection(cfg.host,cfg.user);resultLabel.Text="Memeriksa SSH dan model…";int code=await System.Threading.Tasks.Task.Run(()=>Execute(SSH,SSHArgs("/home/kevin/local-coding-agent/health-check")));resultLabel.Text=code==0?"Terhubung. Server dan model siap.":"Belum siap. Lihat tab Percakapan & proses.";}catch(Exception ex){resultLabel.Text=ex.Message;}finally{test.Enabled=true;}};
        d.ShowDialog(this);
    }}
}
