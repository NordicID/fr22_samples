#!/usr/bin/env python3

try:
    import os
    import json
    from sys import stderr
    from zipfile import ZipFile
    from os.path import join as path_join
    from packaging.version import parse as parse_version
except ModuleNotFoundError as e:
    print(e)
    print(f"Install '{e.name}':\n\tpython3 -m pip install {e.name}")
    exit(1)

def getJson(fpath, ignore = None):
    try:
        with open(fpath) as fp:
            entry = json.load(fp)
    except Exception:
        entry = {}
    else:
        if ignore:
            for ign in ignore:
                if ign in entry:
                    del entry[ign]
    return entry

def updateDependencies(release, depends):
    if depends:
        if not 'depends' in release:
            release['depends'] = depends
        else:
            release['depends'].update(depends)

def updateDependenciesJson(release, fpath):
    updateDependencies(release, getJson(fpath).get('depends'))

def readManifest(fpath):
    with ZipFile(fpath, 'r') as z:
        manifest = json.loads(z.read('meta/manifest.json'))
    depends = manifest.get('depends', {})
    if 'startup' in manifest and 'depends' in manifest['startup']:
        for dep_type in ('plugins', 'applications'):
            if dep_type in manifest['startup']['depends']:
                if dep_type not in depends:
                    depends[dep_type] = []
                for dep_name in manifest['startup']['depends'][dep_type]:
                    found = False
                    for dep in depends[dep_type]:
                        if dep['name'] == dep_name:
                            found = True
                            break
                    if not found:
                        depends[dep_type].append({'name': dep_name})
    return manifest['name'], manifest['version'], depends

def getPlugins(zipEntries, folder):
    index = {}
    for dir, _, files in os.walk(path_join('repo', folder)):
        for f in files:
            fpath = path_join(dir, f)
            url = '/'.join(fpath.split(os.sep)[1:])
            try:
                name, version, depends = readManifest(fpath)
                release = { "version": version, "url": url}
                updateDependenciesJson(release, path_join('meta', folder, name + '.json'))
                updateDependenciesJson(release, path_join('meta', folder, name, version + '.json'))
                updateDependencies(release, depends)
            except Exception as e:
                print(' -', url, '- IGNORED :', e, file=stderr)
            else:
                print(' +', url)
                zipEntries.append((fpath, url))
                if name not in index:
                    index[name] = getJson(path_join('meta', folder, name + '.json'), ['depends'])
                    index[name]['releases'] = [release]
                else:
                    # Sort by version
                    rel_ver = parse_version(version)
                    rels = index[name]['releases']
                    idx = len(rels)
                    for i in range(idx):
                        i_ver = parse_version(rels[i]['version'])
                        if rel_ver > i_ver:
                            idx = i
                            break
                        elif i_ver == rel_ver:
                            if 'platform' in rels[i].get('depends', {}) and 'platform' in release.get('depends', {}):
                                if rels[i]['depends']['platform'] != release['depends']['platform']:
                                    # Let sc20 be the last one for backward compatibility
                                    if rels[i]['depends']['platform'] == 'sc20':
                                        idx = i
                                        break
                                    continue
                            raise RuntimeError(f"Duplicate version of {folder} {name} {version}")
                    index[name]['releases'].insert(idx, release)
    index_list = []
    for plg_name, plg in index.items():
        plugin = { 'name': plg_name }
        plugin.update(plg)
        index_list.append(plugin)
    return index_list

def getFirmwares(zipEntries, folder):
    index = []
    for dir, _, files in os.walk(path_join('meta', folder)):
        for f in files:
            fw = getJson(path_join(dir, f))
            if not fw:
                continue
            if '://' not in fw['url']:
                fpath = path_join('repo', *(fw['url'].split('/')))
                if not os.path.isfile(fpath):
                    print(' -', fw['url'], '- IGNORED', file=stderr)
                    continue
            else:
                fpath = None
            # Sort by version
            rel_ver = parse_version(fw['version'])
            idx = len(index)
            for i in range(idx):
                i_ver = parse_version(index[i]['version'])
                if rel_ver > i_ver:
                    idx = i
                    break
                elif i_ver == rel_ver:
                    if 'platform' in index[i].get('depends', {}) and 'platform' in fw.get('depends', {}):
                        if index[i]['depends']['platform'] != fw['depends']['platform']:
                            continue
                    raise RuntimeError(f"Duplicate version of {folder} {fw['version']}")
            index.insert(idx, fw)
            print(' +', fw['url'])
            if fpath:
                zipEntries.append((fpath, fw['url']))
    return index

def main():
    index_html = path_join('repo', 'index.html')
    index_json = path_join('repo', 'index.json')
    zipEntries = [
        (index_html, 'index.html'),
        (index_json, 'index.json')
    ]
    index = {
        'applications': getPlugins(zipEntries, 'app'),
        'plugins': getPlugins(zipEntries, 'sys')
    }
    fw = getFirmwares(zipEntries, 'fw')
    if fw:
        index['firmware'] = fw
    with open(index_json, 'w') as f:
        json.dump(index, f)
    with open('index.json', 'w') as f:
        json.dump(index, f, indent='\t')
    open(index_html, 'a').close()
    with ZipFile('repo.zip', 'w') as z:
        for zipEntry in zipEntries:
            z.write(*zipEntry)

if __name__ == '__main__':
    main()
