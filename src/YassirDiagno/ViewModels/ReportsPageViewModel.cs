using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using YassirDiagno.Services;

namespace YassirDiagno.ViewModels;

public partial class ReportsPageViewModel : ViewModelBase
{
    private readonly IReportService _report;

    [ObservableProperty] private string _reportPreview = "Aucun rapport généré. Cliquez sur 'Générer le rapport' pour commencer.";
    [ObservableProperty] private bool _isGenerating;
    [ObservableProperty] private string _status = "";

    public ReportsPageViewModel(IReportService report) => _report = report;

    [RelayCommand]
    private async Task Generate()
    {
        IsGenerating = true;
        Status = "Génération en cours...";
        try
        {
            ReportPreview = await _report.GenerateTextReportAsync();
            Status = $"✓ Rapport généré à {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex) { Status = $"Erreur: {ex.Message}"; }
        finally { IsGenerating = false; }
    }

    [RelayCommand]
    private async Task ExportText()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"YassirDiagno-Rapport-{DateTime.Now:yyyy-MM-dd-HHmm}.txt",
            DefaultExt = ".txt",
            Filter = "Texte (*.txt)|*.txt"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var text = string.IsNullOrEmpty(ReportPreview) || ReportPreview.StartsWith("Aucun")
                ? await _report.GenerateTextReportAsync()
                : ReportPreview;
            await File.WriteAllTextAsync(dialog.FileName, text);
            Status = $"✓ Exporté: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { Status = $"Erreur export: {ex.Message}"; }
    }

    [RelayCommand]
    private async Task ExportHtml()
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"YassirDiagno-Rapport-{DateTime.Now:yyyy-MM-dd-HHmm}.html",
            DefaultExt = ".html",
            Filter = "Page HTML (*.html)|*.html"
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var html = await _report.GenerateHtmlReportAsync();
            await File.WriteAllTextAsync(dialog.FileName, html);
            Status = $"✓ Exporté: {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex) { Status = $"Erreur export: {ex.Message}"; }
    }
}
