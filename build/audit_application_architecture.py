#!/usr/bin/env python3
from __future__ import annotations
from dataclasses import dataclass
from pathlib import Path
import argparse, json, re, sys

@dataclass
class TypeDecl:
    name: str; kind: str; start: int; open: int; close: int
@dataclass
class MethodDecl:
    name: str; signature: str; return_type: str; modifiers: str; access: str; start: int; body_start: int; end: int; expr: bool; type_name: str

def mask_csharp(text: str) -> str:
    out=list(text); i=0; n=len(text); state='code'; raw_quotes=0
    while i<n:
        c=text[i]; nxt=text[i+1] if i+1<n else ''
        if state=='code':
            if c=='/' and nxt=='/': out[i]=out[i+1]=' '; i+=2; state='line'; continue
            if c=='/' and nxt=='*': out[i]=out[i+1]=' '; i+=2; state='block'; continue
            if c=='"':
                q=1
                while i+q<n and text[i+q]=='"': q+=1
                if q>=3:
                    raw_quotes=q
                    for j in range(i,i+q): out[j]=' '
                    i+=q; state='raw'; continue
                out[i]=' '; i+=1; state='string'; continue
            if c=="'": out[i]=' '; i+=1; state='char'; continue
            i+=1; continue
        if state=='line':
            if c=='\n': state='code'
            else: out[i]=' '
            i+=1; continue
        if state=='block':
            out[i]=' '
            if c=='*' and nxt=='/': out[i+1]=' '; i+=2; state='code'
            else: i+=1
            continue
        if state=='string':
            out[i]=' '
            if c=='\\' and i+1<n: out[i+1]=' '; i+=2
            elif c=='"': i+=1; state='code'
            else: i+=1
            continue
        if state=='char':
            out[i]=' '
            if c=='\\' and i+1<n: out[i+1]=' '; i+=2
            elif c=="'": i+=1; state='code'
            else: i+=1
            continue
        if state=='raw':
            out[i]=' '
            if c=='"':
                q=1
                while i+q<n and text[i+q]=='"': q+=1
                if q>=raw_quotes:
                    for j in range(i,min(i+q,n)): out[j]=' '
                    i+=q; state='code'
                else: i+=q
            else: i+=1
    return ''.join(out)

def match_brace(masked: str, open_pos: int) -> int:
    depth=0
    for i in range(open_pos,len(masked)):
        if masked[i]=='{': depth+=1
        elif masked[i]=='}':
            depth-=1
            if depth==0: return i
    return -1

TYPE_RE=re.compile(r'''(?mx)^[ \t]*(?:\[[^\]\n]+\][ \t]*)*(?:(?:public|internal|private|protected|file|sealed|abstract|partial|new|readonly|unsafe)\s+)*(?P<kind>class|record\s+class|record|struct)\s+(?P<name>[A-Za-z_]\w*)(?P<tail>[^;{]*?)\{''')
METHOD_RE=re.compile(r'''(?mx)^[ \t]*(?:\[[^\]\n]+\][ \t]*\r?\n[ \t]*)*(?P<signature>(?P<access>public|private|protected|internal)\s+(?P<modifiers>(?:(?:static|async|virtual|override|sealed|partial|new|unsafe|extern|required)\s+)*)(?P<return>[A-Za-z_][\w\.\?<>,\[\]\s:]*(?:\s*\*)?)\s+(?P<name>[A-Za-z_]\w*)\s*\([^;{}]*?\)\s*(?:where\s+[^\{=>\r\n]+\s*)?)(?P<body>=>|\{)''')

def parse_types(text: str) -> list[TypeDecl]:
    masked=mask_csharp(text); result=[]
    for m in TYPE_RE.finditer(masked):
        op=m.end()-1; cl=match_brace(masked,op)
        if cl>=0: result.append(TypeDecl(m.group('name'),m.group('kind'),m.start(),op,cl))
    return result

def enclosing_type(types: list[TypeDecl], pos: int):
    c=[t for t in types if t.open<pos<t.close]
    return min(c,key=lambda t:t.close-t.open) if c else None

