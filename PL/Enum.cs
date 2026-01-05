using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using BO;
using PL.Converters;

namespace PL
{
    /// <summary>
    /// The existing list(with All) for use in filter screens
    /// </summary>
    [MarkupExtensionReturnType(typeof(IEnumerable<object>))]
    public class DeliveryTypesCollection : MarkupExtension, IEnumerable
    {
        static readonly IEnumerable<object> s_enums =
                    new List<object> { "All" } // add a 'all' option
                    .Concat(Enum.GetValues(typeof(DeliveryType)).Cast<object>());

        public IEnumerator GetEnumerator() => s_enums.GetEnumerator();

        public override object ProvideValue(IServiceProvider serviceProvider) => s_enums;
    }

    /// <summary>
    /// This class is intended for use in the Messenger Details window - without the "All" option.
    /// </summary>
    [MarkupExtensionReturnType(typeof(IEnumerable<object>))]
    public class CourierDeliveryTypesCollection : MarkupExtension, IEnumerable
    {
        static readonly IEnumerable<object> s_enums =
                Enum.GetValues(typeof(DeliveryType)).Cast<object>();

        public IEnumerator GetEnumerator() => s_enums.GetEnumerator();

        public override object ProvideValue(IServiceProvider serviceProvider) => s_enums;
    }

    /// <summary>
    /// Collection of courier sorting criteria for ComboBox binding.
    /// </summary>
    [MarkupExtensionReturnType(typeof(IEnumerable<CourierSortBy>))]
    public class CourierSortByCollection : MarkupExtension, IEnumerable
    {
        static readonly IEnumerable<CourierSortBy> s_enums =
            Enum.GetValues(typeof(CourierSortBy)).Cast<CourierSortBy>();

        public IEnumerator GetEnumerator() => s_enums.GetEnumerator();

        public override object ProvideValue(IServiceProvider serviceProvider) => s_enums;
    }

    /// <summary>
    /// Collection of order sorting criteria for ComboBox binding.
    /// </summary>
    [MarkupExtensionReturnType(typeof(IEnumerable<OrderSortBy>))]
    public class OrderSortByCollection : MarkupExtension, IEnumerable
    {
        static readonly IEnumerable<OrderSortBy> s_enums =
            Enum.GetValues(typeof(OrderSortBy)).Cast<OrderSortBy>();

        public IEnumerator GetEnumerator() => s_enums.GetEnumerator();

        public override object ProvideValue(IServiceProvider serviceProvider) => s_enums;
    }

    /// <summary>
    /// Collection of sort order directions for ComboBox binding.
    /// </summary>
    [MarkupExtensionReturnType(typeof(IEnumerable<SortOrder>))]
    public class SortOrderCollection : MarkupExtension, IEnumerable
    {
        static readonly IEnumerable<SortOrder> s_enums =
            Enum.GetValues(typeof(SortOrder)).Cast<SortOrder>();

        public IEnumerator GetEnumerator() => s_enums.GetEnumerator();

        public override object ProvideValue(IServiceProvider serviceProvider) => s_enums;
    }

    /// <summary>
    /// Static class to hold converter instances for XAML binding.
    /// </summary>
    public static class Converter
    {
        public static GreaterThanZeroConverter GreaterThanZeroConverter { get; } = new GreaterThanZeroConverter();
    }
}
