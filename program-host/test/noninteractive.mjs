import net from 'node:net';
import crypto from 'node:crypto';
import { DesktopEyeClient, DesktopEyeRpcError, assert } from '../sdk/index.mjs';

const name = `desktopeye-programhost-test-${process.pid}-${crypto.randomBytes(4).toString('hex')}`;
const pipe = `\\\\.\\pipe\\${name}`;
let connections = 0;
let requests = 0;
const methods = [];

const server = net.createServer(socket => {
  connections++;
  socket.setEncoding('utf8');
  let buffer = '';
  socket.on('data', chunk => {
    buffer += chunk;
    for (;;) {
      const nl = buffer.indexOf('\n');
      if (nl < 0) break;
      const line = buffer.slice(0, nl).trim(); buffer = buffer.slice(nl + 1);
      if (!line) continue;
      const req = JSON.parse(line); requests++; methods.push(req.method);
      let response;
      if (req.method === 'hello') response = { version: 1, id: req.id, ok: true, result: { version: 1, kernelEpoch: 7 } };
      else if (req.method === 'session.current') response = { version: 1, id: req.id, ok: true, result: { session: { sessionId: 1, inputDesktop: 'Default', unlocked: true } } };
      else if (req.method === 'world.cursor') response = { version: 1, id: req.id, ok: true, result: { cursor: 42, floor: 1 } };
      else response = { version: 1, id: req.id, ok: false, error: { code: 'unsupported', message: req.method } };
      socket.write(`${JSON.stringify(response)}\n`);
    }
  });
});

await new Promise((resolve, reject) => { server.once('error', reject); server.listen(pipe, resolve); });
const client = new DesktopEyeClient(name);
let typedError = false;
try {
  const hello = await client.call('hello');
  const session = await client.call('session.current');
  const cursor = await client.call('world.cursor');
  try { await client.call('unsupported.test'); } catch (error) { typedError = error instanceof DesktopEyeRpcError && error.code === 'unsupported'; }
  assert(hello.kernelEpoch === 7, 'hello result mismatch');
  assert(session.session.inputDesktop === 'Default', 'session result mismatch');
  assert(cursor.cursor === 42, 'cursor mismatch');
  assert(typedError, 'typed RPC error not preserved');
  assert(client.callCount === 4, `expected 4 typed calls, got ${client.callCount}`);
  assert(connections === 1, `expected one pipe connection, got ${connections}`);
  assert(requests === 4, `expected four requests, got ${requests}`);
  assert(methods.join(',') === 'hello,session.current,world.cursor,unsupported.test', `unexpected method order: ${methods.join(',')}`);
  console.log(JSON.stringify({ status: 'PASS', typedCalls: client.callCount, pipeConnections: connections, modelRoundTripsBetweenPrimitives: 0, methods }, null, 2));
} finally {
  await client.close();
  await new Promise(resolve => server.close(resolve));
}
