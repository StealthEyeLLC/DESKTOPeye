import fs from 'node:fs';
import path from 'node:path';
import { DesktopEyeClient, assert, firstCandidate } from '../sdk/index.mjs';

const outPath=process.argv[2] ?? path.resolve('program-host/acceptance/build001-result.json');
const client=new DesktopEyeClient(process.env.DESKTOPEYE_PIPE ?? 'desktopeye-kernel-1');
const evidence={startedAt:new Date().toISOString(),node:process.version,modelCallsBetweenPrimitives:0,workflow:'canonical Build 001 60-call Program Host',calls:[]};
let ids={};
try {
  // 1-11 session/app/window/world spine
  const session=await client.call('session.current');                                                       // 1
  const cursor0=await client.call('world.cursor');                                                         // 2
  const aq=await client.call('app.query',{processName:'DESKTOPeye.WpfIdentityFixture',windowTitle:'DESKTOPeye Identity Fixture'}); // 3
  const app=await client.call('app.retain',{target:firstCandidate(aq,'fixture app').id});                    // 4
  ids.app=app.concept.id;
  const aiq=await client.call('app_instance.current',{app:ids.app});                                       // 5
  const ai=await client.call('app_instance.retain',{target:firstCandidate(aiq,'fixture appinst').id});      // 6
  ids.appinst=ai.concept.id;
  const wq=await client.call('window.query',{appInstance:ids.appinst,title:'DESKTOPeye Identity Fixture',automationId:'FixtureMainWindow'}); // 7
  const wr=await client.call('window.retain',{target:firstCandidate(wq,'fixture window').id,documentedKeyProperty:'automationId'}); // 8
  ids.window=wr.concept.id;
  const ws=await client.call('window.get_state',{target:ids.window});                                      // 9
  const sync=await client.call('world.sync',{scope:ids.window,cursor:cursor0.cursor,maxDeltas:128});         // 10
  const deltas0=await client.call('delta.read',{cursor:cursor0.cursor,max:128,scope:ids.window});            // 11

  // 12-34 retained controls + virtualization
  const tq=await client.call('control.query',{parent:ids.window,automationId:'PrimaryText',controlType:50004,limit:8}); // 12
  const tr=await client.call('control.retain',{target:firstCandidate(tq,'textbox').id,documentedKeyProperty:'automationId'}); // 13
  ids.textbox=tr.concept.id;
  const gq=await client.call('control.query',{parent:ids.window,automationId:'FeatureToggle',limit:8});       // 14
  const gr=await client.call('control.retain',{target:firstCandidate(gq,'toggle').id,documentedKeyProperty:'automationId'}); // 15
  ids.toggle=gr.concept.id;
  const cq=await client.call('collection.query',{parent:ids.window,automationId:'KeyedList',limit:8});        // 16
  const cr=await client.call('collection.retain',{target:firstCandidate(cq,'keyed collection').id,documentedKeyProperty:'automationId',commands:{sort:'SortKeyed',filter:'FilterKeyed',clearFilter:'ClearFilter'}}); // 17
  ids.collection=cr.concept.id;
  const view0=await client.call('collection.get_view',{target:ids.collection});                             // 18
  const iq=await client.call('item.find_by_key',{collection:ids.collection,key:'key-142'});                 // 19
  const ir=await client.call('item.retain',{target:firstCandidate(iq,'keyed item').id});                    // 20
  ids.item=ir.concept.id;
  const realize=await client.call('item.realize',{target:ids.item});                                       // 21
  const scroll=await client.call('item.scroll_into_view',{target:ids.item});                               // 22
  const select=await client.call('item.select',{target:ids.item});                                         // 23
  const selectWait=await client.call('wait.selection',{target:ids.item,timeoutMs:4000});                    // 24
  const deltas1=await client.call('delta.read',{cursor:cursor0.cursor,max:128});                             // 25
  const sort=await client.call('collection.sort',{target:ids.collection});                                 // 26
  const epoch1=await client.call('wait.collection_view_epoch',{target:ids.collection,greaterThan:view0.viewEpoch,timeoutMs:4000}); // 27
  const itemAfterSort=await client.call('item.resolve',{target:ids.item});                                  // 28
  const itemRep=await client.call('item.get_representation',{target:ids.item});                             // 29
  const filter=await client.call('collection.filter',{target:ids.collection});                             // 30
  const virtualWait=await client.call('wait.control_state',{target:ids.item,identity:'virtualized',timeoutMs:4000}); // 31
  const clear=await client.call('collection.clear_filter',{target:ids.collection});                         // 32
  const epoch2=await client.call('wait.collection_view_epoch',{target:ids.collection,greaterThan:epoch1.current.viewEpoch,timeoutMs:4000}); // 33
  const itemAfterClear=await client.call('item.resolve',{target:ids.item});                                 // 34

  // 35-43 menu and semantic value
  const mq=await client.call('control.query',{parent:ids.window,automationId:'ActionsMenu',limit:8});         // 35
  const mr=await client.call('control.retain',{target:firstCandidate(mq,'Actions menu').id,documentedKeyProperty:'automationId'}); // 36
  ids.menu=mr.concept.id;
  const expand=await client.call('control.expand',{target:ids.menu});                                      // 37
  const popup=await client.call('wait.popup',{target:ids.menu,timeoutMs:4000});                             // 38
  const cmdq=await client.call('control.query',{parent:ids.window,automationId:'MenuCommand',visibleOnly:true,limit:8}); // 39
  const cmd=await client.call('control.invoke',{target:firstCandidate(cmdq,'visible menu command').id});     // 40
  const cmdPost=await client.call('wait.value',{parent:ids.window,automationId:'StateText',contains:'menu-command',timeoutMs:4000}); // 41
  const setv=await client.call('control.set_value',{target:ids.textbox,value:'program-host-60'});             // 42
  const valueWait=await client.call('wait.value',{target:ids.textbox,equals:'program-host-60',timeoutMs:4000}); // 43

  // 44-52 identical dialog stale branch
  const contq=await client.call('control.query',{parent:ids.window,automationId:'Continue',limit:8});         // 44
  const openA=await client.call('control.invoke',{target:firstCandidate(contq,'Continue').id});              // 45
  const dlgAq=await client.call('wait.dialog_exists',{appInstance:ids.appinst,title:'Confirm',timeoutMs:4000}); // 46
  const dlgAr=await client.call('dialog.retain',{target:firstCandidate(dlgAq.current,'Confirm A').id});      // 47
  ids.dialogA=dlgAr.concept.id;
  const okq=await client.call('control.query',{parent:ids.dialogA,automationId:'DialogOk',limit:8});          // 48
  const closeA=await client.call('control.invoke',{target:firstCandidate(okq,'Confirm A OK').id});           // 49
  ids.oldDialogButton=closeA.targetId;
  const closeWait=await client.call('wait.dialog_closes',{target:ids.dialogA,timeoutMs:4000});               // 50
  const dlgBq=await client.call('wait.dialog_exists',{appInstance:ids.appinst,title:'Confirm',timeoutMs:4000}); // 51
  const staleOld=await client.call('control.invoke',{target:ids.oldDialogButton},7000,{expectError:'destroyed'}); // 52

  // 53-60 visual + physical + final delta
  const frame=await client.call('capture.window_region',{window:ids.window});                               // 53
  const visual=await client.call('visual.revalidate',{frame:frame.id,r:123,g:45,b:231,tolerance:8,minPixels:500,maxComponents:1}); // 54
  const pointer=await client.call('pointer.click',{target:visual.id});                                      // 55
  const pointerPost=await client.call('wait.value',{parent:ids.window,automationId:'StateText',contains:'canvas-hit',timeoutMs:4000}); // 56
  // PhysicalText is deliberately pre-retained before the Program Host invocation; the canonical local program starts from retained world state and spends no extra primitive locating it.
  ids.physical=process.env.DESKTOPEYE_PHYSICAL_ID;
  assert(ids.physical,'DESKTOPEYE_PHYSICAL_ID must be pre-retained before Program Host invocation');
  const focus=await client.call('focus.ensure',{target:ids.physical});                                      // 57
  const typed=await client.call('keyboard.type',{target:ids.physical,text:'host-keyboard'});                // 58
  const typedPost=await client.call('wait.value',{target:ids.physical,contains:'host-keyboard',timeoutMs:4000}); // 59
  const finalDelta=await client.call('delta.read',{cursor:deltas1.head ?? cursor0.cursor,max:256});           // 60

  assert(client.callCount===60,`exactly 60 meaningful calls required, got ${client.callCount}`);
  assert(selectWait.satisfied,'selection wait'); assert(valueWait.satisfied,'value wait'); assert(closeWait.satisfied,'dialog A close'); assert(pointerPost.satisfied,'visual pointer postcondition'); assert(typedPost.satisfied,'physical keyboard postcondition');
  assert(itemAfterSort.id===ids.item && itemAfterClear.id===ids.item,'logical keyed item must survive view changes');
  assert(staleOld.expectedError && staleOld.error.code==='destroyed','old A button must be destroyed');
  evidence.completedAt=new Date().toISOString();evidence.status='PASS';evidence.operationCount=client.callCount;evidence.ids=ids;evidence.summary={sessionId:session.session?.sessionId??session.sessionId,app:ids.app,appinst:ids.appinst,window:ids.window,collection:ids.collection,item:ids.item,semanticRoutes:[setv.route,select.route,cmd.route],visual:{frame:frame.id,descriptor:visual.id,pointerAssurance:pointer.assurance},keyboard:{assurance:typed.assurance},staleBranch:staleOld.error.code,deltaCount:finalDelta.deltas?.length??0,modelCallsBetweenPrimitives:0};evidence.calls=client.calls;
} catch (e) { evidence.completedAt=new Date().toISOString();evidence.status='FAIL';evidence.error={name:e.name,message:e.message,stack:e.stack,error:e.error??null};evidence.operationCount=client.callCount;evidence.calls=client.calls;process.exitCode=1; }
finally { client.close(); fs.mkdirSync(path.dirname(outPath),{recursive:true}); fs.writeFileSync(outPath,JSON.stringify(evidence,null,2)); console.log(JSON.stringify({status:evidence.status,operationCount:evidence.operationCount,error:evidence.error??null,summary:evidence.summary??null})); }