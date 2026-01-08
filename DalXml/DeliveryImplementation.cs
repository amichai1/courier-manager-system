namespace Dal;
using DalApi;
using DO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal class DeliveryImplementation : IDelivery
{
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
