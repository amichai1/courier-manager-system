namespace Dal;
using DalApi;
using DO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

internal class DeliveryImplementation : IDelivery
{
    static Delivery getDelivery(XElement s)
        => new Delivery(
            s.ToIntNullable("Id") ?? throw new FormatException("can't convert id"),
            s.ToIntNullable("OrderId") ?? throw new FormatException("can't convert OrderId"),
            s.ToIntNullable("CourierId") ?? throw new FormatException("can't convert CourierId"),
            s.ToEnumNullable<DeliveryType>("DeliveryType") ?? DeliveryType.OnFoot,
            s.ToDateTimeNullable("StartTime") ?? default,
            s.ToDoubleNullable("ActualDistance")
        )
        {
            CompletionStatus = s.ToEnumNullable<DeliveryStatus>("CompletionStatus"),
            EndTime = s.ToDateTimeNullable("EndTime")
        };

    static XElement createDeliveryElement(Delivery item)
    {
        var elem = new XElement("Delivery",
            new XElement("Id", item.Id),
            new XElement("OrderId", item.OrderId),
            new XElement("CourierId", item.CourierId),
            new XElement("DeliveryType", item.DeliveryType.ToString()),
            new XElement("StartTime", item.StartTime.ToString("o"))
        );
        if (item.ActualDistance.HasValue)
            elem.Add(new XElement("ActualDistance", item.ActualDistance.Value));
        if (item.CompletionStatus.HasValue)
            elem.Add(new XElement("CompletionStatus", item.CompletionStatus.Value.ToString()));
        if (item.EndTime.HasValue)
            elem.Add(new XElement("EndTime", item.EndTime.Value.ToString("o")));
        return elem;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Delivery item)
    {
        List<Delivery> list = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        int newId = Config.NextDeliveryId;
        Delivery newDelivery = item with { Id = newId };
        list.Add(newDelivery);
        XMLTools.SaveListToXMLSerializer(list, Config.s_deliveries_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Delivery? Read(int id)
    {
        List<Delivery> list = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return list.FirstOrDefault(d => d.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Delivery? Read(Func<Delivery, bool> filter)
    {
        List<Delivery> list = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return list.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Delivery> ReadAll(Func<Delivery, bool>? filter = null)
    {
        List<Delivery> list = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        return filter is null ? list : list.Where(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Delivery item)
    {
        List<Delivery> list = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        if (list.RemoveAll(d => d.Id == item.Id) == 0)
            throw new DalDoesNotExistException($"Delivery with ID={item.Id} does Not exist");
        list.Add(item);
        XMLTools.SaveListToXMLSerializer(list, Config.s_deliveries_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        List<Delivery> list = XMLTools.LoadListFromXMLSerializer<Delivery>(Config.s_deliveries_xml);
        if (list.RemoveAll(d => d.Id == id) == 0)
            throw new DalDoesNotExistException($"Delivery with ID={id} does Not exist");
        XMLTools.SaveListToXMLSerializer(list, Config.s_deliveries_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
        => XMLTools.SaveListToXMLSerializer(new List<Delivery>(), Config.s_deliveries_xml);
}
