using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DESKTOPeye.WpfIdentityFixture;

public partial class MainWindow : Window
{
    public ObservableCollection<RowItem> KeyedRows {get;}=new(); public ObservableCollection<string> UnkeyedRows{get;}=new();
    ICollectionView _keyedView; int _viewEpoch,_dialogGeneration,_replaceGeneration,_parentGeneration,_pageGeneration; readonly List<DialogWindow> _dialogs=new(); FixtureOracleServer? _oracle;
    public int MenuCount{get;private set;} public int DuplicateACount{get;private set;} public int DuplicateBCount{get;private set;} public int MissingIdCount{get;private set;} public int AsyncCount{get;private set;} public string SelectedKey{get;private set;}="";
    public MainWindow()
    {
        InitializeComponent(); DataContext=this; for(int i=0;i<240;i++)KeyedRows.Add(new($"key-{i:000}",$"Duplicate row {i%12:00}",i));for(int i=0;i<180;i++)UnkeyedRows.Add($"Unkeyed duplicate {i%8}");_keyedView=CollectionViewSource.GetDefaultView(KeyedRows); BuildDynamicParent(); VisualSurface.Hit+=hits=>State($"canvas-hit-{hits}"); Loaded+=OnLoaded; Closed+=(_,_)=>_oracle?.Dispose(); var t=new DispatcherTimer{Interval=TimeSpan.FromMilliseconds(350)};t.Tick+=(_,_)=>{t.Stop();AsyncAction.IsEnabled=true;State("async-enabled");};t.Start();
    }
    async void OnLoaded(object? s,RoutedEventArgs e){_oracle=new FixtureOracleServer(this,$"desktopeye-fixture-{Environment.ProcessId}");await _oracle.StartAsync();}
    void State(string s){StateText.Text=s;AutomationProperties.SetHelpText(StateText,$"dialogs={_dialogGeneration};view={_viewEpoch};replace={_replaceGeneration};parent={_parentGeneration};page={_pageGeneration};canvas={VisualSurface.Hits}");}
    void BuildDynamicParent(){DynamicParent.Children.Clear();var b=new Button{Content="Ephemeral Control",Name="EphemeralControl"};AutomationProperties.SetAutomationId(b,"EphemeralControl");b.Click+=(_,_)=>State($"ephemeral-hit-{_replaceGeneration}");DynamicParent.Children.Add(b);}
    void ReplaceControl_Click(object s,RoutedEventArgs e){_replaceGeneration++;BuildDynamicParent();State("control-replaced");}
    void RecreateParent_Click(object s,RoutedEventArgs e){_parentGeneration++;var old=DynamicParent;var border=(Border)old.Parent;DynamicParent=new StackPanel();AutomationProperties.SetAutomationId(DynamicParent,"DynamicParent");border.Child=DynamicParent;BuildDynamicParent();State("parent-recreated");}
    void Navigate_Click(object s,RoutedEventArgs e){_pageGeneration++;BuildDynamicParent();State("same-hwnd-navigation");}
    void OpenConfirm_Click(object s,RoutedEventArgs e){Dispatcher.BeginInvoke(()=>OpenDialog(false,true));}
    DialogWindow OpenDialog(bool modal,bool spawnReplacement=false){_dialogGeneration++;var d=new DialogWindow(_dialogGeneration){Owner=this};d.Closed+=(_,_)=>{_dialogs.Remove(d);State($"dialog-closed-{d.Generation}");if(spawnReplacement)Dispatcher.BeginInvoke(()=>OpenDialog(false,false));};_dialogs.Add(d);if(modal)d.ShowDialog();else d.Show();State($"dialog-opened-{d.Generation}");return d;}
    public void SpawnConcurrentDialogs(){OpenDialog(false,false);OpenDialog(false,false);}
    public void CloseDialogs(){foreach(var d in _dialogs.ToArray())d.Close();}
    void MenuCommand_Click(object s,RoutedEventArgs e){MenuCount++;State("menu-command");}
    void PrimaryText_TextChanged(object s,TextChangedEventArgs e){if(IsLoaded)State("primary-text");}
    void PhysicalText_TextChanged(object s,TextChangedEventArgs e){if(IsLoaded)State("physical-text");}
    void FeatureToggle_Changed(object s,RoutedEventArgs e){if(IsLoaded)State("toggle");}
    void AsyncAction_Click(object s,RoutedEventArgs e){AsyncCount++;State("async-action");}
    void DuplicateA_Click(object s,RoutedEventArgs e){DuplicateACount++;State("dup-a");} void DuplicateB_Click(object s,RoutedEventArgs e){DuplicateBCount++;State("dup-b");} void MissingId_Click(object s,RoutedEventArgs e){MissingIdCount++;State("missing-id");}
    void SortKeyed_Click(object s,RoutedEventArgs e){_keyedView.SortDescriptions.Clear();_keyedView.SortDescriptions.Add(new SortDescription(nameof(RowItem.Ordinal),_viewEpoch%2==0?ListSortDirection.Descending:ListSortDirection.Ascending));BumpView();}
    void FilterKeyed_Click(object s,RoutedEventArgs e){_keyedView.Filter=o=>o is RowItem r&&r.Ordinal%2==0;BumpView();}
    void ClearFilter_Click(object s,RoutedEventArgs e){_keyedView.Filter=null;BumpView();}
    void BumpView(){_viewEpoch++;ViewEpochText.Text=$"view:{_viewEpoch}";AutomationProperties.SetItemStatus(KeyedList,$"view-epoch:{_viewEpoch}");State("view-changed");}
    void KeyedList_SelectionChanged(object s,SelectionChangedEventArgs e){SelectedKey=(KeyedList.SelectedItem as RowItem)?.Key??"";State("selection");}
    public FixtureState Snapshot()=>new(Environment.ProcessId,new System.Windows.Interop.WindowInteropHelper(this).Handle.ToInt64(),_viewEpoch,_dialogGeneration,_replaceGeneration,_parentGeneration,_pageGeneration,MenuCount,DuplicateACount,DuplicateBCount,MissingIdCount,AsyncCount,SelectedKey,PrimaryText.Text,PhysicalText.Text,FeatureToggle.IsChecked==true,VisualSurface.Hits,VisualSurface.TargetX,VisualSurface.TargetY,VisualSurface.Overlay,_dialogs.Select(d=>new System.Windows.Interop.WindowInteropHelper(d).Handle.ToInt64()).ToArray());
    public async Task<object> SetupAsync(string action){return await Dispatcher.InvokeAsync<object>(()=>{switch(action){case"move_canvas":VisualSurface.MoveTarget();break;case"overlay_on":VisualSurface.Overlay=true;break;case"overlay_off":VisualSurface.Overlay=false;break;case"replace_control":ReplaceControl_Click(this,new RoutedEventArgs());break;case"recreate_parent":RecreateParent_Click(this,new RoutedEventArgs());break;case"navigate":Navigate_Click(this,new RoutedEventArgs());break;case"concurrent_dialogs":SpawnConcurrentDialogs();break;case"modal_dialog":Dispatcher.BeginInvoke(()=>OpenDialog(true,false));break;case"close_dialogs":CloseDialogs();break;case"focus_physical":PhysicalText.Focus();Keyboard.Focus(PhysicalText);break;case"focus_primary":PrimaryText.Focus();Keyboard.Focus(PrimaryText);break;case"disable_async":AsyncAction.IsEnabled=false;break;case"enable_async":AsyncAction.IsEnabled=true;break;case"visual_ambiguous_on":VisualSurface.Ambiguous=true;break;case"visual_ambiguous_off":VisualSurface.Ambiguous=false;break;case"capture_exclude_on":CaptureExclusion.Set(new System.Windows.Interop.WindowInteropHelper(this).Handle,true);break;case"capture_exclude_off":CaptureExclusion.Set(new System.Windows.Interop.WindowInteropHelper(this).Handle,false);break;case"minimize":WindowState=WindowState.Minimized;break;case"restore":WindowState=WindowState.Normal;Activate();break;case"desktop_switch_brief":DesktopSwitchStimulus.Begin();break;default:throw new InvalidOperationException(action);}State("setup-"+action);return Snapshot();});}
}
public sealed record RowItem(string Key,string Display,int Ordinal);
public sealed record FixtureState(int Pid,long MainHwnd,int ViewEpoch,int DialogGeneration,int ReplaceGeneration,int ParentGeneration,int PageGeneration,int MenuCount,int DuplicateACount,int DuplicateBCount,int MissingIdCount,int AsyncCount,string SelectedKey,string PrimaryText,string PhysicalText,bool Toggle,int CanvasHits,double CanvasTargetX,double CanvasTargetY,bool CanvasOverlay,long[] DialogHwnds);
static class CaptureExclusion
{
    const uint WDA_NONE=0x00000000,WDA_EXCLUDEFROMCAPTURE=0x00000011;
    [DllImport("user32.dll",SetLastError=true)] static extern bool SetWindowDisplayAffinity(IntPtr hwnd,uint affinity);
    public static void Set(IntPtr hwnd,bool excluded){if(hwnd==IntPtr.Zero||!SetWindowDisplayAffinity(hwnd,excluded?WDA_EXCLUDEFROMCAPTURE:WDA_NONE))throw new Win32Exception(Marshal.GetLastWin32Error(),"SetWindowDisplayAffinity acceptance stimulus failed");}
}static class DesktopSwitchStimulus
{
    const uint DesktopAccess=0x000F01FF;
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr OpenDesktop(string desktop,uint flags,bool inherit,uint desiredAccess);
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateDesktop(string desktop,IntPtr device,IntPtr devmode,uint flags,uint desiredAccess,IntPtr securityAttributes);
    [DllImport("user32.dll",SetLastError=true)] static extern bool SwitchDesktop(IntPtr desktop);
    [DllImport("user32.dll",SetLastError=true)] static extern bool CloseDesktop(IntPtr desktop);
    public static void Begin()
    {
        var primary=OpenDesktop("Default",0,false,DesktopAccess);if(primary==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error(),"OpenDesktop(Default) failed");
        var alternate=CreateDesktop("DESKTOPeyeAcceptance-"+Guid.NewGuid().ToString("N"),IntPtr.Zero,IntPtr.Zero,0,DesktopAccess,IntPtr.Zero);if(alternate==IntPtr.Zero){var error=Marshal.GetLastWin32Error();CloseDesktop(primary);throw new Win32Exception(error,"CreateDesktop acceptance stimulus failed");}
        if(!SwitchDesktop(alternate)){var error=Marshal.GetLastWin32Error();CloseDesktop(alternate);CloseDesktop(primary);throw new Win32Exception(error,"SwitchDesktop acceptance stimulus failed");}
        _=Task.Run(async()=>{try{await Task.Delay(1500);SwitchDesktop(primary);}finally{CloseDesktop(alternate);CloseDesktop(primary);}});
    }
}

