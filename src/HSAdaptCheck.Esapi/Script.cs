using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using HSAdaptCheck.Core;
using HSAdaptCheck.Esapi;
using VMS.TPS.Common.Model.API;
// No IsWriteable attribute and no BeginModifications. Loaded patient only; never opens/edits records.
namespace VMS.TPS
{
    public class Script
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            if(context.Patient==null) { MessageBox.Show("Open a patient in Eclipse first.");return; }
            var plans=context.Patient.Courses.SelectMany(c=>c.PlanSetups).OfType<ExternalPlanSetup>().ToList();
            var w=new Window {Title="HSAdaptCheck 0.1 — development prototype",Width=1200,Height=760};
            var panel=new DockPanel {Margin=new Thickness(12)};w.Content=panel;
            var top=new StackPanel();DockPanel.SetDock(top,Dock.Top);panel.Children.Add(top);
            top.Children.Add(new TextBlock {Text="Read-only physics review support. Snapshot only. Not treatment authorisation. Independently confirm configuration and dataset UIDs.",TextWrapping=TextWrapping.Wrap});
            var reference=new ComboBox();var adaptive=new ComboBox();
            foreach(var p in plans) {var label=p.Course.Id+" / "+p.Id;reference.Items.Add(new ComboBoxItem {Content=label,Tag=p});adaptive.Items.Add(new ComboBoxItem {Content=label,Tag=p});}
            top.Children.Add(new TextBlock {Text="Reference plan"});top.Children.Add(reference);top.Children.Add(new TextBlock {Text="Adaptive candidate"});top.Children.Add(adaptive);
            var buttons=new StackPanel {Orientation=Orientation.Horizontal};top.Children.Add(buttons);
            var load=new Button {Content="Load independently completed configuration",Margin=new Thickness(4)};buttons.Children.Add(load);
            var run=new Button {Content="Capture and check",Margin=new Thickness(4)};buttons.Children.Add(run);
            var export=new Button {Content="Export snapshot report",Margin=new Thickness(4),IsEnabled=false};buttons.Children.Add(export);
            var state=new TextBlock {Text="No configuration loaded. No checks run.",TextWrapping=TextWrapping.Wrap};top.Children.Add(state);
            var grid=new DataGrid {IsReadOnly=true,AutoGenerateColumns=true};panel.Children.Add(grid);
            Configuration config=null;Report report=null;
            Action invalidate=()=> {report=null;grid.ItemsSource=null;export.IsEnabled=false;state.Text="Selection/configuration changed. Capture again. No active report.";};
            reference.SelectionChanged+=(s,e)=>invalidate();adaptive.SelectionChanged+=(s,e)=>invalidate();
            load.Click+=(s,e)=> {var dialog=new OpenFileDialog {Filter="JSON configuration|*.json"};if(dialog.ShowDialog()!=true)return;invalidate();config=null;try { config=Json.Read<Configuration>(dialog.FileName);if(config==null)throw new InvalidDataException();state.Text="Loaded configuration version: "+config.Version+". Expected values must come from independent records, not generated plan names."; } catch {state.Text="Configuration load failed; correct schema/access before running.";} };
            run.Click+=(s,e)=> {
                invalidate();if(config==null||reference.SelectedItem==null||adaptive.SelectedItem==null) {state.Text="Select both plans and load configuration.";return;}
                try {
                    var snap=new Snapshot {Reference=Extractor.Read(((ComboBoxItem)reference.SelectedItem).Tag),Adaptive=Extractor.Read(((ComboBoxItem)adaptive.SelectedItem).Tag),CapturedUtc=DateTime.UtcNow.ToString("o"),Source="Eclipse read-only ESAPI adapter; API assembly "+typeof(PlanSetup).Assembly.GetName().Version};
                    report=Checker.Run(snap,config);grid.ItemsSource=report.Results;export.IsEnabled=true;
                    state.Text="Captured "+snap.CapturedUtc+". "+string.Join("; ",Enum.GetValues(typeof(Status)).Cast<Status>().Select(x=>x+"="+report.Results.Count(y=>y.Status==x)))+". Re-run after any edit. No overall treatment-release decision.";
                } catch { report=null;export.IsEnabled=false;state.Text="Capture/check failed. No report available. Check local API compatibility and permissions."; }
            };
            export.Click+=(s,e)=> {
                if(report==null)return;
                var dialog=new SaveFileDialog {Filter="HTML report|*.html",FileName="HSAdaptCheck-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss")+".html",OverwritePrompt=true};
                if(dialog.ShowDialog()!=true)return;
                try {File.WriteAllText(dialog.FileName,HtmlReport.Render(report));state.Text="Exported original snapshot; contains dataset identifiers. It does not reflect subsequent edits.";}catch {state.Text="Export failed; check approved output path and permissions.";}
            };
            w.ShowDialog();
        }
    }
}
