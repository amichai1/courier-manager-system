using Helpers;

namespace BO;

/// <summary>
/// Represents a geographical location.
/// </summary>
public class Location
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    // Override ToString using the helper method defined in BL.Helpers.Tools
    public override string ToString() => this.ToStringProperty();
}
