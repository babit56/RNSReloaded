using System.ComponentModel;

namespace RNSReloaded.HostDT.Config;

public class Config : Configurable<Config> {
    // ReSharper disable InconsistentNaming



    [DisplayName("Enable player and auto restart")]
    [Description("is host afk")]
    [DefaultValue(true)]
    public bool AutoRestart { get; set; } = true;

    [DisplayName("Enable debug logging to console")]
    [Description("Add some extra logs")]
    [DefaultValue(false)]
    public bool DebugMode { get; set; } = false;

    [DisplayName("Make most commands host-only")]
    [Description("Only leave commands such as !help, !ping etc available to all, to avoid griefing")]
    [DefaultValue(false)]
    public bool HostOnly { get; set; } = false;
}
