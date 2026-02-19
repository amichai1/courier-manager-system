namespace DO;
using System;
using System.Runtime.Serialization;

/// <summary>
/// Exception thrown when an attempt is made to access an entity that does not exist.
/// </summary>
[Serializable]
public class DalDoesNotExistException: Exception
{
    // Constructor receiving the error message
    public DalDoesNotExistException(string? message) : base(message) { }

// Default constructor
public DalDoesNotExistException() : base() { }

// Constructor for serialization
protected DalDoesNotExistException(SerializationInfo info, StreamingContext context) { }
}

/// <summary>
/// Exception thrown when an attempt is made to create an entity that already exists (duplicate ID).
/// </summary>
[Serializable]
public class DalAlreadyExistsException : Exception
{
    // Constructor receiving the error message
    public DalAlreadyExistsException(string? message) : base(message) { }

    // Default constructor 
    public DalAlreadyExistsException() : base() { }

    // Constructor for serialization
    protected DalAlreadyExistsException(SerializationInfo info, StreamingContext context) { }
    ///: base(info, context) { } -might be needed;idk yet
}
[Serializable]
public class DalXMLFileLoadCreateException : Exception
{
    public DalXMLFileLoadCreateException(string? message) : base(message) { }

    // Accepts an inner exception for XML read/write failures (e.g. permissions or format issues)
    public DalXMLFileLoadCreateException(string? message, Exception? innerException) : base(message, innerException) { }
}