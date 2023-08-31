using System;

namespace Senq {
    /// <summary>
    /// Represents the base exception class for the Senq library from which all the other Senq exceptions are derived.
    /// </summary>
    public class SenqException : Exception {
        /// <summary>
        /// Initializes a new instance of the <see cref="SenqException"/> class.
        /// </summary>
        public SenqException() : base("Something unexpected happende during the Senq execution") {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SenqException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">Message that describes this exception.</param>
        public SenqException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SenqException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">Error message that explains the reason for the exception.</param>
        /// <param name="innerException">Exception that is the cause of the current exception.</param>
        public SenqException(string message, Exception innerException) : base(message, innerException) {
        }
    }

    /// <summary>
    /// Represents errors that occur when the first provided address is invalid or poorly formatted.
    /// </summary>
    public class BadStartingAddressException : SenqException {
        /// <summary>
        /// Initializes a new instance of the <see cref="BadStartingUriException"/> class.
        /// </summary>
        public BadStartingAddressException() : base("Starting address is invalid or poorly formatted") {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadStartingUriException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">Message that describes this exception.</param>
        public BadStartingAddressException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BadStartingAddressException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">Error message that explains the reason for the exception.</param>
        /// <param name="innerException">Exception that is the cause of the current exception.</param>
        public BadStartingAddressException(string message, Exception innerException) : base(message, innerException) {
        }
    }

    /// <summary>
    /// Represents error that occurs when all the provided prozy addresses are non functioning and the Host address
    ///  is either not working or can't be used becouse of configuration.
    /// </summary>
    public class NoWorkingClientsException : SenqException {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoWorkingClientsException"/> class.
        /// </summary>
        public NoWorkingClientsException() : base("All provided proxies are non functioning and Host address can't be used") {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoWorkingClientsException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">Message that describes this exception.</param>
        public NoWorkingClientsException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoWorkingClientsException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">Error message that explains the reason for the exception.</param>
        /// <param name="innerException">Exception that is the cause of the current exception.</param>
        public NoWorkingClientsException(string message, Exception innerException) : base(message, innerException) {
        }
    }

    /// <summary>
    /// Represents error that occurs when all connection from host to the internet couldn't be established.
    /// </summary>
    public class NoConnectionException : SenqException {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoConnectionException"/> class.
        /// </summary>
        public NoConnectionException() : base("Connection from host to the internet couldn't be established") {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoConnectionException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">Message that describes this exception.</param>
        public NoConnectionException(string message) : base(message) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NoConnectionException"/> class with a specified error message and inner exception.
        /// </summary>
        /// <param name="message">Error message that explains the reason for the exception.</param>
        /// <param name="innerException">Exception that is the cause of the current exception.</param>
        public NoConnectionException(string message, Exception innerException) : base(message, innerException) {
        }
    }
}