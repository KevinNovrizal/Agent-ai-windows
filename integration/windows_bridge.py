"""SSH-only job bridge for the Windows desktop client; no listening port."""
import hashlib, json, os, re, signal, subprocess, sys, zipfile, difflib
from pathlib import Path, PurePosixPath
ROOT=Path('/home/kevin/local-coding-agent')
JOBS=ROOT/'state/windows'
JOBS.mkdir(parents=True,exist_ok=True)
def safe_id(value):
    if not re.fullmatch(r'[a-f0-9]{32}',value): raise ValueError('Invalid job ID')
    return value
def safe_path(value):
    p=PurePosixPath(value)
    if p.is_absolute() or '..' in p.parts or '\\' in value or not p.parts: raise ValueError('Invalid relative path')
    return p
def files(root):
    result={}
    for p in root.rglob('*'):
        if '.git' in p.parts or p.is_symlink() or not p.is_file(): continue
        if p.stat().st_size>5_000_000: continue
        rel=p.relative_to(root).as_posix()
        if any(x in p.parts for x in ('__pycache__','.pytest_cache','node_modules','.venv','venv')): continue
        result[rel]=p.read_bytes()
    return result
def main():
    action=sys.argv[1]
    if action=='init': print(str(JOBS)); return
    job=safe_id(sys.argv[2]); state=JOBS/job; state.mkdir(exist_ok=True)
    if action=='stop':
        (state/'stop').touch()
        pidfile=state/'pid'
        if pidfile.exists():
            pid=int(pidfile.read_text())
            # Guard against recycled PIDs before signaling our own process group.
            cmd=Path(f'/proc/{pid}/cmdline')
            if cmd.exists() and b'/local-coding-agent/agent' in cmd.read_bytes():
                os.killpg(pid,signal.SIGTERM)
        print('Stop requested'); return
    if action!='run': raise ValueError('Unknown action')
    if (state/'stop').exists(): raise RuntimeError('Job canceled before start')
    archive=JOBS/(job+'.zip')
    workspace=Path('/home/kevin/projects/windows')/job
    if workspace.exists(): raise ValueError('Job already exists; use a new ID')
    workspace.mkdir(parents=True)
    with zipfile.ZipFile(archive) as z:
        if sum(i.file_size for i in z.infolist())>100_000_000 or len(z.infolist())>2001: raise ValueError('Workspace exceeds limits')
        request=json.loads(z.read('_request.json'))
        for item in z.infolist():
            if item.filename=='_request.json' or item.is_dir(): continue
            p=safe_path(item.filename)
            if '.git' in p.parts or item.file_size>5_000_000: raise ValueError('Excluded file in archive')
            dest=workspace/str(p); dest.parent.mkdir(parents=True,exist_ok=True); dest.write_bytes(z.read(item))
    before=files(workspace)
    (state/'prompt.txt').write_text(request['prompt'])
    for args in [['init','-q'],['add','.'],['-c','user.name=WindowsSnapshot','-c','user.email=snapshot@localhost','commit','--allow-empty','-qm','Windows workspace snapshot']]:
        subprocess.run(['git','-C',str(workspace),*args],check=True)
    print('Workspace server: '+str(workspace),flush=True)
    if (state/'stop').exists(): raise RuntimeError('Job canceled before generation')
    proc=subprocess.Popen([str(ROOT/'agent'),str(workspace),'--oneshot','--query-file',str(state/'prompt.txt')],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,start_new_session=True)
    (state/'pid').write_text(str(proc.pid))
    with (state/'transcript.txt').open('wb') as log:
        for line in iter(proc.stdout.readline,b''):
            sys.stdout.buffer.write(line); sys.stdout.buffer.flush(); log.write(line)
    code=proc.wait(); (state/'pid').unlink(missing_ok=True)
    after=files(workspace); changes=[]
    with zipfile.ZipFile(state/'result.zip','w',zipfile.ZIP_DEFLATED) as z:
        for name in sorted(before.keys()|after.keys()):
            old=before.get(name); new=after.get(name)
            if old==new: continue
            kind='added' if old is None else 'deleted' if new is None else 'modified'
            diff=''.join(difflib.unified_diff((old or b'').decode('utf-8',errors='replace').splitlines(True),(new or b'').decode('utf-8',errors='replace').splitlines(True),fromfile='a/'+name,tofile='b/'+name))[:100000]
            changes.append({'path':name,'kind':kind,'before_sha256':hashlib.sha256(old).hexdigest() if old is not None else None,'diff':diff})
            if new is not None: z.writestr('files/'+name,new)
        z.writestr('manifest.json',json.dumps({'job':job,'agent_exit_code':code,'changes':changes},ensure_ascii=False))
    print(f'\nPerubahan siap ditinjau: {len(changes)} file. Agent exit: {code}',flush=True)
if __name__=='__main__': main()