def parse_methods(text: str) -> list[MethodDecl]:
    masked=mask_csharp(text); types=parse_types(text); raw=[]
    for m in METHOD_RE.finditer(masked):
        typ=enclosing_type(types,m.start())
        if typ is None or typ.kind.startswith('record'): continue
        body=m.group('body'); bs=m.start('body')
        if body=='{': end=match_brace(masked,bs)
        else:
            i=bs+2; par=br=cur=0
            while i<len(masked):
                c=masked[i]
                if c=='(': par+=1
                elif c==')': par=max(0,par-1)
                elif c=='[': br+=1
                elif c==']': br=max(0,br-1)
                elif c=='{': cur+=1
                elif c=='}' and cur: cur-=1
                elif c==';' and par==br==cur==0: break
                i+=1
            end=i
        if end<0: continue
        raw.append(MethodDecl(m.group('name'),re.sub(r'\s+',' ',m.group('signature')).strip(),m.group('return').strip(),m.group('modifiers').strip(),m.group('access'),m.start(),bs,end+1,body=='=>',typ.name))
    return [m for m in raw if not any(o.start<m.start<o.end and (o.end-o.start)>(m.end-m.start) for o in raw if o is not m)]

def line_of(text: str, pos: int) -> int: return text.count('\n',0,pos)+1

def iter_sources(app_root: Path):
    for path in app_root.rglob('*'):
        if not path.is_file() or path.suffix.lower() not in {'.cs','.razor'}: continue
        rel=path.relative_to(app_root).as_posix()
        if any(part in {'bin','obj','Migrations','.git','.vs'} for part in path.parts): continue
        if path.name.endswith('.Designer.cs'): continue
        yield path,rel

def allowed_static(rel: str, product: str) -> bool:
    if rel=='Program.cs': return True
    if product=='publisherstudio' and rel in {'PublisherStudioServiceCollectionExtensions.cs','StreamingServiceCollectionExtensions.cs'}: return True
    return False

def static_audit(app_root: Path, product: str):
    failures=[]
    decl_re=re.compile(r'(?m)^[ \t]*(?:public|private|protected|internal)\s+(?:(?:sealed|partial|new|unsafe|readonly|abstract)\s+)*static\s+[^\r\n]+')
    for path,rel in iter_sources(app_root):
        text=path.read_text(encoding='utf-8-sig',errors='replace'); masked=mask_csharp(text) if path.suffix=='.cs' else text
        if path.suffix=='.cs':
            if 'GeneratedRegex' in masked: failures.append(f'{rel}: GeneratedRegex attribute is not allowed in application code')
            for m in decl_re.finditer(masked):
                declaration=re.sub(r'\s+',' ',text[m.start():m.end()]).strip()
                if rel=='Program.cs':
                    continue
                if product=='publisherstudio' and rel in {'PublisherStudioServiceCollectionExtensions.cs','StreamingServiceCollectionExtensions.cs'}:
                    if re.search(r'\bstatic\s+class\s+\w+ServiceCollectionExtensions\b',declaration):
                        continue
                    if 'this IServiceCollection' in declaration and 'ILogger' in declaration:
                        continue
                if rel.startswith('Extensions/'):
                    if re.search(r'\bstatic\s+class\s+\w+Extensions\b', declaration):
                        continue
                    signature_window=masked[m.start():m.start()+1000].split('{',1)[0]
                    if re.search(r'\bpublic\s+static\b', declaration) and re.search(r'\(\s*this\s+', signature_window):
                        continue
                failures.append(f'{rel}:{line_of(text,m.start())}: {declaration}')
        elif path.suffix=='.razor':
            if rel.endswith('_Imports.razor'):
                # RenderMode import is framework syntax, not application state.
                for i,line in enumerate(text.splitlines(),1):
                    if '@using static' in line and 'Microsoft.AspNetCore.Components.Web.RenderMode' not in line:
                        failures.append(f'{rel}:{i}: unsupported static Razor import')
            else:
                razor_decl_re=re.compile(r'(?m)^[ \t]*(?:public|private|protected|internal)\s+(?:(?:sealed|partial|new|unsafe|readonly|abstract|async)\s+)*static\s+[^\r\n]+')
                for m in razor_decl_re.finditer(text):
                    declaration=re.sub(r'\s+',' ',text[m.start():m.end()]).strip()
                    failures.append(f'{rel}:{line_of(text,m.start())}: {declaration}')
    if product=='publisherstudio':
        for rel in ('PublisherStudioServiceCollectionExtensions.cs','StreamingServiceCollectionExtensions.cs'):
            p=app_root/rel
            if not p.exists(): failures.append(f'{rel}: required DI extension boundary missing'); continue
            t=p.read_text(encoding='utf-8-sig',errors='replace')
            for m in re.finditer(r'public\s+static\s+IServiceCollection\s+\w+\s*\((?P<args>[^)]*)\)',mask_csharp(t)):
                args=m.group('args')
                body_start=mask_csharp(t).find('{',m.end())
                body_end=match_brace(mask_csharp(t),body_start) if body_start>=0 else -1
                body=t[body_start:body_end+1] if body_end>=0 else ''
                if 'this IServiceCollection' not in args or 'ILogger' not in args: failures.append(f'{rel}: DI extension method must accept IServiceCollection and ILogger')
                if not (re.search(r'\btry\b',mask_csharp(body)) and re.search(r'\bcatch\b',mask_csharp(body)) and re.search(r'\b(?:logger|_logger|Logger)\.Log\w*\s*\(',body)):
                    failures.append(f'{rel}: DI extension method must own try/catch and logging')
    return failures

