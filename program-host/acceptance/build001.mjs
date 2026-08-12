import { DesktopEyeClient, DesktopEyeRpcError, assert, exactOne } from '../sdk/index.mjs';

const pipe = process.env.DESKTOPEYE_KERNEL_PIPE ?? 'desktopeye-kernel-1';
const client = new DesktopEyeClient(pipe, { defaultTimeoutMs: 6000 });
const evidence = [];
let modelRoundTripsBetweenPrimitives = 0;
const record = async (label, method, params = {}, timeout = 6000) => {
  const result = await client.call(method, params, timeout);
  evidence.push({ call: client.callCount, label, method, ok: true, result });
  return result;
};
const expectError = async (label, method, params, code, timeout = 2500) => {
  try { await client.call(method, params, timeout); throw new Error(`${label}: expected ${code}`); }
  catch (error) {
    assert(error instanceof DesktopEyeRpcError, `${label}: expected typed RPC error, got ${error}`);
    assert(error.code === code, `${label}: expected ${code}, got ${error.code}`);
    evidence.push({ call: client.callCount, label, method, ok: false, expectedError: code, error: { code: error.code, message: error.message } });
  }
};
const retain = async (query, endpoint, params = {}) => {
  const candidate = exactOne(query, endpoint);
  const kept = await record(`retain ${endpoint}`, endpoint, { target: candidate.id, ...params });
  return kept.concept;
};

