using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class ToolsPageViewModel : ViewModelBase
{
    private readonly IDiagnosticService _diag;
    private readonly IStressTestService _stress;

    [ObservableProperty] private ObservableCollection<DiagnosticFinding> _findings = new();
    [ObservableProperty] private bool _isScanning;
    [ObservableProperty] private string _scanStatus = "Cliquez sur 'Lancer le scan' pour analyser votre PC.";

    [ObservableProperty] private bool _isStressing;
    [ObservableProperty] private int _stressDuration = 30;
    [ObservableProperty] private int _stressElapsed;
    [ObservableProperty] private double _stressCurrentTemp;
    [ObservableProperty] private double _stressPeakTemp;
    [ObservableProperty] private string _stressStatus = "Le test charge tous les coeurs CPU pendant la durée spécifiée.";

    public ToolsPageViewModel(IDiagnosticService diag, IStressTestService stress)
    {
        _diag = diag;
        _stress = stress;
        _stress.ProgressChanged += OnStressProgress;
    }

    [RelayCommand]
    private async Task RunScan()
    {
        IsScanning = true;
        ScanStatus = "Scan en cours...";
        Findings.Clear();
        try
        {
            var results = await _diag.RunQuickScanAsync();
            foreach (var f in results) Findings.Add(f);
            var critical = results.Count(r => r.Severity == DiagnosticSeverity.Critical);
            var warnings = results.Count(r => r.Severity == DiagnosticSeverity.Warning);
            ScanStatus = critical > 0
                ? $"⚠ {critical} problème(s) critique(s), {warnings} avertissement(s)"
                : warnings > 0 ? $"⚠ {warnings} avertissement(s) détecté(s)"
                : "✓ Aucun problème détecté";
        }
        catch (Exception ex) { ScanStatus = $"Erreur: {ex.Message}"; }
        finally { IsScanning = false; }
    }

    [RelayCommand]
    private async Task RunStress()
    {
        if (IsStressing) { _stress.Stop(); return; }
        IsStressing = true;
        StressElapsed = 0;
        StressPeakTemp = 0;
        StressStatus = "Stress test en cours sur tous les coeurs CPU...";
        try { await _stress.RunCpuStressAsync(Math.Max(5, Math.Min(300, StressDuration))); }
        catch (Exception ex) { StressStatus = $"Erreur: {ex.Message}"; }
        finally
        {
            IsStressing = false;
            StressStatus = $"Test terminé. Peak: {StressPeakTemp:F1}°C. Surveillez le retour à la température idle.";
        }
    }

    private void OnStressProgress(object? sender, StressTestProgress p)
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            StressElapsed = p.ElapsedSeconds;
            StressCurrentTemp = p.CurrentCpuTemp;
            StressPeakTemp = p.PeakCpuTemp;
        });
    }
}
