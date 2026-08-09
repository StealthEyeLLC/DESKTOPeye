using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace DESKTOPeye.Win32Adversary;

internal static class Program
{
    [STAThread]
    static void Main(){ApplicationConfiguration.Initialize();Application.Run(new AdversaryForm());}
}

public sealed class AdversaryForm : Form
{
    readonly Button _target=new(){Name="MovingTarget",Text="Target",AccessibleName="Moving Target",Size=new(120,44),Location=new(60,70)};
    readonly Button _wrong=new(){Name="WrongTarget",Text="Wrong Target",AccessibleName="Wrong Target",Size=new(120,44),Location=new(250,70)};
    readonly Button _overlay=new(){Name="Overlay",Text="Overlay",AccessibleName="Destructive Overlay",Size=new(120,44),Location=new(60,70),Visible=false};
    readonly TextBox _focus=new(){Name="FocusTarget",AccessibleName="Focus Target",Size=new(250,30),Location=new(60,160)};
    readonly Label _state=new(){Name="StateLabel",AccessibleName="Adversary State",AutoSize=true,Location=new(60,220),Text="ready"};
    readonly List<Form> _twins=[]; FocusThiefForm? _thief; HangForm? _hang; NativeTargetForm? _nativeTarget; OracleServer? _oracle;
    int _targetHits,_wrongHits,_recreateGeneration,_nativeGeneration,_focusSteals; Point _targetHome=new(60,70);
    public AdversaryForm()
    {
        Text="DESKTOPeye Win32 Adversary";Name="AdversaryMain";AccessibleName="DESKTOPeye Win32 Adversary";StartPosition=FormStartPosition.Manual;Location=new(80,80);Size=new(620,420);
        Controls.AddRange([_target,_wrong,_overlay,_focus,_state]);_overlay.BringToFront();_target.Click+=(_,_)=>{_targetHits++;UpdateState("target-hit");};_wrong.Click+=(_,_)=>{_wrongHits++;UpdateState("wrong-hit");};
        Shown+=(_,_)=>{_oracle=new OracleServer(this,$"desktopeye-win32-adversary-{Environment.ProcessId}");_oracle.Start();CreateNativeTarget();};FormClosed+=(_,_)=>{_oracle?.Dispose();CloseAux();};
    }
    void UpdateState(string reason){_state.Text=$"{reason};target={_targetHits};wrong={_wrongHits};recreate={_recreateGeneration};native={_nativeGeneration};focusSteals={_focusSteals}";}
    void CreateNativeTarget(){_nativeGeneration++;_nativeTarget=new NativeTargetForm(_nativeGeneration){Owner=this};_nativeTarget.Show();}
    void CloseAux(){foreach(var t in _twins.ToArray())t.Close();_twins.Clear();_thief?.Close();_thief=null;_hang?.Close();_hang=null;_nativeTarget?.Close();_nativeTarget=null;}
    public AdversaryState Snapshot()=>new(Environment.ProcessId,Handle.ToInt64(),_target.Handle.ToInt64(),_wrong.Handle.ToInt64(),_overlay.Visible?_overlay.Handle.ToInt64():0,_focus.Handle.ToInt64(),_targetHits,_wrongHits,_recreateGeneration,_nativeGeneration,_focusSteals,_target.Left,_target.Top,_wrong.Left,_wrong.Top,_overlay.Visible,_focus.Text,_twins.Select(x=>x.Handle.ToInt64()).ToArray(),_thief?.Handle.ToInt64()??0,_hang?.Handle.ToInt64()??0,_nativeTarget?.Handle.ToInt64()??0,_state.Text);
    public Task<object> Setup(string action)=>InvokeAsync<object>(()=>
    {
        switch(action)
        {
            case "move_target": var old=_target.Location;_target.Location=new Point(old.X==_targetHome.X?360:_targetHome.X,old.Y+60);_wrong.Location=old;_overlay.Location=_target.Location;UpdateState("target-moved");break;
            case "reset_target": _target.Location=_targetHome;_wrong.Location=new Point(250,70);_overlay.Location=_targetHome;_overlay.Visible=false;UpdateState("target-reset");break;
            case "overlay_on": _overlay.Location=_target.Location;_overlay.Visible=true;_overlay.BringToFront();UpdateState("overlay-on");break;
            case "overlay_off": _overlay.Visible=false;UpdateState("overlay-off");break;
            case "focus_target": Activate();_focus.Focus();UpdateState("focus-target");break;
            case "focus_thief": _thief??=new FocusThiefForm();if(!_thief.Visible)_thief.Show();_thief.Activate();_thief.FocusField();_focusSteals++;UpdateState("focus-stolen");break;
            case "open_twins": foreach(var t in _twins.ToArray())t.Close();_twins.Clear();for(int i=0;i<2;i++){var f=new Form{Name="TwinWindow",Text="Same Title",AccessibleName="Same Title",Size=new(280,150),StartPosition=FormStartPosition.Manual,Location=new Point(720+i*300,100)};var b=new Button{Name="TwinAction",Text="Action",AccessibleName="Action",Dock=DockStyle.Fill};f.Controls.Add(b);f.Show();_twins.Add(f);}UpdateState("twins-open");break;
            case "close_twins": foreach(var t in _twins.ToArray())t.Close();_twins.Clear();UpdateState("twins-closed");break;
            case "recreate_native": _nativeTarget?.Close();_nativeTarget=null;_recreateGeneration++;CreateNativeTarget();UpdateState("native-recreated");break;
            case "hang_open": _hang??=new HangForm();if(!_hang.Visible)_hang.Show();UpdateState("hang-open");break;
            case "hang_on": _hang??=new HangForm();if(!_hang.Visible)_hang.Show();_hang.HangGetObject=true;UpdateState("hang-on");break;
            case "hang_off": if(_hang!=null)_hang.HangGetObject=false;UpdateState("hang-off");break;
            case "close_aux": CloseAux();CreateNativeTarget();UpdateState("aux-closed");break;
            case "churn": for(int i=0;i<80;i++){using var f=new Form{Name="Churn",Text="Churn",ShowInTaskbar=false,StartPosition=FormStartPosition.Manual,Location=new Point(-2000,-2000),Size=new(100,80)};f.Show();var h=f.Handle.ToInt64();f.Close();Application.DoEvents();}UpdateState("churned");break;
            default: throw new InvalidOperationException(action);
        }
        return Snapshot();
    });
    Task<T> InvokeAsync<T>(Func<T> f){var tcs=new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);BeginInvoke(()=>{try{tcs.SetResult(f());}catch(Exception ex){tcs.SetException(ex);}});return tcs.Task;}
}

