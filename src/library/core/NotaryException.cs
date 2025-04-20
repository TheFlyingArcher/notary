using System;
using System.Collections.Generic;
using System.Text;

namespace Notary
{
  [Serializable]
  public class NotaryException : Exception
  {
    public NotaryException() { }

    public NotaryException(string slug, string message) : base(message) { Slug = slug; }
    public NotaryException(string message) : base(message) { }
    public NotaryException(string message, Exception inner) : base(message, inner) { }

    public string Slug { get; set; }
  }
}