public sealed class DialogWindow:Window
{
    public int Generation{get;} public DialogWindow(int gen){Generation=gen;Title="Confirm";Width=330;Height=160;WindowStartupLocation=WindowStartupLocation.CenterOwner;AutomationProperties.SetAutomationId(this,"ConfirmDialog");var p=new StackPanel{Margin=new Thickness(15)};var txt=new TextBlock{Text="Identical confirmation"};p.Children.Add(txt);var row=new StackPanel{Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,20,0,0)};var ok=new Button{Content="OK",Width=80,Margin=new Thickness(4)};AutomationProperties.SetAutomationId(ok,"DialogOk");ok.Click+=(_,_)=>Close();var cancel=new Button{Content="Cancel",Width=80,Margin=new Thickness(4)};AutomationProperties.SetAutomationId(cancel,"DialogCancel");cancel.Click+=(_,_)=>Close();row.Children.Add(ok);row.Children.Add(cancel);p.Children.Add(row);Content=p;}
}

public sealed class VisualOnlySurface:FrameworkElement
{
    double _x=110,_y=60;public event Action<int>? Hit; public int Hits{get;private set;}public bool Overlay{get=>_overlay;set{_overlay=value;InvalidateVisual();}}bool _overlay;public bool Ambiguous{get=>_ambiguous;set{_ambiguous=value;InvalidateVisual();}}bool _ambiguous;public double TargetX=>_x;public double TargetY=>_y;
    protected override AutomationPeer? OnCreateAutomationPeer()=>null;
    protected override void OnRender(DrawingContext dc){base.OnRender(dc);dc.DrawRectangle(Brushes.Navy,null,new Rect(0,0,ActualWidth,ActualHeight));dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(123,45,231)),null,new Rect(_x-18,_y-18,36,36));if(_ambiguous)dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(123,45,231)),null,new Rect(Math.Max(40,ActualWidth-90),25,36,36));if(_overlay)dc.DrawRectangle(Brushes.OrangeRed,null,new Rect(_x-25,_y-25,50,50));}
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e){var p=e.GetPosition(this);if(!_overlay&&Math.Abs(p.X-_x)<=18&&Math.Abs(p.Y-_y)<=18){Hits++;Hit?.Invoke(Hits);InvalidateVisual();}base.OnMouseLeftButtonDown(e);}
    public void MoveTarget(){_x=_x<ActualWidth/2?Math.Max(40,ActualWidth-100):110;_y=_y==60?90:60;InvalidateVisual();}
}