public sealed class NativeTargetForm:Form
{
    public int Generation{get;} public NativeTargetForm(int generation){Generation=generation;Name="NativeTargetWindow";Text="Adversary Target";AccessibleName="Adversary Target";Size=new(320,160);StartPosition=FormStartPosition.Manual;Location=new Point(760,420);var b=new Button{Name="NativeAction",Text="Native Action",AccessibleName="Native Action",Dock=DockStyle.Fill};Controls.Add(b);}
}
public sealed class FocusThiefForm:Form
{
    readonly TextBox _box=new(){Name="FocusThiefBox",AccessibleName="Focus Thief",Dock=DockStyle.Fill};public FocusThiefForm(){Name="FocusThiefWindow";Text="Focus Thief";AccessibleName="Focus Thief";Size=new(300,120);StartPosition=FormStartPosition.Manual;Location=new Point(700,700);Controls.Add(_box);}public void FocusField()=>_box.Focus();
}
public sealed class HangForm:Form
{
    const int WM_GETOBJECT=0x003D; public volatile bool HangGetObject; public HangForm(){Name="HangWindow";Text="Provider Hang";AccessibleName="Provider Hang";Size=new(260,120);StartPosition=FormStartPosition.Manual;Location=new Point(1080,420);Controls.Add(new Label{Name="HangLabel",Text="Hang target",AccessibleName="Hang target",Dock=DockStyle.Fill});}
    protected override void WndProc(ref Message m){if(m.Msg==WM_GETOBJECT&&HangGetObject)Thread.Sleep(30000);base.WndProc(ref m);}
}
public sealed record AdversaryState(int Pid,long MainHwnd,long TargetHwnd,long WrongHwnd,long OverlayHwnd,long FocusHwnd,int TargetHits,int WrongHits,int RecreateGeneration,int NativeGeneration,int FocusSteals,int TargetX,int TargetY,int WrongX,int WrongY,bool Overlay,string FocusText,long[] TwinHwnds,long FocusThiefHwnd,long HangHwnd,long NativeTargetHwnd,string State);

sealed class OracleServer:IDisposable
{
    readonly AdversaryForm _form;readonly string _pipe;readonly CancellationTokenSource _cts=new();Task? _loop;public OracleServer(AdversaryForm form,string pipe){_form=form;_pipe=pipe;}public void Start()=>_loop=Task.Run(Run);
    async Task Run(){while(!_cts.IsCancellationRequested){await using var s=new NamedPipeServerStream(_pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);try{await s.WaitForConnectionAsync(_cts.Token);using var r=new StreamReader(s,Encoding.UTF8,false,4096,true);using var w=new StreamWriter(s,new UTF8Encoding(false),4096,true){AutoFlush=true};string? line;while((line=await r.ReadLineAsync(_cts.Token))!=null){var q=JsonDocument.Parse(line).RootElement;var cmd=q.GetProperty("cmd").GetString();object result=cmd switch{"state"=>_form.Snapshot(),"snapshot"=>_form.Snapshot(),"setup"=>await _form.Setup(q.GetProperty("action").GetString()!),_=>throw new InvalidOperationException(cmd)};await w.WriteLineAsync(JsonSerializer.Serialize(result));}}catch(OperationCanceledException){break;}catch{}}}
    public void Dispose(){_cts.Cancel();try{_loop?.Wait(500);}catch{}_cts.Dispose();}
}