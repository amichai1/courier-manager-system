namespace Dal;
using DalApi;
using DO;
using System;
using System.Linq;
using System.Xml.Linq;
using System.Runtime.CompilerServices;

internal class CourierImplementation : ICourier
{
    static Courier getCourier(XElement s)
        => new Courier(
            s.ToIntNullable("Id") ?? throw new FormatException("can't convert id"),
            s.ToDateTimeNullable("StartWorkingDate") ?? default
           )
        {
            Name = (string?)s.Element("Name") ?? "",
            Phone = (string?)s.Element("Phone") ?? "",
            Email = (string?)s.Element("Email") ?? "",
            Password = (string?)s.Element("Password") ?? "",
            IsActive = (bool?)s.Element("IsActive") ?? false,
            MaxDeliveryDistance = s.ToDoubleNullable("MaxDeliveryDistance"),
            DeliveryType = s.ToEnumNullable<DeliveryType>("DeliveryType") ?? DeliveryType.OnFoot,
            AddressLatitude = s.ToDoubleNullable("AddressLatitude") ?? 0,
            AddressLongitude = s.ToDoubleNullable("AddressLongitude") ?? 0
        };

    static XElement createCourierElement(Courier item)
    {
        var elem = new XElement("Courier",
            new XElement("Id", item.Id),
            new XElement("Name", item.Name),
            new XElement("Phone", item.Phone),
            new XElement("Email", item.Email),
            new XElement("Password", item.Password),
            new XElement("IsActive", item.IsActive),
            new XElement("DeliveryType", item.DeliveryType.ToString()),
            new XElement("StartWorkingDate", item.StartWorkingDate.ToString("o")),
            new XElement("AddressLatitude", item.AddressLatitude.ToString()),
            new XElement("AddressLongitude", item.AddressLongitude.ToString())
        );
        if (item.MaxDeliveryDistance.HasValue)
            elem.Add(new XElement("MaxDeliveryDistance", item.MaxDeliveryDistance.Value));
        return elem;
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Create(Courier item)
    {
        XElement root = XMLTools.LoadListFromXMLElement(Config.s_couriers_xml);
        if (root.Elements().Any(e => (int?)e.Element("Id") == item.Id))
            throw new DalAlreadyExistsException($"Courier with ID={item.Id} already exists");
        root.Add(createCourierElement(item));
        XMLTools.SaveListToXMLElement(root, Config.s_couriers_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(int id)
    {
        XElement? elem = XMLTools.LoadListFromXMLElement(Config.s_couriers_xml)
            .Elements().FirstOrDefault(e => (int?)e.Element("Id") == id);
        return elem is null ? null : getCourier(elem);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public Courier? Read(Func<Courier, bool> filter)
        => XMLTools.LoadListFromXMLElement(Config.s_couriers_xml)
               .Elements().Select(e => getCourier(e)).FirstOrDefault(filter);

    [MethodImpl(MethodImplOptions.Synchronized)]
    public IEnumerable<Courier> ReadAll(Func<Courier, bool>? filter = null)
    {
        var items = XMLTools.LoadListFromXMLElement(Config.s_couriers_xml)
                   .Elements().Select(e => getCourier(e));
        return filter is null ? items : items.Where(filter);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Update(Courier item)
    {
        XElement root = XMLTools.LoadListFromXMLElement(Config.s_couriers_xml);
        var found = root.Elements().FirstOrDefault(e => (int?)e.Element("Id") == item.Id)
                    ?? throw new DalDoesNotExistException($"Courier with ID={item.Id} does Not exist");
        found.Remove();
        root.Add(createCourierElement(item));
        XMLTools.SaveListToXMLElement(root, Config.s_couriers_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void Delete(int id)
    {
        XElement root = XMLTools.LoadListFromXMLElement(Config.s_couriers_xml);
        var found = root.Elements().FirstOrDefault(e => (int?)e.Element("Id") == id)
                    ?? throw new DalDoesNotExistException($"Courier with ID={id} does Not exist");
        found.Remove();
        XMLTools.SaveListToXMLElement(root, Config.s_couriers_xml);
    }

    [MethodImpl(MethodImplOptions.Synchronized)]
    public void DeleteAll()
        => XMLTools.SaveListToXMLElement(new XElement("Couriers"), Config.s_couriers_xml);
}
