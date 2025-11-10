using DO; // Assuming DO is needed here for entity definition or constraint

namespace DalApi;

public interface ICrud<T> where T : class
{
    void Create(T item); // Creates new entity object in DAL

    T? Read(int id); // Reads entity object by its ID

    void Update(T item); // Updates entity object
    void Delete(int id); // Deletes an object by its Id
    void DeleteAll(); // Delete all entity objects

    // [Chapter 8b] Update ReadAll signature for generic filtering 
     // Replaces: List<T> ReadAll(); [cite: 915]
    IEnumerable<T> ReadAll(Func<T, bool>? filter = null); // Stage 2: Returns filtered list [cite: 193]

    // [Chapter 8c] Add new Read method for single object retrieval by filter
    T? Read(Func<T, bool> filter); // Stage 2: Returns single object matching filter [cite: 227]
}