namespace BO;

using System;
using System.Runtime.Serialization;

[Serializable]
public class BLException : Exception
{
    public BLException(string message) : base(message) { }
    public BLException(string message, Exception innerException) : base(message, innerException) { }
}

[Serializable]
public class BLDoesNotExistException : BLException
{
    public BLDoesNotExistException(string message) : base(message) { }
    public BLDoesNotExistException(string message, Exception innerException) : base(message, innerException) { }
}

[Serializable]
public class BLEnableDeleteACtiveCourierException : BLException
{
    public BLEnableDeleteACtiveCourierException(string message) : base(message) { }
    public BLEnableDeleteACtiveCourierException(string message, Exception innerException) : base(message, innerException) { }
}

[Serializable]
public class BLAlreadyExistsException : BLException
{
    public BLAlreadyExistsException(string message) : base(message) { }
    public BLAlreadyExistsException(string message, Exception innerException) : base(message, innerException) { }
}

[Serializable]
public class BLInvalidValueException : BLException
{
    public BLInvalidValueException(string message) : base(message) { }
}

[Serializable]
public class BLOperationFailedException : BLException
{
    public BLOperationFailedException(string message) : base(message) { }
    public BLOperationFailedException(string message, Exception innerException) : base(message, innerException) { }
}
