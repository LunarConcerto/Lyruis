"""Compare matching Lyruis peak sweeps, including every stored layout coordinate."""
import argparse
import json
import math
from pathlib import Path
import struct


def compare(before_path, after_path, output):
    before = json.loads(before_path.read_text(encoding='utf-8'))
    after = json.loads(after_path.read_text(encoding='utf-8'))
    for field in ['unity', 'processor', 'width', 'height', 'samples', 'warmups', 'diagnosticSamples', 'externalProfiler']:
        if before[field] != after[field]:
            raise ValueError(f'Configuration differs: {field}')
    key = lambda row: (row['cards'], row['nodes'], row['name'], row['batch'], row['rebuildsPerOperation'])
    a = {key(row): row for row in before['rows']}
    b = {key(row): row for row in after['rows']}
    if a.keys() != b.keys():
        raise ValueError('Sweep configurations differ')
    old_geometry = (before_path.parent / before['geometryFile']).read_bytes()
    new_geometry = (after_path.parent / after['geometryFile']).read_bytes()
    rows = []
    for identity, old in a.items():
        new = b[identity]
        count = old['nodes'] * 10
        old_values = struct.unpack_from(f'<{count}f', old_geometry, old['geometryOffset'])
        new_values = struct.unpack_from(f'<{count}f', new_geometry, new['geometryOffset'])
        if not all(math.isfinite(v) for v in (*old_values, *new_values)):
            raise ValueError('Non-finite stored geometry')
        error = max(abs(x-y) for x, y in zip(old_values, new_values))
        if error > .01:
            raise ValueError(f'Geometry mismatch {identity}: {error}')
        if old['meanRootCompletions'] != old['rebuildsPerOperation'] or new['meanRootCompletions'] != new['rebuildsPerOperation']:
            raise ValueError(f'Unexpected root completion count: {identity}')
        rows.append((old, new, error))
    text = [
        '# Peak sweep comparison / 峰值扫描与优化', '',
        f"Unity {after['unity']}; {after['processor']}; {after['width']}x{after['height']}; {after['samples']} samples/config; {after['warmups']} warmups; external Profiler {after['externalProfiler']}.", '',
        'Operations include one explicit Canvas flush. Root4 issues four synchronous rebuilds inside the same operation; other workloads issue one mutation/rebuild. These are operation costs, not measured FPS or guaranteed hardware limits. Max is the largest observed sample. Timing excludes detailed diagnostic counters; separate phase totals are retained in JSON.', '',
        '优化保持尺寸和排布公式：合并并跳过无变化的 RectTransform 写入；按拓扑版本缓存根节点，在层级、启停及编辑器验证时保守失效。未引入跨帧尺寸缓存或局部重建，因此原有显式 Refresh 语义保持不变。', '',
        '| Cards | Nodes | Workload | Before median | After median | Reduction | Before P95 | After P95 | Before max | After max | Geometry error |',
        '| ---: | ---: | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |'
    ]
    for old, new, error in rows:
        reduction = (1-new['medianMs']/old['medianMs'])*100
        text.append(f"| {old['cards']} | {old['nodes']} | {old['name']} | {old['medianMs']:.3f} | {new['medianMs']:.3f} | {reduction:.1f}% | {old['p95Ms']:.3f} | {new['p95Ms']:.3f} | {old['maxMs']:.3f} | {new['maxMs']:.3f} | {error:.6f} |")
    text += ['', '## Sampled reference-budget crossings / 采样预算越界点', '',
             'First sampled node count whose operation P95 exceeds 16.67ms; this is not a full-frame 60-FPS capacity claim. No interpolation is performed.', '',
             '| Workload | Before | After |', '| --- | --- | --- |']
    for name in ['SingleLeaf','Batch32','BatchAll','Root1','Root4','Resize']:
        values=[]
        for report in [before, after]:
            matches=[r['nodes'] for r in report['rows'] if r['name']==name and r['p95Ms']>16.67]
            values.append(str(min(matches)) if matches else 'Not crossed through 4241 nodes')
        text.append(f'| {name} | {values[0]} | {values[1]} |')
    text += ['', '## Largest configuration: diagnostic SELF ms per operation', '',
             'Diagnostic scopes perturb timings and are not summed with parent inclusive totals. LeafIntrinsicSize counts all leaf-resolution scopes, including Fixed/Keep.', '',
             '| Workload | Phase | Before self ms/op | After self ms/op |', '| --- | --- | ---: | ---: |']
    for old,new,_ in rows:
        if old['cards']!=80 or old['name'] not in ['BatchAll','Root1','Root4']:
            continue
        p={v['name']:v for v in old['phases']}; q={v['name']:v for v in new['phases']}
        for name in ['ApplyChildRects','RootLookup','LeafIntrinsicSize','ClearDrivenTracker']:
            text.append(f"| {old['name']} | {name} | {p[name]['selfMs']/before['diagnosticSamples']:.3f} | {q[name]['selfMs']/after['diagnosticSamples']:.3f} |")
    regressions=[f"{o['nodes']}/{o['name']}" for o,n,_ in rows if n['medianMs']>o['medianMs']*1.05]
    tail_regressions=[f"{o['nodes']}/{o['name']}" for o,n,_ in rows if n['p95Ms']>o['p95Ms']*1.05]
    max_regressions=[f"{o['nodes']}/{o['name']}" for o,n,_ in rows if n['maxMs']>o['maxMs']*1.05]
    text += ['', '## Validation and limits / 验证与边界', '',
             f'- All {len(rows)} configurations matched root completion counts and all stored size/position/anchor/pivot coordinates within .01. Maximum absolute difference: {max(e for _,_,e in rows):.6f}.',
             '- Median regressions above 5%: '+(', '.join(regressions) if regressions else 'none in this sweep')+'.',
             '- P95 increases above 5%: '+(', '.join(tail_regressions) if tail_regressions else 'none in this sweep')+'.',
             '- Observed Max increases above 5%: '+(', '.join(max_regressions) if max_regressions else 'none in this sweep')+'.',
             '- Sequential before/after Editor captures; not randomized A/B or statistical confidence intervals. Temperature, editor activity and GC can influence tails. Player builds and rendering outside explicit flush are not covered.',
             '- Original scene is disabled during temporary test-lab execution and restored afterward. No source scene is overwritten.', '',
             f'- [Before raw JSON]({before_path.name}) / [geometry]({before["geometryFile"]})',
             f'- [After raw JSON]({after_path.name}) / [geometry]({after["geometryFile"]})', '']
    output.write_text('\n'.join(text),encoding='utf-8')
    print(f'Compared {len(rows)} configurations; max geometry difference {max(e for _,_,e in rows):.6f}; report: {output}')


if __name__=='__main__':
    parser=argparse.ArgumentParser()
    parser.add_argument('before',type=Path);parser.add_argument('after',type=Path);parser.add_argument('output',type=Path)
    args=parser.parse_args();compare(args.before,args.after,args.output)
