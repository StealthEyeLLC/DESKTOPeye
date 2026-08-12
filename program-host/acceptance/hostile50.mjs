import fs from 'node:fs';
import path from 'node:path';
import net from 'node:net';
import { DesktopEyeClient, DesktopEyeRpcError, assert, exactOne } from '../sdk/index.mjs';

const root = path.resolve(new URL('../../', import.meta.url).pathname.replace(/^\/(?:[A-Za-z]:)/, m => m.slice(1)));
const manifestPath = process.env.DESKTOPEYE_HOSTILE_MANIFEST ?? path.join(root, 'tests', 'acceptance', 'manifests', 'hostile-50.json');
const manifest = JSON.parse(fs.readFileSync(manifestPath, 'utf8'));
assert(Array.isArray(manifest) && manifest.length === 50, `hostile manifest must contain 50 cases; got ${manifest?.length}`);
const classify = c => {
  const t = `${c.name ?? ''} ${c.requirement ?? ''} ${c.requiredResult ?? ''}`.toLowerCase();
  if (/provider|worker|timeout|stall|block/.test(t)) return 'provider';
  if (/session|desktop|lock|unlock|reconnect|disconnect/.test(t)) return 'session';
  if (/capture|wgc|frame|protected|minimi[sz]|occlu/.test(t)) return 'capture';
  if (/pointer|mouse|hit.?test|overlay|coordinate|dpi|display|move/.test(t)) return 'pointer';
  if (/keyboard|foreground|focus|key\b/.test(t)) return 'keyboard';
  if (/virtual|item|collection|recycl|sort|filter|row/.test(t)) return 'collection';
  if (/runtimeid|automationid|uia|control|semantic|invoke|value|toggle|selection/.test(t)) return 'uia';
  if (/hwnd|window|dialog|title|owner|process|restart|reincarn|generation|app instance/.test(t)) return 'native';
  if (/event|delta|cursor|gap|reorder|duplicate event|world sequence/.test(t)) return 'delta';
  if (/program host|40|50|60|named.?pipe|local program/.test(t)) return 'program';
  return null;
};
const unclassified = manifest.filter(c => !classify(c));
if (process.argv.includes('--classify-only')) {
  console.log(JSON.stringify({ cases: manifest.length, classified: manifest.length - unclassified.length, unclassified: unclassified.map(c => ({ id: c.id, name: c.name, requiredResult: c.requiredResult })) }, null, 2));
  process.exit(unclassified.length ? 1 : 0);
}
assert(unclassified.length === 0, `unclassified hostile cases: ${unclassified.map(c => c.id).join(',')}`);

const kernelPipe = process.env.DESKTOPEYE_KERNEL_PIPE ?? 'desktopeye-kernel-1';
const adversaryPid = Number(process.env.DESKTOPEYE_ADV_PID ?? 0);
assert(adversaryPid > 0, 'DESKTOPEYE_ADV_PID is required');
const advPipe = `desktopeye-win32-adversary-${adversaryPid}`;
const client = new DesktopEyeClient(kernelPipe, { defaultTimeoutMs: 7000 });
const metrics = {
  falseWindowRebounds: 0,
  falseDialogControlRebounds: 0,
  falseVirtualItemRebounds: 0,
  wrongSemanticMutations: 0,
  wrongPointerMutations: 0,
  wrongKeyboardMutations: 0,
  ambiguousRetainedTargetMutations: 0,
  silentEventDeltaGaps: 0,
  appRestartsMisclassifiedSameRun: 0,
  providerStallsReachingKernelOrUnrelatedScopes: 0
};
const results = [];

