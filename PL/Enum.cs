namespace PL;

/// <summary>
/// The existing list(with All) for use in filter screens
/// </summary>
internal class DeliveryTypesCollection : System.Collections.IEnumerable
{
    static readonly IEnumerable<object> s_enums =
                new List<object> { "All" } // add a 'all' option
                .Concat(Enum.GetValues(typeof(BO.DeliveryType)).Cast<object>());
    public System.Collections.IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}

/// <summary>
/// This class is intended for use in the Messenger Details window - without the "All" option.
/// </summary>
internal class CourierDeliveryTypesCollection : System.Collections.IEnumerable
{
    static readonly IEnumerable<object> s_enums =
            Enum.GetValues(typeof(BO.DeliveryType)).Cast<object>();
    public System.Collections.IEnumerator GetEnumerator() => s_enums.GetEnumerator();
}
