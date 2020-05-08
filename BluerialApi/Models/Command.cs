namespace BluerialApi.Models
{
    /// <summary>
    /// Class that describes the content of a command.
    /// The information of the Class must be coherent with that 
    /// of the README.md
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Represents the unique key in a relational database
        /// </summary>
        /// <value></value>
        public long Id { get; set; }

        /// <summary>
        /// The name or type of the command
        /// </summary>
        /// <value></value>
        public string Name { get; set; }

        /// <summary>
        /// The value of the command, if any
        /// </summary>
        /// <value></value>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// User readable description of the command
        /// </summary>
        /// <value></value>
        public string Description { get; set; }

        /// <summary>
        /// This enables a Data Transfer Object (DTO) approach
        /// </summary>
        /// <value></value>
        public string Secret { get; set; }

        /// <summary>
        /// The function is called to get the command in a sendable format
        /// </summary>
        /// <returns>Command in sendable format</returns>
        public string CommandToSend()
        {
            string output = string.Empty;

            if (!string.IsNullOrEmpty(Value))
            {
                output = Name + "-###" + Value;
            } 
            else
            {
                output = Name;
            }

            return output;
        }

        public override string ToString()
        {
            string output = string.Empty;

            if (!string.IsNullOrEmpty(Value))
            {
                output = Name + "-###" + Value + Description;
            } 
            else
            {
                output = Name + Description;
            }

            return output;
        }
    }
}