using System;

namespace Senq {
    /// <summary>
    /// Represents errors that occur when the first provided address is invalid or poorly formatted
    /// </summary>
    public class BadStartingAddressException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadStartingUriException"/> class.
        /// </summary>
        public BadStartingUriException() : base("Starting address is invalid or poorly formatted") {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadStartingUriException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes this exception.</param>
        public BadStartingUriException(string message) : base(message) {
        }
    }
}