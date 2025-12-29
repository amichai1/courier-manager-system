using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Markup;
using BO;

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
}
