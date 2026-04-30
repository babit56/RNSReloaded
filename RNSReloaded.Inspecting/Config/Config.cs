using System.ComponentModel;

namespace RNSReloaded.Inspecting.Config;

public class Config : Configurable<Config> {
    // ReSharper disable InconsistentNaming


    [DisplayName("Dump funcs")]
    [Description("")]
    [DefaultValue(true)]
    public bool DumpFuncsConfig { get; set; } = true;
}
