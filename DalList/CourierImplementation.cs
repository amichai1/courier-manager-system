namespace Dal;
using DalApi;
using DO;

/// <summary>
/// Implementation of data access methods for Courier entity.
/// Provides CRUD operations for managing courier data in the system.
/// </summary>
internal class CourierImplementation : ICourier
{
    /// <summary>
    /// Creates a new courier in the system.
    /// </summary>
    /// <param name="item">Courier object with all properties filled (ID must be valid national ID)</param>
    /// <exception cref="Exception">Thrown if a courier with the same ID already exists</exception>
    public void Create(Courier item)
    {
        // Check if courier with this ID already exists
        if (Read(item.Id) is not null)
            throw new Exception($"Courier with ID={item.Id} already exists");
        // Add the courier to the list
        DataSource.Couriers.Add(item);
    }

    /// <summary>
    /// Reads a single courier by ID.
    /// </summary>
    /// <param name="id">National ID of the courier</param>
    /// <returns>Courier object if found, null otherwise</returns>
    public Courier? Read(int id)
    {
        // Find and return courier with matching ID, or null if not found
        return DataSource.Couriers.Find(c => c.Id == id);
    }

    /// <summary>
    /// Reads all couriers in the system.
    /// </summary>
    /// <returns>Copy of the list containing all couriers</returns>
    public List<Courier> ReadAll()
    {
        return new List<Courier>(DataSource.Couriers);
    }

    /// <summary>
    /// Updates an existing courier in the system.
    /// </summary>
    /// <param name="item">Updated courier object with valid ID</param>
    /// <exception cref="Exception">Thrown if courier with the given ID does not exist</exception>
    public void Update(Courier item)
    {
        // Check if courier exists
        var ExistItem = Read(item.Id);
        if (ExistItem is null)
            throw new Exception($"Courier with ID={item.Id} does not exists");

        // Remove old courier
        DataSource.Couriers.Remove(ExistItem);
        // Add updated courier
        DataSource.Couriers.Add(item);
    }
 
    /// <summary>
    /// Deletes a courier from the system.
    /// </summary>
    /// <param name="id">National ID of the courier to delete</param>
    /// <exception cref="Exception">Thrown if courier with the given ID does not exist</exception>
    public void Delete(int id)
    {
        // Check if courier exist
        var CourierToDelete = Read(id);
        if (CourierToDelete is null)
            throw new Exception($"Courier with ID {id} does not exist");
        
        // Remove courier from list
        DataSource.Couriers.Remove(CourierToDelete);
    }

    /// <summary>
    /// Deletes all couriers from the system.
    /// </summary>
    public void DeleteAll()
    {
        DataSource.Couriers.Clear();
    }
}