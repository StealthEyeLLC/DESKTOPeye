import net from 'node:net';
import crypto from 'node:crypto';

export class DesktopEyeRpcError extends Error {
  constructor(error, method) {
    super(`${method}: ${error?.code ?? 'native_error'}: ${error?.message ?? 'RPC failed'}`);
    this.name = 'DesktopEyeRpcError';
    this.code = error?.code ?? 'native_error';
    this.nativeCode = error?.nativeCode ?? null;
    this.detail = error?.detail ?? null;
    this.method = method;
  }
}

export class DesktopEyeClient {
  constructor(pipe, { defaultTimeoutMs = 5000 } = {}) {
    this.pipe = pipe.startsWith('\\\\.\\pipe\\') ? pipe : `\\\\.\\pipe\\${pipe}`;
    this.defaultTimeoutMs = defaultTimeoutMs;
    this.socket = null;
    this.buffer = '';
    this.pending = new Map();
    this.callCount = 0;
  }

  async connect(timeoutMs = 3000) {
    if (this.socket && !this.socket.destroyed) return this;
    await new Promise((resolve, reject) => {
      const socket = net.createConnection(this.pipe);
      const timer = setTimeout(() => { socket.destroy(); reject(new Error(`pipe connect timeout: ${this.pipe}`)); }, timeoutMs);
      socket.setEncoding('utf8');
      socket.once('connect', () => { clearTimeout(timer); this.socket = socket; this.#wire(); resolve(); });
      socket.once('error', error => { clearTimeout(timer); reject(error); });
    });
    return this;
  }

  #wire() {
    this.socket.on('data', chunk => {
      this.buffer += chunk;
      for (;;) {
        const newline = this.buffer.indexOf('\n');
        if (newline < 0) break;
        const line = this.buffer.slice(0, newline).trim();
        this.buffer = this.buffer.slice(newline + 1);
        if (!line) continue;
        let message;
        try { message = JSON.parse(line); } catch { continue; }
        const pending = this.pending.get(message.id);
        if (!pending) continue;
        this.pending.delete(message.id);
        clearTimeout(pending.timer);
        if (message.ok) pending.resolve(message.result ?? {});
        else pending.reject(new DesktopEyeRpcError(message.error, pending.method));
      }
    });
    const fail = error => {
      for (const pending of this.pending.values()) { clearTimeout(pending.timer); pending.reject(error); }
      this.pending.clear();
    };
    this.socket.on('error', fail);
    this.socket.on('close', () => fail(new Error(`pipe closed: ${this.pipe}`)));
  }

  async call(method, params = {}, timeoutMs = this.defaultTimeoutMs) {
    await this.connect(Math.min(timeoutMs, 3000));
    const id = crypto.randomUUID().replaceAll('-', '');
    const request = { version: 1, id, method, params, deadlineMs: timeoutMs };
    this.callCount++;
    return await new Promise((resolve, reject) => {
      const timer = setTimeout(() => { this.pending.delete(id); reject(new Error(`${method}: client timeout`)); }, timeoutMs + 250);
      this.pending.set(id, { resolve, reject, timer, method });
      this.socket.write(`${JSON.stringify(request)}\n`, 'utf8', error => {
        if (!error) return;
        clearTimeout(timer); this.pending.delete(id); reject(error);
      });
    });
  }

  async close() {
    if (!this.socket) return;
    const socket = this.socket; this.socket = null;
    await new Promise(resolve => { socket.once('close', resolve); socket.end(); setTimeout(() => { if (!socket.destroyed) socket.destroy(); }, 250); });
  }
}

export function assert(condition, message) { if (!condition) throw new Error(message); }
export function firstCandidate(result, label = 'query') {
  assert(result && Array.isArray(result.candidates), `${label}: missing candidates`);
  assert(result.candidates.length >= 1, `${label}: no candidates`);
  return result.candidates[0];
}
export function exactOne(result, label = 'query') {
  assert(result && result.count === 1 && Array.isArray(result.candidates) && result.candidates.length === 1, `${label}: expected exactly one candidate, got ${result?.count ?? 'unknown'}`);
  return result.candidates[0];
}
