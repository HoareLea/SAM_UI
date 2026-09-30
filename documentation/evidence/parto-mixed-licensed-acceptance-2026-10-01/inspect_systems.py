"""Systems inventory of the design model and the Mixed run model (zip of JSON). Names only; no paths."""
import sys, zipfile, json, collections
def load(p):
    z = zipfile.ZipFile(p); return json.loads(z.read(z.namelist()[0]))
def walk(o, f):
    if isinstance(o, dict):
        f(o)
        for v in o.values(): walk(v, f)
    elif isinstance(o, list):
        for v in o: walk(v, f)
def objs(d, typ):
    out = []
    def f(o):
        if o.get('_type', '').split(',')[0].endswith('.' + typ) or o.get('_type', '').split(',')[0] == typ: out.append(o)
    walk(d, f); return out
for label, p in zip(sys.argv[1::2], sys.argv[2::2]):
    d = load(p)
    print('==', label)
    vs = objs(d, 'VentilationSystem')
    seen = {}
    for v in vs: seen[v.get('Guid')] = v
    for v in sorted(seen.values(), key=lambda x: x.get('Name') or ''):
        print('  VentilationSystem  %-28s guid=%s' % (v.get('Name'), (v.get('Guid') or '')[:8]))
    ah = {}
    for a in objs(d, 'AirHandlingUnit'): ah[a.get('Guid')] = a
    for a in sorted(ah.values(), key=lambda x: x.get('Name') or ''):
        print('  AirHandlingUnit    %-28s guid=%s' % (a.get('Name'), (a.get('Guid') or '')[:8]))
    c = collections.Counter()
    walk(d, lambda o: c.update([o['_type'].split(',')[0].split('.')[-1]]) if '_type' in o else None)
    for k in ['PartOMaterialisationRecord', 'SimulationResultProvenance', 'OverheatingScenario', 'PartODwellingStrategySet', 'PartOManualEquipmentSelection']:
        print('  %-34s %d' % (k, c[k]))
    print('  model-level keys:', sorted(d.keys()))