STRICT_METHOD_FILES = {
    'localgpt': {
        'Controller/RuntimePolicyController.cs',
        'Services/Persistence/LocalGptRuntimePolicyDataService.cs',
        'Services/Persistence/LocalGptRuntimePolicyStoreService.cs',
        'Services/Persistence/LocalGptRuntimePolicySeedDataService.cs',
        'Services/Persistence/LocalGptVocabularyService.cs',
        'Services/Persistence/OneWireReplayPolicyDataService.cs',
        'Services/OneWire/OneWireTransportSecurityPolicy.cs',
    },
    'publisherstudio': {
        'Controllers/RuntimePolicyController.cs',
        'Services/Configuration/PublisherRuntimePolicyDataService.cs',
        'Services/Configuration/PublisherRuntimePatternService.cs',
        'Services/Configuration/OrganicReplayPolicyDataService.cs',
        'Services/OrganicPlugins/OrganicTransportSecurityPolicy.cs',
        'Services/Streaming/Hotkeys/WindowsHotkeyNativeService.cs',
        'Services/Streaming/Capture/WindowsProcessLoopbackNativeService.cs',
    },
}

def method_audit(app_root: Path, product: str):
    failures=[]
    strict=STRICT_METHOD_FILES[product]
    for path,rel in iter_sources(app_root):
        if path.suffix!='.cs' or rel not in strict: continue
        text=path.read_text(encoding='utf-8-sig',errors='replace')
        for method in parse_methods(text):
            body=text[method.body_start:method.end]
            masked_body=mask_csharp(body)
            ident=f'{rel}:{line_of(text,method.start)} {method.type_name}.{method.name}'
            if not re.search(r'\b(?:logger|_logger|Logger)\s*\.\s*Log\w*\s*\(', body):
                failures.append(f'{ident}: maintained operational method has no structured log call')
            has_yield=bool(re.search(r'\byield\s+(?:return|break)\b',masked_body))
            required_end=r'\bfinally\b' if has_yield else r'\bcatch\b'
            if not (re.search(r'\btry\b',masked_body) and re.search(required_end,masked_body)):
                failures.append(f'{ident}: maintained operational method has no try/catch (or iterator try/finally)')
            for log_call in re.finditer(r'\b(?:logger|_logger|Logger)\s*\.\s*Log\w*\s*\((?P<args>[\s\S]{0,500}?)\)', body):
                args=log_call.group('args')
                if '"' in args and not any(token in args for token in ('$"','$@"','@$"')):
                    failures.append(f'{ident}: maintained operational log message must be interpolated')
                    break
    for rel in sorted(strict):
        if not (app_root/rel).exists(): failures.append(f'{rel}: maintained operational policy file is missing')
    return failures