sealed class FixtureOracleServer:IDisposable
{
    readonly MainWindow _window;readonly string _pipe;readonly CancellationTokenSource _cts=new();Task? _run;public FixtureOracleServer(MainWindow w,string p){_window=w;_pipe=p;}public Task StartAsync(){_run=Task.Run(Loop);return Task.CompletedTask;}
    async Task Loop(){while(!_cts.IsCancellationRequested){await using var s=new NamedPipeServerStream(_pipe,PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);try{await s.WaitForConnectionAsync(_cts.Token);using var r=new StreamReader(s,Encoding.UTF8,false,4096,true);using var w=new StreamWriter(s,new UTF8Encoding(false),4096,true){AutoFlush=true};string? line;while((line=await r.ReadLineAsync(_cts.Token))!=null){var q=JsonDocument.Parse(line).RootElement;var cmd=q.GetProperty("cmd").GetString();object result=cmd switch{"state"=>await _window.Dispatcher.InvokeAsync(_window.Snapshot),"setup"=>await _window.SetupAsync(q.GetProperty("action").GetString()!),_=>throw new InvalidOperationException(cmd)};await w.WriteLineAsync(JsonSerializer.Serialize(result));}}catch(OperationCanceledException){break;}catch{}}}
    public void Dispose(){_cts.Cancel();try{_run?.Wait(500);}catch{}_cts.Dispose();}
}