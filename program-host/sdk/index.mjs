import net from 'node:net';
import { randomUUID } from 'node:crypto';

export class DesktopEyeRpcError extends Error {
  constructor(error, method) { super(`${method}: ${error?.code ?? 'unknown'}: ${error?.message ?? 'RPC error'}`); this.name='DesktopEyeRpcError'; this.error=error; this.method=method; }
}

export class DesktopEyeClient {
  constructor(pipe='desktopeye-kernel-1') { this.pipe=pipe; this.socket=null; this.buffer=''; this.pending=new Map(); this.callCount=0; this.calls=[]; }
  async connect() {
    if (this.socket && !this.socket.destroyed) return;
    const path=`\\\\.\\pipe\\${this.pipe}`;
    await new Promise((resolve,reject)=>{
      const s=net.createConnection(path,()=>resolve()); this.socket=s; s.setEncoding('utf8');
      s.on('data',d=>this.#data(d)); s.on('error',e=>{ if(this.pending.size===0) reject(e); for(const [,p] of this.pending)p.reject(e); this.pending.clear(); });
      s.on('close',()=>{for(const [,p] of this.pending)p.reject(new Error('DESKTOPeye pipe closed'));this.pending.clear();});
    });
  }
  #data(d){ this.buffer+=d; for(;;){const i=this.buffer.indexOf('\n'); if(i<0)break; const line=this.buffer.slice(0,i).trim(); this.buffer=this.buffer.slice(i+1); if(!line)continue; const r=JSON.parse(line); const p=this.pending.get(r.id); if(p){this.pending.delete(r.id); p.resolve(r);} } }
  async call(method, params={}, deadlineMs=7000, {expectError=null, meaningful=true}={}) {
    await this.connect(); const id=randomUUID().replaceAll('-',''); const ordinal=meaningful?++this.callCount:this.callCount;
    const started=performance.now(); const response=await new Promise((resolve,reject)=>{const timer=setTimeout(()=>{this.pending.delete(id);reject(new Error(`${method}: client timeout`));},deadlineMs+1500);this.pending.set(id,{resolve:r=>{clearTimeout(timer);resolve(r);},reject:e=>{clearTimeout(timer);reject(e);}});this.socket.write(JSON.stringify({version:1,id,method,params,deadlineMs})+'\n');});
    const latencyMs=Math.round((performance.now()-started)*1000)/1000; if(meaningful)this.calls.push({ordinal,method,ok:response.ok,latencyMs,error:response.error??null});
    if(expectError){ if(response.ok)throw new Error(`${method}: expected error ${expectError} but succeeded`); if(response.error?.code!==expectError)throw new DesktopEyeRpcError(response.error,method); return {expectedError:true,error:response.error}; }
    if(!response.ok)throw new DesktopEyeRpcError(response.error,method); return response.result;
  }
  close(){this.socket?.end();this.socket?.destroy();this.socket=null;}
}

export function assert(cond,msg){if(!cond)throw new Error(`assertion failed: ${msg}`);}
export function firstCandidate(q,label){assert(q.count===1,`${label} expected exactly one candidate, got ${q.count}`);return q.candidates[0];}