def runtime_value_audit(app_root: Path, product: str):
    failures=[]
    allowed_regex_compilers = {
        'localgpt': {
            'Services/Persistence/LocalGptRuntimePolicyDataService.cs',
            'Services/Persistence/CouncilTextPatternDataService.cs',
            'Services/Persistence/RegexPatternService.cs',
            'Services/ProjectMaintenanceService.cs',
        },
        'publisherstudio': {
            'Services/Configuration/PublisherRuntimePatternService.cs',
            'Services/Configuration/PanelStudioTextPatternDataService.cs',
        },
    }[product]
    for path,rel in iter_sources(app_root):
        if path.suffix!='.cs': continue
        text=path.read_text(encoding='utf-8-sig',errors='replace'); code=mask_csharp(text)
        if 'GeneratedRegex' in code:
            failures.append(f'{rel}: GeneratedRegex is not a serializable runtime-value boundary')
        if re.search(r'\bnew\s+Regex\s*\(',code) and rel not in allowed_regex_compilers:
            failures.append(f'{rel}: compiles a Regex outside an approved policy/data service')
        # Static collections and regex fields are forbidden even in otherwise allowed files.
        if rel != 'Program.cs' and re.search(r'(?m)^[ \t]*(?:public|private|protected|internal)\s+(?:static\s+|readonly\s+)*static\s+[^\n]*(?:Regex|List<|Dictionary<|HashSet<|FrozenSet<|\[\])',code):
            failures.append(f'{rel}: contains static runtime collection or Regex state')
    if product=='localgpt':
        required={
            'Services/Persistence/LocalGptRuntimePolicySeedDataService.cs': ['RegexTimeoutMilliseconds','AllowedNativeExecutables','VocabularyJson'],
            'Services/Persistence/LocalGptRuntimePolicyDataService.cs': ['ILocalGptRuntimePolicyStoreService','store.GetDefinition()','GetPattern','GetCollection'],
            'Controller/RuntimePolicyController.cs': ['GetDefinition','GetSeed','Reload'],
        }
    else:
        required={
            'Services/Configuration/PublisherRuntimePolicyDataService.cs': ['PublisherRuntimePolicyOptions','GetCollection','GetSnapshot'],
            'Services/Configuration/PublisherRuntimePatternService.cs': ['PublisherRuntimePattern','GetRegex','TimeoutMilliseconds'],
            'Controllers/RuntimePolicyController.cs': ['PublisherRuntimePolicySnapshot','Get'],
        }
        settings=app_root/'appsettings.json'
        if not settings.exists(): failures.append('appsettings.json: missing PublisherStudio runtime-policy configuration')
        else:
            try:
                doc=json.loads(settings.read_text(encoding='utf-8-sig'))
                node=doc.get('PublisherStudio',{}).get('RuntimePolicy')
                if not isinstance(node,dict) or not node: failures.append('appsettings.json: PublisherStudio.RuntimePolicy must be a populated serializable object')
            except Exception as exc: failures.append(f'appsettings.json: invalid JSON ({exc})')
    for rel,tokens in required.items():
        path=app_root/rel
        if not path.exists(): failures.append(f'{rel}: required runtime-value data boundary is missing'); continue
        text=path.read_text(encoding='utf-8-sig',errors='replace')
        for token in tokens:
            if token not in text: failures.append(f'{rel}: required runtime-value ownership token is missing: {token}')
    return failures

def structure_audit(app_root: Path):
    failures=[]
    for path,rel in iter_sources(app_root):
        if path.suffix!='.cs': continue
        text=path.read_text(encoding='utf-8-sig',errors='replace'); masked=mask_csharp(text)
        balance=masked.count('{')-masked.count('}')
        if balance: failures.append(f'{rel}: brace balance {balance:+d}')
    return failures

def main():
    ap=argparse.ArgumentParser(); ap.add_argument('--root',required=True); ap.add_argument('--product',choices=['localgpt','publisherstudio'],required=True); ap.add_argument('--mode',choices=['static','methods','runtime','structure','all'],default='all'); ap.add_argument('--json',action='store_true'); args=ap.parse_args()
    root=Path(args.root).resolve()
    app=root/('src/LocalGPT' if args.product=='localgpt' else 'src/PublisherStudio.Web')
    checks={}
    if args.mode in {'static','all'}: checks['static']=static_audit(app,args.product)
    if args.mode in {'methods','all'}: checks['methods']=method_audit(app,args.product)
    if args.mode in {'runtime','all'}: checks['runtime']=runtime_value_audit(app,args.product)
    if args.mode=='structure': checks['structure']=structure_audit(app)
    failures=[f'{k}: {v}' for k,vals in checks.items() for v in vals]
    if args.json: print(json.dumps({'product':args.product,'checks':checks,'failureCount':len(failures)},indent=2))
    else:
        if failures:
            print('Architecture policy audit failed:')
            for f in failures: print(f'  - {f}')
        else: print('Architecture policy audit passed: application statics, operational diagnostics, and C# structure comply with the maintained boundaries.')
    return 1 if failures else 0
if __name__=='__main__': raise SystemExit(main())
