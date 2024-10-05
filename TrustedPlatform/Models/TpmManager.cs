using System;
using System.ComponentModel;
using System.Management;

public class TpmManager : INotifyPropertyChanged
{
    private string _manufacturerName;
    private string _manufacturerVersion;
    private string _specificationVersion;
    private string _status;
    private string _tpmSubVersion;
    private string _pcClientSpecVersion;
    private string _pcrValues;

    public string ManufacturerName
    {
        get => _manufacturerName;
        private set
        {
            _manufacturerName = value;
            OnPropertyChanged(nameof(ManufacturerName));
        }
    }

    public string ManufacturerVersion
    {
        get => _manufacturerVersion;
        private set
        {
            _manufacturerVersion = value;
            OnPropertyChanged(nameof(ManufacturerVersion));
        }
    }

    public string SpecificationVersion
    {
        get => _specificationVersion;
        private set
        {
            _specificationVersion = value;
            OnPropertyChanged(nameof(SpecificationVersion));
        }
    }

    public string TpmSubVersion
    {
        get => _tpmSubVersion;
        private set
        {
            _tpmSubVersion = value;
            OnPropertyChanged(nameof(TpmSubVersion));
        }
    }

    public string PcClientSpecVersion
    {
        get => _pcClientSpecVersion;
        private set
        {
            _pcClientSpecVersion = value;
            OnPropertyChanged(nameof(PcClientSpecVersion));
        }
    }

    public string PcrValues
    {
        get => _pcrValues;
        private set
        {
            _pcrValues = value;
            OnPropertyChanged(nameof(PcrValues));
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            _status = value;
            OnPropertyChanged(nameof(Status));
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public TpmManager()
    {
        GetTpmInfo();
    }

    public void RefreshTpmInfo()
    {
        GetTpmInfo();
    }

    public List<string> GetTpmInfo()
    {
        try
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(@"root\CIMV2\Security\MicrosoftTpm", "SELECT * FROM Win32_Tpm"))
            {
                foreach (ManagementObject queryObj in searcher.Get())
                {
                    return new List<string>
                    {
                        queryObj["ManufacturerID"] != null ? ConvertManufacturerIdToName((uint)queryObj["ManufacturerID"]) : "Unknown",
                        queryObj["ManufacturerVersion"]?.ToString() ?? "Unknown",
                        queryObj["SpecVersion"]?.ToString() ?? "Unknown",
                        queryObj["ManufacturerVersion"]?.ToString() ?? "Unknown",
                        queryObj["SpecVersion"]?.ToString() ?? "Unknown",
                        GetPcrValues(),
                        queryObj["IsActivated_InitialValue"] != null && (bool)queryObj["IsActivated_InitialValue"] ? "Ready" : "Not Ready"
                    };
                    /*ManufacturerName = queryObj["ManufacturerID"] != null ? ConvertManufacturerIdToName((uint)queryObj["ManufacturerID"]) : "Unknown";
                    ManufacturerVersion = queryObj["ManufacturerVersion"]?.ToString() ?? "Unknown";
                    SpecificationVersion = queryObj["SpecVersion"]?.ToString() ?? "Unknown";
                    TpmSubVersion = queryObj["ManufacturerVersion"]?.ToString() ?? "Unknown";
                    PcClientSpecVersion = queryObj["SpecVersion"]?.ToString() ?? "Unknown";
                    PcrValues = GetPcrValues();

                    Status = queryObj["IsActivated_InitialValue"] != null && (bool)queryObj["IsActivated_InitialValue"] ? "Ready" : "Not Ready";*/
                }
            }
        }
        catch (Exception ex)
        {
            ManufacturerName = "N/A";
            ManufacturerVersion = "N/A";
            SpecificationVersion = "N/A";
            TpmSubVersion = "N/A";
            PcClientSpecVersion = "N/A";
            PcrValues = "N/A";
            Status = "Error communicating with TPM";
            Console.WriteLine($"An error occurred while getting TPM information: {ex.Message}");
        }

        return new List<string>()
        {
            ManufacturerName,
            ManufacturerVersion,
            SpecificationVersion,
            TpmSubVersion,
            PcClientSpecVersion,
            PcrValues,

            Status,
        };
    }

    private string ConvertManufacturerIdToName(uint manufacturerId)
    {
        var manufacturerStr = string.Empty;
        manufacturerStr += (char)((manufacturerId >> 24) & 0xFF);
        manufacturerStr += (char)((manufacturerId >> 16) & 0xFF);
        manufacturerStr += (char)((manufacturerId >> 8) & 0xFF);
        manufacturerStr += (char)(manufacturerId & 0xFF);
        return manufacturerStr;
    }

    private string GetPcrValues()
    {
        // Placeholder for PCR retrieval logic. This should query TPM for PCR values.
        // Requires advanced access via TBS API or TPM library.
        return "PCR values are not directly accessible via WMI. Requires advanced TPM API.";
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
