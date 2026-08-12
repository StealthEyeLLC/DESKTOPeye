import net from 'node:net';
import { randomUUID } from 'node:crypto';
import { DesktopEyeClient, DesktopEyeRpcError, assert } from '../sdk/index.mjs';

const pipe=`desktopeye-program-host-test-${randomUUID().replaceAll('-','')}`;
const pipePath=`\\\\.\\pipe\\${pipe}`;
let connections=0;
let requests=0;
const methods=[];
const server=net.createServer(socket=>{
  connections++;
  socket.setEncoding('utf8');
  let buffer='';
  socket.on('data',chunk=>{
    buffer+=chunk;
    for(;;){
      const i=buffer.indexOf('\n'); if(i<0)break;
      const line=buffer.slice(0,i).trim(); buffer=buffer.slice(i+1); if(!line)continue;
      const req=JSON.parse(line); requests++; methods.push(req.method);
      const response=req.method==='expected.failure'
        ? {version:1,id:req.id,ok:false,error:{code:'stale',message:'fixture stale'}}
        : {version:1,id:req.id,ok:true,result:{method:req.method,ordinal:requests,echo:req.params}};
      socket.write(JSON.stringify(response)+'\n');
    }
  });
});

await new Promise((resolve,reject)=>{server.once('error',reject);server.listen(pipePath,resolve);});
const client=new DesktopEyeClient(pipe);
try {
  const one=await client.call('session.current',{stage:1});
  const two=await client.call('world.cursor',{stage:2});
  const failure=await client.call('expected.failure',{},1000,{expectError:'stale'});
  const three=await client.call('delta.read',{stage:3});
  assert(one.method==='session.current','first call result');
  assert(two.ordinal===2,'second call ordinal');
  assert(failure.expectedError && failure.error.code==='stale','typed expected error');
  assert(three.method==='delta.read','fourth call result');
  assert(client.callCount===4,`expected 4 meaningful calls, got ${client.callCount}`);
  assert(connections===1,`one local pipe connection expected, got ${connections}`);
  assert(requests===4,`four requests expected, got ${requests}`);
  assert(methods.join(',')==='session.current,world.cursor,expected.failure,delta.read','request order');
  console.log(JSON.stringify({status:'PASS',connections,requests,callCount:client.callCount,methods,modelCallsBetweenPrimitives:0}));
} catch (error) {
  if(error instanceof DesktopEyeRpcError) console.error(error.error);
  throw error;
} finally {
  client.close();
  await new Promise(resolve=>server.close(resolve));
}