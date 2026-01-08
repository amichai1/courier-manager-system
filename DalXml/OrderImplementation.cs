namespace Dal;
using DalApi;
using DO;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal class OrderImplementation : IOrder
{
    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Order item)
    {
        List<Order> orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        int newId = Config.NextOrderId;
        Order newOrder = item with { Id = newId };
        orders.Add(newOrder);
        XMLTools.SaveListToXMLSerializer(orders, Config.s_orders_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(int id)
    {
        List<Order> orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return orders.FirstOrDefault(o => o.Id == id);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Order? Read(Func<Order, bool> filter)
    {
        List<Order> orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return orders.FirstOrDefault(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Order> ReadAll(Func<Order, bool>? filter = null)
    {
        List<Order> orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        return filter is null ? orders : orders.Where(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Order item)
    {
        List<Order> orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        if (orders.RemoveAll(o => o.Id == item.Id) == 0)
            throw new DalDoesNotExistException($"Order with ID={item.Id} does Not exist");
        orders.Add(item);
        XMLTools.SaveListToXMLSerializer(orders, Config.s_orders_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        List<Order> orders = XMLTools.LoadListFromXMLSerializer<Order>(Config.s_orders_xml);
        if (orders.RemoveAll(o => o.Id == id) == 0)
            throw new DalDoesNotExistException($"Order with ID={id} does Not exist");
        XMLTools.SaveListToXMLSerializer(orders, Config.s_orders_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
        => XMLTools.SaveListToXMLSerializer(new List<Order>(), Config.s_orders_xml);
}
