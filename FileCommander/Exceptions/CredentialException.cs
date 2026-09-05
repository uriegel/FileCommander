using System;

namespace FileCommander.Exceptions;

class CredentialException : Exception
{
    public CredentialException(string message, Exception inner) : base(message, inner) {  }
}