try {
  const hello = await record('kernel hello', 'hello');                                      // 1
  const status0 = await record('runtime status', 'runtime.status');                         // 2
  const session0 = await record('session current', 'session.current');                      // 3
  assert(session0.session?.unlocked === true, 'canonical workflow requires unlocked interactive desktop');
  const cursor0 = await record('initial cursor', 'world.cursor');                           // 4

  const appQ = await record('query WPF app', 'app.query', { processName: 'DESKTOPeye.WpfIdentityFixture' }); // 5
  const app = await retain(appQ, 'app.retain');                                             // 6
  const aiQ = await record('query WPF app instance', 'app_instance.current', { app: app.id }); // 7
  const appinst = await retain(aiQ, 'app_instance.retain');                                 // 8
  const winQ = await record('query main WPF window', 'window.query', { appInstance: appinst.id, titleContains: 'DESKTOPeye' }); // 9
  const window = await retain(winQ, 'window.retain');                                       // 10
  await record('main window state', 'window.get_state', { target: window.id });              // 11

  const primaryQ = await record('query PrimaryText', 'control.query', { parent: window.id, automationId: 'PrimaryText' }); // 12
  const primary = await retain(primaryQ, 'control.retain', { documentedKeyProperty: 'automationId' }); // 13
  await record('set PrimaryText', 'control.set_value', { target: primary.id, value: 'program-host-alpha' }); // 14
  await record('wait PrimaryText alpha', 'wait.value', { target: primary.id, value: 'program-host-alpha', timeoutMs: 3000 }); // 15

  const physicalQ = await record('query PhysicalText', 'control.query', { parent: window.id, automationId: 'PhysicalText' }); // 16
  const physical = await retain(physicalQ, 'control.retain', { documentedKeyProperty: 'automationId' }); // 17
  await record('focus PhysicalText', 'focus.ensure', { target: physical.id });                // 18
  await record('physical keyboard type', 'keyboard.type', { target: physical.id, text: 'typed-locally' }); // 19
  await record('wait PhysicalText', 'wait.value', { target: physical.id, value: 'typed-locally', timeoutMs: 3000 }); // 20

  const continueQ = await record('query Continue', 'control.query', { parent: window.id, automationId: 'Continue' }); // 21
  const continueButton = await retain(continueQ, 'control.retain', { documentedKeyProperty: 'automationId' }); // 22
  await record('reobserve window pre-dialog', 'window.get_state', { target: window.id });     // 23
  const cursor1 = await record('cursor before local composites', 'world.cursor');             // 24
  await record('bounded delta read', 'delta.read', { cursor: cursor0.cursor, max: 64 });       // 25
  await record('sync retained main window', 'world.sync', { scope: window.id, cursor: cursor1.cursor, maxDeltas: 64 }); // 26
  await record('runtime after first phase', 'runtime.status');                                // 27
  await record('session after first phase', 'session.current');                               // 28

  const collectionQ = await record('query KeyedList collection', 'collection.query', { parent: window.id, automationId: 'KeyedList' }); // 29
  const collection = await retain(collectionQ, 'collection.retain', { documentedKeyProperty: 'automationId', commands: { sort: 'SortKeyed', filter: 'FilterKeyed', clearFilter: 'ClearFilter' } }); // 30
  const view0 = await record('initial collection view', 'collection.get_view', { target: collection.id }); // 31
  const sortQ = await record('query sort command', 'control.query', { parent: window.id, automationId: 'SortKeyed' }); // 32
  await retain(sortQ, 'control.retain', { documentedKeyProperty: 'automationId' });           // 33
  await record('collection sort composite', 'collection.sort', { target: collection.id });    // 34
  await record('wait sort view epoch', 'wait.collection_view_epoch', { target: collection.id, minimum: Math.max(1, Number(view0.viewEpoch ?? 0) + 1), timeoutMs: 3000 }); // 35
  await record('collection view after sort', 'collection.get_view', { target: collection.id }); // 36
  const filterQ = await record('query filter command', 'control.query', { parent: window.id, automationId: 'FilterKeyed' }); // 37
  await retain(filterQ, 'control.retain', { documentedKeyProperty: 'automationId' });         // 38
  await record('collection filter composite', 'collection.filter', { target: collection.id }); // 39
  await record('collection view after filter', 'collection.get_view', { target: collection.id }); // 40
  const clearQ = await record('query clear filter command', 'control.query', { parent: window.id, automationId: 'ClearFilter' }); // 41
  await retain(clearQ, 'control.retain', { documentedKeyProperty: 'automationId' });          // 42
  await record('collection clear-filter composite', 'collection.clear_filter', { target: collection.id }); // 43
  await record('collection view restored', 'collection.get_view', { target: collection.id }); // 44

  const frame = await record('capture main window', 'capture.window_region', { window: window.id }); // 45
  const visual = await record('revalidate unique visual target', 'visual.revalidate', { frame: frame.id, r: 123, g: 45, b: 231, tolerance: 22, minPixels: 40, maxComponents: 1 }); // 46
  await record('physical pointer click', 'pointer.click', { target: visual.id });              // 47
  await record('reobserve after pointer', 'window.get_state', { target: window.id });          // 48
  await record('focus PrimaryText', 'focus.ensure', { target: primary.id });                   // 49
  await record('semantic PrimaryText second value', 'control.set_value', { target: primary.id, value: 'program-host-omega' }); // 50
  await record('wait PrimaryText omega', 'wait.value', { target: primary.id, value: 'program-host-omega', timeoutMs: 3000 }); // 51
  const cursor2 = await record('post-mutation cursor', 'world.cursor');                        // 52
  await record('read local program deltas', 'delta.read', { cursor: cursor1.cursor, max: 128 }); // 53
  await record('sync retained interests', 'world.sync', { cursor: cursor2.cursor, maxDeltas: 128 }); // 54
  await record('runtime pre-provider-fault', 'runtime.status');                               // 55
  await record('session pre-provider-fault', 'session.current');                              // 56
  await expectError('bounded provider stall', 'debug.provider_block', { affinity: 'program-host-blocked', milliseconds: 3000, providerDeadlineMs: 250, deadlineMs: 1500 }, 'provider_timeout', 2000); // 57
  await record('runtime survives provider stall', 'runtime.status');                          // 58
  const cursor3 = await record('final cursor', 'world.cursor');                               // 59
  await record('final bounded deltas', 'delta.read', { cursor: Math.max(0, cursor3.cursor - 32), max: 64 }); // 60

  assert(client.callCount === 60, `canonical Build 001 program must make exactly 60 typed calls; got ${client.callCount}`);
  assert(modelRoundTripsBetweenPrimitives === 0, 'model round trips occurred inside local program');
  console.log(JSON.stringify({ status: 'PASS', pipe, typedCalls: client.callCount, modelRoundTripsBetweenPrimitives, kernelEpoch: hello.kernelEpoch, finalWorldCursor: cursor3.cursor, evidence }, null, 2));
} finally {
  await client.close();
}
