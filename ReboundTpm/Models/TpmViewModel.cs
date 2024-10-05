using System;
using System.ComponentModel;
using System.Threading.Tasks;

public class TpmViewModel : INotifyPropertyChanged
{
    private readonly TpmManager _tpmManager;

    public string ManufacturerName => _tpmManager.ManufacturerName;
    public string ManufacturerVersion => _tpmManager.ManufacturerVersion;
    public string SpecificationVersion => _tpmManager.SpecificationVersion;
    public string Status => _tpmManager.Status;

    // New properties
    public string TpmSubVersion => _tpmManager.TpmSubVersion;
    public string PcClientSpecVersion => _tpmManager.PcClientSpecVersion;
    public string PcrValues => _tpmManager.PcrValues;

    public event PropertyChangedEventHandler PropertyChanged;

    public TpmViewModel()
    {
        _tpmManager = new TpmManager();
        _tpmManager.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
    }

    public async Task LoadTpmInfoAsync()
    {
        // Call the method to refresh TPM info
        await Task.Run(() => _tpmManager.RefreshTpmInfo());
        // Notify properties
        OnPropertyChanged(nameof(ManufacturerName));
        OnPropertyChanged(nameof(ManufacturerVersion));
        OnPropertyChanged(nameof(SpecificationVersion));
        OnPropertyChanged(nameof(Status));
        // Notify new properties
        OnPropertyChanged(nameof(TpmSubVersion));
        OnPropertyChanged(nameof(PcClientSpecVersion));
        OnPropertyChanged(nameof(PcrValues));
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