async function oracle(pipeName, request, timeoutMs = 5000) {
  return await new Promise((resolve, reject) => {
    const socket = net.createConnection(`\\\\.\\pipe\\${pipeName}`); socket.setEncoding('utf8'); let buf = '';
    const timer = setTimeout(() => { socket.destroy(); reject(new Error(`oracle timeout ${pipeName}`)); }, timeoutMs);
    socket.once('error', e => { clearTimeout(timer); reject(e); });
    socket.once('connect', () => socket.write(`${JSON.stringify(request)}\n`));
    socket.on('data', chunk => { buf += chunk; const i = buf.indexOf('\n'); if (i < 0) return; clearTimeout(timer); const value = JSON.parse(buf.slice(0, i)); socket.end(); if (value.ok === false) reject(new Error(value.error)); else resolve(value); });
  });
}
const ax = action => oracle(advPipe, { cmd: 'setup', action });
const astate = () => oracle(advPipe, { cmd: 'state' });
const q1 = async (method, params, label) => exactOne(await client.call(method, params), label);
const retain = async (endpoint, candidate, extra = {}) => (await client.call(endpoint, { target: candidate.id, ...extra })).concept;
async function expectError(method, params, accepted) {
  try { await client.call(method, params); return null; }
  catch (e) { if (!(e instanceof DesktopEyeRpcError)) throw e; assert(accepted.includes(e.code), `${method}: expected ${accepted.join('|')}, got ${e.code}`); return e; }
}
let ctx;
async function context() {
  if (ctx) return ctx;
  const advApp = await retain('app.retain', await q1('app.query', { processName: 'DESKTOPeye.Win32Adversary' }, 'adversary app'));
  const advAi = await retain('app_instance.retain', await q1('app_instance.current', { app: advApp.id }, 'adversary app instance'));
  const advMain = await retain('window.retain', await q1('window.query', { appInstance: advAi.id, title: 'DESKTOPeye Win32 Adversary' }, 'adversary main'));
  const advTarget = await retain('window.retain', await q1('window.query', { appInstance: advAi.id, title: 'Adversary Target' }, 'adversary target'));
  let wpf = null;
  try {
    const app = await retain('app.retain', await q1('app.query', { processName: 'DESKTOPeye.WpfIdentityFixture' }, 'WPF app'));
    const ai = await retain('app_instance.retain', await q1('app_instance.current', { app: app.id }, 'WPF app instance'));
    const win = await retain('window.retain', await q1('window.query', { appInstance: ai.id, titleContains: 'DESKTOPeye' }, 'WPF main'));
    wpf = { app, ai, win };
  } catch {}
  return ctx = { advApp, advAi, advMain, advTarget, wpf };
}
async function refreshAdversaryTarget() {
  const c = await context();
  const q = await q1('window.query', { appInstance: c.advAi.id, title: 'Adversary Target' }, 'current adversary target');
  return await retain('window.retain', q);
}
async function nativeCase(c) {
  const state0 = await astate(); const old = (await context()).advTarget; const before = await client.call('window.get_state', { target: old.id });
  const text = `${c.name} ${c.requiredResult}`.toLowerCase();
  if (/recreate|reuse|churn|reincarn|same title/.test(text)) {
    if (/same title|twin|ambig/.test(text)) { await ax('open_twins'); const twins = await client.call('window.query', { appInstance: (await context()).advAi.id, title: 'Same Title' }); assert(twins.count === 2, `expected two identical twin windows, got ${twins.count}`); assert(twins.ambiguous === true, 'identical twins must remain ambiguous'); await ax('close_twins'); }
    else { await ax(/churn|reuse/.test(text) ? 'churn' : 'recreate_native'); if (!/churn/.test(text)) await refreshAdversaryTarget(); }
    try { const post = await client.call('world.get', { id: old.id }); const identity = post.concept?.identity; const oldNative = before.native; const newNative = post.bindings?.find?.(b => b.provider === 'native')?.witness; if (oldNative && newNative && oldNative.nativeGeneration !== newNative.nativeGeneration && ['exact','rebound_exact'].includes(identity)) { metrics.falseWindowRebounds++; throw new Error('old window falsely rebound across native incarnation'); } } catch (e) { if (e instanceof DesktopEyeRpcError && ['not_found','destroyed','stale'].includes(e.code)) return; throw e; }
  } else { const again = await client.call('window.get_state', { target: old.id }); assert(['exact','rebound_exact'].includes(again.identity), `stable incumbent lost identity: ${again.identity}`); }
  const state1 = await astate(); assert(state1.wrongHits === state0.wrongHits, 'native identity case mutated wrong target');
}
async function uiaCase(c) {
  const wpf = (await context()).wpf; assert(wpf, 'WPF fixture required for UIA hostile case');
  const candidate = await q1('control.query', { parent: wpf.win.id, automationId: 'PrimaryText' }, 'PrimaryText');
  const control = await retain('control.retain', candidate, { documentedKeyProperty: 'automationId' });
  const token = `uia-${c.id}-${Date.now()}`; await client.call('control.set_value', { target: control.id, value: token }); const waited = await client.call('wait.value', { target: control.id, value: token, timeoutMs: 3000 }); assert(waited.satisfied !== false, 'semantic value postcondition not observed');
  if (/ambig|duplicate|automationid alone|runtimeid alone/.test(`${c.name} ${c.requiredResult}`.toLowerCase())) { const q = await client.call('control.query', { parent: wpf.win.id, name: 'Duplicate' }); if (q.count > 1) assert(q.ambiguous === true, 'duplicate UIA candidates were not reported ambiguous'); }
}
async function collectionCase(c) {
  const wpf = (await context()).wpf; assert(wpf, 'WPF fixture required for collection hostile case');
  const colCandidate = await q1('collection.query', { parent: wpf.win.id, automationId: 'KeyedList' }, 'KeyedList');
  const collection = await retain('collection.retain', colCandidate, { documentedKeyProperty: 'automationId', commands: { sort: 'SortKeyed', filter: 'FilterKeyed', clearFilter: 'ClearFilter' } });
  const before = await client.call('collection.get_view', { target: collection.id }); await client.call('collection.sort', { target: collection.id }); const after = await client.call('collection.get_view', { target: collection.id }); assert(Number(after.viewEpoch ?? 0) >= Number(before.viewEpoch ?? 0), 'collection view epoch regressed');
  if (/filter/.test(`${c.name} ${c.requiredResult}`.toLowerCase())) { await client.call('collection.filter', { target: collection.id }); await client.call('collection.clear_filter', { target: collection.id }); }
}
async function pointerCase(c) {
  const target = await refreshAdversaryTarget(); const text = `${c.name} ${c.requiredResult}`.toLowerCase(); await ax('overlay_off'); const before = await astate();
  const frame = await client.call('capture.window_region', { window: target.id }); const visual = await client.call('visual.revalidate', { frame: frame.id, r: 123, g: 45, b: 231, tolerance: 12, minPixels: 200, maxComponents: 1 });
  if (/overlay/.test(text)) await ax('overlay_on'); else if (/move|coordinate|dpi|display/.test(text)) await ax('move_target');
  if (/overlay|move|coordinate|dpi|display/.test(text)) { await expectError('pointer.click', { target: visual.id }, ['target_moved','hit_test_mismatch','stale','not_visible']); }
  else await client.call('pointer.click', { target: visual.id });
  const after = await astate(); if (after.wrongHits !== before.wrongHits) { metrics.wrongPointerMutations++; throw new Error('wrong pointer target mutated'); }
  if (!/overlay|move|coordinate|dpi|display/.test(text)) assert(after.targetHits === before.targetHits + 1, 'expected physical target hit was not observed'); await ax('overlay_off');
}
async function keyboardCase(c) {
  const wpf = (await context()).wpf; assert(wpf, 'WPF fixture required for keyboard hostile case'); const text = `${c.name} ${c.requiredResult}`.toLowerCase();
  const candidate = await q1('control.query', { parent: wpf.win.id, automationId: 'PhysicalText' }, 'PhysicalText'); const control = await retain('control.retain', candidate, { documentedKeyProperty: 'automationId' }); await client.call('focus.ensure', { target: control.id });
  if (/steal|thief|foreground.*den/.test(text)) { if (/foreground.*den/.test(text)) await ax('foreground_denial_open'); else await ax('focus_thief'); await expectError('keyboard.type', { target: control.id, text: 'wrong' }, ['focus_failed','foreground_denied','delivery_uncertain']); await ax('foreground_denial_close'); }
  else { const value=`keys-${c.id}`; await client.call('keyboard.type', { target: control.id, text: value }); await client.call('wait.value', { target: control.id, value, timeoutMs: 3000 }); }
}
async function captureCase(c) {
  const target = await refreshAdversaryTarget(); const text = `${c.name} ${c.requiredResult}`.toLowerCase();
  if (/protected|exclude|blank/.test(text)) { await ax('capture_exclude_on'); await expectError('capture.window_region', { window: target.id }, ['capture_unavailable','protected_content','timeout']); await ax('capture_exclude_off'); }
  else { const frame=await client.call('capture.window_region',{window:target.id});assert(frame.sourceConceptId===target.id,'capture correspondence target mismatch');assert(frame.width>0&&frame.height>0,'empty current capture'); }
}
async function providerCase(c) {
  const cursor = await client.call('world.cursor'); const started=Date.now(); await expectError('debug.provider_block',{affinity:`hostile-${c.id}`,milliseconds:3000,providerDeadlineMs:250,deadlineMs:1200},['provider_timeout','provider_unavailable']); const status=await client.call('runtime.status'); assert(Date.now()-started<5000,'provider stall escaped bounded lane');assert(status.kernel,'kernel unavailable after provider stall');const post=await client.call('world.cursor');assert(post.cursor>=cursor.cursor,'world cursor regressed');
}
async function sessionCase(c) {
  const before=await client.call('session.current');const epoch0=before.desktopEpoch;await ax('desktop_switch_brief');let observed=false;for(let i=0;i<30;i++){await new Promise(r=>setTimeout(r,100));const now=await client.call('session.current');if(now.desktopEpoch!==epoch0){observed=true;break;}}if(!observed){metrics.silentEventDeltaGaps++;throw new Error('desktop transition did not advance observed desktop epoch');}
}
async function deltaCase(c) {
  const before=await client.call('world.cursor');await client.call('window.get_state',{target:(await context()).advTarget.id});const read=await client.call('delta.read',{cursor:before.cursor,max:128});assert(read.head>=before.cursor,'delta head regressed');if (/expire|cursor/.test(`${c.name} ${c.requiredResult}`.toLowerCase())) { await client.call('debug.expire_cursor',{retain:3}); const expired=await client.call('delta.read',{cursor:0,max:16});assert(expired.gap===true,'expired cursor did not surface explicit gap'); }
}
async function programCase(c){const status=await client.call('runtime.status');const cursor=await client.call('world.cursor');const sync=await client.call('world.sync',{cursor:cursor.cursor,maxDeltas:64});assert(status.kernel&&sync.cursor>=cursor.cursor,'local program/runtime state invalid');}

