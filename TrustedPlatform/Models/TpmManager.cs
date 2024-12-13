using System.ComponentModel;
using System.Diagnostics;
using System.Management;
using System.Text;
using Tpm2Lib;

public class TpmManager : INotifyPropertyChanged
{
    public string _manufacturerName;
    public string _manufacturerVersion;
    public string _specificationVersion;
    public string _status;
    public string _tpmSubVersion;
    public string _pcClientSpecVersion;
    public string _pcrValues;

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
        tpmDevice = new TbsDevice(); // or whichever device you are using
        tpmDevice.Connect(); // No assignment, just calling the method
        tpm = new Tpm2(tpmDevice);
        GetTpmInfo(); // No assignment, just calling the method
    }

    public void RefreshTpmInfo() => GetTpmInfo(); // No assignment, just calling the method

    private void GetTpmInfo()
    {
        try
        {
            using var searcher = new ManagementObjectSearcher(@"root\CIMV2\Security\MicrosoftTpm", "SELECT * FROM Win32_Tpm");
            foreach (ManagementObject queryObj in searcher.Get())
            {
                ManufacturerName = queryObj["ManufacturerID"] != null ? ConvertManufacturerIdToName((uint)queryObj["ManufacturerID"]) : "Unknown";
                ManufacturerVersion = queryObj["ManufacturerVersion"]?.ToString() ?? "Unknown";
                SpecificationVersion = queryObj["SpecVersion"]?.ToString() ?? "Unknown";
                TpmSubVersion = queryObj["ManufacturerVersion"]?.ToString() ?? "Unknown";
                PcClientSpecVersion = queryObj["SpecVersion"]?.ToString() ?? "Unknown";
                PcrValues = GetPcrValues();

                Status = queryObj["IsActivated_InitialValue"] != null && (bool)queryObj["IsActivated_InitialValue"] ? "Ready" : "Not Ready";
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
    }

    public string ConvertManufacturerIdToName(uint manufacturerId)
    {
        var manufacturerStr = string.Empty;
        manufacturerStr += (char)((manufacturerId >> 24) & 0xFF);
        manufacturerStr += (char)((manufacturerId >> 16) & 0xFF);
        manufacturerStr += (char)((manufacturerId >> 8) & 0xFF);
        manufacturerStr += (char)(manufacturerId & 0xFF);
        return manufacturerStr;
    }

    public Tpm2Device tpmDevice;
    public Tpm2 tpm;
    public string GetPcrValues()
    {
        try
        {
            // Specify PCR selection for reading (e.g., PCR 0, 1, 2)
            PcrSelection[] pcrSelectionIn = { new(TpmAlgId.Sha256, new uint[] { 0, 1, 2 }) };
            PcrSelection[] pcrSelectionOut;
            Tpm2bDigest[] pcrValues;

            // Read PCR values
            _ = tpm.PcrRead(pcrSelectionIn, out pcrSelectionOut, out pcrValues);

            // Build a string to display PCR values
            var pcrStringBuilder = new StringBuilder();

            // Check if pcrValues has entries
            if (pcrValues.Length == 0)
            {
                return "No PCR values available.";
            }

            for (var i = 0; i < pcrSelectionOut.Length; i++)
            {
                _ = pcrStringBuilder.AppendLine($"PCR {i}: {BitConverter.ToString(pcrValues[i].buffer)}");
            }

            return pcrStringBuilder.ToString();
        }
        catch (TpmException tpmEx)
        {
            // Log specific TPM-related errors
            Debug.WriteLine($"TPM Error retrieving PCR values: {tpmEx.Message} (Error Code:)");
            return $"TPM Error: {tpmEx.Message} (Error Code:)";
        }
        catch (Exception ex)
        {
            // Log general errors with stack trace
            Debug.WriteLine($"General Error retrieving PCR values: {ex.Message}\n{ex.StackTrace}");
            return $"Error retrieving PCR values: {ex.Message}";
        }
        finally
        {
            // Ensure resources are cleaned up
            tpm?.Dispose();
            tpmDevice?.Dispose();
        }
    }

    protected virtual void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