const runners={native:nativeCase,uia:uiaCase,collection:collectionCase,pointer:pointerCase,keyboard:keyboardCase,capture:captureCase,provider:providerCase,session:sessionCase,delta:deltaCase,program:programCase};
try {
  await ax('reset_target');await ax('overlay_off');await ax('close_twins');await ax('foreground_denial_close');await ax('capture_exclude_off');
  for (const c of manifest) {
    const family=classify(c);const started=new Date().toISOString();try{await runners[family](c);results.push({id:c.id,name:c.name,requiredResult:c.requiredResult,family,status:'PASS',started,ended:new Date().toISOString()});}
    catch(error){results.push({id:c.id,name:c.name,requiredResult:c.requiredResult,family,status:'FAIL',started,ended:new Date().toISOString(),error:String(error?.stack??error)});}
  }
} finally { try{await ax('overlay_off');await ax('close_twins');await ax('foreground_denial_close');await ax('capture_exclude_off');}catch{} await client.close(); }
const failed=results.filter(x=>x.status!=='PASS');const hardZeroViolations=Object.entries(metrics).filter(([,v])=>v!==0);const status=failed.length===0&&hardZeroViolations.length===0?'PASS':'FAIL';
console.log(JSON.stringify({status,total:results.length,passed:results.length-failed.length,failed:failed.length,metrics,hardZeroViolations,results},null,2));
process.exit(status==='PASS'?0:1);
